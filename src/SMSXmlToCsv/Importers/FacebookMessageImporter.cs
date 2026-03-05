using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Imports messages from Facebook Messenger data export JSON files.
/// Scans the messages/inbox directory structure.
/// </summary>
public class FacebookMessageImporter : IDataImporter
{
    public string SourceName => "Facebook Messenger";

    public bool CanImport(string sourcePath)
    {
        if (!Directory.Exists(sourcePath))
        {
            return false;
        }

        // Direct checks first (original behavior)
        string inboxPath1 = Path.Combine(sourcePath, "messages", "inbox");
        string inboxPath2 = Path.Combine(sourcePath, "inbox");
        if (Directory.Exists(inboxPath1) || Directory.Exists(inboxPath2))
        {
            return true;
        }

        // NEW: recursively search for any directory structure matching */messages/inbox
        try
        {
            foreach (string messagesDir in Directory.EnumerateDirectories(sourcePath, "messages", SearchOption.AllDirectories))
            {
                string inbox = Path.Combine(messagesDir, "inbox");
                if (Directory.Exists(inbox))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore traversal errors (permissions, etc.)
        }

        // Also allow any directory whose name is 'inbox' and whose parent is 'messages'
        try
        {
            foreach (string inboxDir in Directory.EnumerateDirectories(sourcePath, "inbox", SearchOption.AllDirectories))
            {
                string? parent = Path.GetFileName(Path.GetDirectoryName(inboxDir));
                if (parent != null && parent.Equals("messages", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    public async Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source path not found: {sourcePath}");
        }

        // Parse account identity info up-front and store globally
        TryLoadFacebookIdentity(sourcePath);

        // Collect all inbox directories (messages/inbox) recursively
        HashSet<string> inboxDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Original direct pattern
        string directInbox = Path.Combine(sourcePath, "messages", "inbox");
        if (Directory.Exists(directInbox)) inboxDirectories.Add(directInbox);

        // Fallback alternative root inbox
        string altInbox = Path.Combine(sourcePath, "inbox");
        if (Directory.Exists(altInbox)) inboxDirectories.Add(altInbox);

        // Recursive discovery */messages/inbox
        try
        {
            foreach (string messagesDir in Directory.EnumerateDirectories(sourcePath, "messages", SearchOption.AllDirectories))
            {
                string inbox = Path.Combine(messagesDir, "inbox");
                if (Directory.Exists(inbox)) inboxDirectories.Add(inbox);
            }
        }
        catch { }

        // Any directory named inbox whose parent is messages
        try
        {
            foreach (string inboxDir in Directory.EnumerateDirectories(sourcePath, "inbox", SearchOption.AllDirectories))
            {
                string? parent = Path.GetFileName(Path.GetDirectoryName(inboxDir));
                if (parent != null && parent.Equals("messages", StringComparison.OrdinalIgnoreCase))
                {
                    inboxDirectories.Add(inboxDir);
                }
            }
        }
        catch { }

        if (inboxDirectories.Count == 0)
        {
            throw new DirectoryNotFoundException($"Could not find any Facebook messages inbox directories under {sourcePath}");
        }

        foreach (string inboxPath in inboxDirectories)
        {
            // Find all message_*.json files under this inbox
            string[] messageFiles = Array.Empty<string>();
            try
            {
                messageFiles = Directory.GetFiles(inboxPath, "message_*.json", SearchOption.AllDirectories);
            }
            catch { }

            foreach (string messageFile in messageFiles)
            {
                IEnumerable<Message> conversationMessages = await ParseMessageFile(messageFile);
                messages.AddRange(conversationMessages);
            }
        }

        // Prefer explicit names loaded from identity store; otherwise heuristic
        var userNames = UserIdentityStore.Names;
        string? detectedUserName = null;
        if (userNames.Count > 0)
        {
            // Use the first matching name that appears in messages; if multiple, we'll mark any match as Sent
            // We'll keep detectedUserName for logs only
            detectedUserName = messages.Select(m => m.From.Name)
                                       .FirstOrDefault(n => userNames.Contains(n));
        }
        else
        {
            detectedUserName = DetectUserName(messages);
        }

        if (userNames.Count > 0 || !string.IsNullOrWhiteSpace(detectedUserName))
        {
            HashSet<string> nameSet = userNames.Count > 0
                ? new HashSet<string>(userNames, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(new[] { detectedUserName! }, StringComparer.OrdinalIgnoreCase);

            List<Message> normalized = new List<Message>(messages.Count);
            foreach (var m in messages)
            {
                var direction = nameSet.Contains(m.From.Name)
                    ? MessageDirection.Sent
                    : MessageDirection.Received;
                normalized.Add(new SocialMediaMessage(m.SourceApplication, m.From, m.To, m.TimestampUtc, m.Body, direction, m.Attachments));
            }
            messages = normalized;
            System.Diagnostics.Debug.WriteLine($"[FacebookMessageImporter] Using user names: {string.Join(", ", nameSet)}");
            Serilog.Log.Information("Facebook import: using user names: {Names}", string.Join(", ", nameSet));
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[FacebookMessageImporter] Could not confidently detect user name; directions may be inaccurate.");
            Serilog.Log.Warning("Facebook import: could not detect user name; all directions may be treated as received.");
        }

        // Logging: total and per-contact counts (to/from)
        System.Diagnostics.Debug.WriteLine($"[FacebookMessageImporter] Total messages imported: {messages.Count}");
        Serilog.Log.Information("Facebook import: total messages imported: {Count}", messages.Count);

        // Aggregate per contact: Received counts by sender, Sent counts by conversation title (recipient)
        var receivedByContact = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sentByContact = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var msg in messages)
        {
            if (msg.Direction == MessageDirection.Received)
            {
                string name = msg.From?.Name ?? "Unknown";
                if (!receivedByContact.ContainsKey(name)) receivedByContact[name] = 0;
                receivedByContact[name]++;
            }
            else if (msg.Direction == MessageDirection.Sent)
            {
                // For Facebook JSON, recipient is the conversation title; in 1:1 chats it's usually the contact's name
                string name = msg.To?.Name ?? "Unknown";
                if (!sentByContact.ContainsKey(name)) sentByContact[name] = 0;
                sentByContact[name]++;
            }
        }

        // Merge keys for a unified view and log sorted by total desc
        var allKeys = new HashSet<string>(receivedByContact.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var k in sentByContact.Keys) allKeys.Add(k);

        var summary = allKeys
            .Select(k => new
            {
                Contact = k,
                Sent = sentByContact.TryGetValue(k, out var s) ? s : 0,
                Received = receivedByContact.TryGetValue(k, out var r) ? r : 0,
                Total = (sentByContact.TryGetValue(k, out var s2) ? s2 : 0) + (receivedByContact.TryGetValue(k, out var r2) ? r2 : 0)
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Contact, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var item in summary)
        {
            string line = $"Contact='{item.Contact}' Sent={item.Sent} Received={item.Received} Total={item.Total}";
            System.Diagnostics.Debug.WriteLine("[FacebookMessageImporter] " + line);
            Serilog.Log.Information(line);
        }

        return messages;
    }

    private async Task<IEnumerable<Message>> ParseMessageFile(string filePath)
    {
        List<Message> messages = new List<Message>();

        try
        {
            string jsonContent = await File.ReadAllTextAsync(filePath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            // Get participants
            List<string> participants = new List<string>();
            if (root.TryGetProperty("participants", out JsonElement participantsElement))
            {
                foreach (JsonElement participant in participantsElement.EnumerateArray())
                {
                    if (participant.TryGetProperty("name", out JsonElement nameElement))
                    {
                        participants.Add(nameElement.GetString() ?? "Unknown");
                    }
                }
            }

            // Get conversation title
            string conversationTitle = "Unknown";
            if (root.TryGetProperty("title", out JsonElement titleElement))
            {
                conversationTitle = titleElement.GetString() ?? "Unknown";
            }

            // Parse messages
            if (root.TryGetProperty("messages", out JsonElement messagesElement))
            {
                foreach (JsonElement msgElement in messagesElement.EnumerateArray())
                {
                    Message? message = ParseMessage(msgElement, participants, conversationTitle);
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Skip files that can't be parsed
        }

        return messages;
    }

    private Message? ParseMessage(JsonElement msgElement, List<string> participants, string conversationTitle)
    {
        try
        {
            // Get sender name
            string senderName = "Unknown";
            if (msgElement.TryGetProperty("sender_name", out JsonElement senderElement))
            {
                senderName = senderElement.GetString() ?? "Unknown";
            }

            // Get timestamp (milliseconds since Unix epoch)
            long timestampMs = 0;
            if (msgElement.TryGetProperty("timestamp_ms", out JsonElement timestampElement))
            {
                timestampMs = timestampElement.GetInt64();
            }
            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);

            // Get message content (may be absent for non-text messages)
            string content = string.Empty;
            if (msgElement.TryGetProperty("content", out JsonElement contentElement))
            {
                content = contentElement.GetString() ?? string.Empty;
            }

            // Check for shares (links, posts)
            if (msgElement.TryGetProperty("share", out JsonElement shareElement))
            {
                if (shareElement.TryGetProperty("link", out JsonElement linkElement))
                {
                    string link = linkElement.GetString() ?? string.Empty;
                    content += $"\n[Shared: {link}]";
                }
                if (shareElement.TryGetProperty("share_text", out JsonElement shareTextElement))
                {
                    string shareText = shareTextElement.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(shareText))
                    {
                        content += $"\n{shareText}";
                    }
                }
            }

            // Get attachments
            List<MediaAttachment> attachments = new List<MediaAttachment>();
            
            if (msgElement.TryGetProperty("photos", out JsonElement photosElement))
            {
                foreach (JsonElement photo in photosElement.EnumerateArray())
                {
                    if (photo.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        string uri = uriElement.GetString() ?? string.Empty;
                        attachments.Add(new MediaAttachment(uri, "image/jpeg"));
                    }
                }
            }

            if (msgElement.TryGetProperty("videos", out JsonElement videosElement))
            {
                foreach (JsonElement video in videosElement.EnumerateArray())
                {
                    if (video.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        string uri = uriElement.GetString() ?? string.Empty;
                        attachments.Add(new MediaAttachment(uri, "video/mp4"));
                    }
                }
            }

            if (msgElement.TryGetProperty("files", out JsonElement filesElement))
            {
                foreach (JsonElement file in filesElement.EnumerateArray())
                {
                    if (file.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        string uri = uriElement.GetString() ?? string.Empty;
                        attachments.Add(new MediaAttachment(uri, "application/octet-stream"));
                    }
                }
            }

            if (msgElement.TryGetProperty("audio_files", out JsonElement audioElement))
            {
                foreach (JsonElement audio in audioElement.EnumerateArray())
                {
                    if (audio.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        string uri = uriElement.GetString() ?? string.Empty;
                        attachments.Add(new MediaAttachment(uri, "audio/mpeg"));
                    }
                }
            }

            // Skip messages with no content and no attachments
            if (string.IsNullOrWhiteSpace(content) && attachments.Count == 0)
            {
                return null;
            }

            // Create contacts
            Contact sender = Contact.FromName(senderName);
            // For Facebook, the recipient is typically "the conversation" rather than a specific person
            Contact recipient = Contact.FromName(conversationTitle);

            // Determine direction (we consider "Me" as sent, others as received)
            MessageDirection direction = senderName.Equals("Me", StringComparison.OrdinalIgnoreCase)
                ? MessageDirection.Sent
                : MessageDirection.Received;

            return new SocialMediaMessage(
                SourceName,
                sender,
                recipient,
                timestamp,
                content,
                direction,
                attachments);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void TryLoadFacebookIdentity(string sourcePath)
    {
        try
        {
            // Common FB export locations for identity info
            // account_information/profile_information.json
            // account_information/personal_information.json
            // account_information/contact_info.json
            List<string> candidates = new List<string>();
            try
            {
                foreach (var path in Directory.EnumerateFiles(sourcePath, "*.json", SearchOption.AllDirectories))
                {
                    string lower = path.Replace('\\', '/').ToLowerInvariant();
                    if (lower.Contains("account_information/profile_information.json") ||
                        lower.Contains("account information/profile information.json") ||
                        lower.EndsWith("/profile_information.json") ||
                        lower.Contains("account_information/personal_information.json") ||
                        lower.EndsWith("/personal_information.json") ||
                        lower.Contains("account_information/contact_info.json") ||
                        lower.EndsWith("/contact_info.json"))
                    {
                        candidates.Add(path);
                    }
                }
            }
            catch { }

            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);

            foreach (string file in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    ExtractNamesAndEmails(doc.RootElement, names, emails);
                }
                catch { }
            }

            if (names.Count > 0) UserIdentityStore.AddNames(names);
            if (emails.Count > 0) UserIdentityStore.AddEmails(emails);

            if (names.Count > 0 || emails.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[FacebookMessageImporter] Loaded identity: names=({string.Join(", ", names)}), emails=({string.Join(", ", emails)})");
                Serilog.Log.Information("Facebook identity: names={Names} emails={Emails}", string.Join(", ", names), string.Join(", ", emails));
            }
        }
        catch { }
    }

    private void ExtractNamesAndEmails(JsonElement element, HashSet<string> names, HashSet<string> emails)
    {
        // Walk JSON recursively, collecting plausible names and emails
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    string key = prop.Name.ToLowerInvariant();
                    if (key.Contains("name") && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var name = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
                    }
                    if ((key.Contains("email") || key.Contains("mail")) && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var email = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(email)) emails.Add(email);
                    }
                    // Recurse
                    ExtractNamesAndEmails(prop.Value, names, emails);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractNamesAndEmails(item, names, emails);
                }
                break;
            case JsonValueKind.String:
                var str = element.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    if (str.Contains("@")) emails.Add(str);
                }
                break;
        }
    }

    // Heuristic to detect the user's own name: choose the sender name that appears in the most distinct conversations.
    // If tie, choose the one with highest total message count. Require a minimum presence across conversations.
    private string? DetectUserName(List<Message> messages)
    {
        if (messages.Count == 0) return null;

        // Build conversation identifier set per sender (using recipient Name as conversation key)
        var conversationPresence = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var totalCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var m in messages)
        {
            string sender = m.From.Name ?? "Unknown";
            string convo = m.To.Name ?? "Conversation";
            if (!conversationPresence.ContainsKey(sender)) conversationPresence[sender] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            conversationPresence[sender].Add(convo);
            if (!totalCounts.ContainsKey(sender)) totalCounts[sender] = 0;
            totalCounts[sender]++;
        }

        var ranked = conversationPresence
            .Select(kvp => new
            {
                Sender = kvp.Key,
                ConversationCount = kvp.Value.Count,
                TotalMessages = totalCounts[kvp.Key]
            })
            .OrderByDescending(x => x.ConversationCount)
            .ThenByDescending(x => x.TotalMessages)
            .ToList();

        if (ranked.Count == 0) return null;

        var top = ranked.First();
        // Require at least presence in 2 conversations to reduce false positives in single-convo exports
        if (top.ConversationCount < 2) return null;
        return top.Sender;
    }
}
