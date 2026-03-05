using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Imports messages from Google Takeout archives.
/// Supports Google Hangouts (JSON), Google Chat Groups (JSON), and Google Voice (HTML).
/// </summary>
public class GoogleTakeoutImporter : IDataImporter
{
    // Threshold to determine if timestamp is in milliseconds vs seconds
    // Timestamps greater than this value (year 2286 in seconds) are assumed to be milliseconds
    private const long MILLISECONDS_THRESHOLD = 10_000_000_000L;

    // Detected user email (set during import)
    private string? _userEmail;

    public string SourceName => "Google Takeout";

    public bool CanImport(string sourcePath)
    {
        if (!Directory.Exists(sourcePath))
        {
            return false;
        }

        // Check for new Google Chat Groups structure
        string[] googleChatGroupsPaths = new[]
        {
            Path.Combine(sourcePath, "Google Chat", "Groups"),
            Path.Combine(sourcePath, "google chat", "groups"),
            Path.Combine(sourcePath, "Google Chat", "groups"),
            Path.Combine(sourcePath, "google chat", "Groups")
        };

        foreach (string path in googleChatGroupsPaths)
        {
            if (Directory.Exists(path))
            {
                // Check if any subdirectory contains messages.json
                foreach (string subdir in Directory.GetDirectories(path))
                {
                    if (File.Exists(Path.Combine(subdir, "messages.json")))
                    {
                        return true;
                    }
                }
            }
        }

        // Check for Hangouts.json (case-insensitive)
        string[] hangoutsPaths = new[]
        {
            Path.Combine(sourcePath, "Hangouts", "Hangouts.json"),
            Path.Combine(sourcePath, "hangouts", "hangouts.json"),
            Path.Combine(sourcePath, "Hangouts.json"),
            Path.Combine(sourcePath, "hangouts.json"),
            Path.Combine(sourcePath, "Google Chat", "Hangouts.json"),
            Path.Combine(sourcePath, "google chat", "hangouts.json"),
            Path.Combine(sourcePath, "Google Chat", "Groups", "Hangouts.json"),
            Path.Combine(sourcePath, "google chat", "groups", "hangouts.json")
        };

        foreach (string path in hangoutsPaths)
        {
            if (File.Exists(path))
            {
                return true;
            }
        }

        // Also check subdirectories case-insensitively
        if (Directory.Exists(sourcePath))
        {
            foreach (string dir in Directory.GetDirectories(sourcePath))
            {
                string dirName = Path.GetFileName(dir).ToLowerInvariant();
                if (dirName == "hangouts" || dirName == "google chat")
                {
                    return true;
                }
            }
        }
        
        // Check for Google Voice directory (case-insensitive)
        string[] voicePaths = new[]
        {
            Path.Combine(sourcePath, "Voice", "Calls"),
            Path.Combine(sourcePath, "voice", "calls")
        };

        foreach (string path in voicePaths)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Try to import Google Chat Groups (new format)
        IEnumerable<Message> googleChatMessages = await ImportGoogleChatGroupsAsync(sourcePath);
        messages.AddRange(googleChatMessages);

        // Try to import Hangouts (legacy format)
        IEnumerable<Message> hangoutsMessages = await ImportHangoutsAsync(sourcePath);
        messages.AddRange(hangoutsMessages);

        // Try to import Google Voice
        IEnumerable<Message> voiceMessages = await ImportGoogleVoiceAsync(sourcePath);
        messages.AddRange(voiceMessages);

        return messages;
    }

    private async Task<IEnumerable<Message>> ImportGoogleChatGroupsAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Look for Google Chat Groups directory
        string[] possibleGroupsPaths = new[]
        {
            Path.Combine(sourcePath, "Google Chat", "Groups"),
            Path.Combine(sourcePath, "google chat", "groups"),
            Path.Combine(sourcePath, "Google Chat", "groups"),
            Path.Combine(sourcePath, "google chat", "Groups")
        };

        string? groupsPath = null;
        foreach (string path in possibleGroupsPaths)
        {
            if (Directory.Exists(path))
            {
                groupsPath = path;
                break;
            }
        }

        if (groupsPath == null)
        {
            return messages; // No Google Chat Groups data
        }

        // Get all subdirectories (each is a conversation/group)
        string[] groupDirectories = Directory.GetDirectories(groupsPath);
        int totalGroups = 0;
        int processedGroups = 0;

        // Count groups with messages.json first for display
        foreach (string groupDir in groupDirectories)
        {
            if (File.Exists(Path.Combine(groupDir, "messages.json")))
            {
                totalGroups++;
            }
        }

        if (totalGroups == 0)
        {
            return messages; // No valid groups found
        }

        // Detect user email by finding the most common email across all conversations
        _userEmail = await DetectUserEmailAsync(groupDirectories);
        if (!string.IsNullOrEmpty(_userEmail))
        {
            Serilog.Log.Information("Detected user email: {UserEmail}", _userEmail);
        }

        // Show count instead of enumerating
        Serilog.Log.Information("Found {GroupCount} Google Chat group(s) to import", totalGroups);

        foreach (string groupDir in groupDirectories)
        {
            string messagesPath = Path.Combine(groupDir, "messages.json");
            string groupInfoPath = Path.Combine(groupDir, "group_info.json");

            if (!File.Exists(messagesPath))
            {
                continue; // Skip if no messages.json
            }

            try
            {
                // Parse group_info.json to get recipients
                Dictionary<string, (string name, string email)> participants = new Dictionary<string, (string, string)>();
                if (File.Exists(groupInfoPath))
                {
                    participants = await ParseGroupInfoAsync(groupInfoPath);
                }

                // Parse messages.json
                IEnumerable<Message> groupMessages = await ParseGoogleChatMessagesAsync(messagesPath, participants);
                messages.AddRange(groupMessages);
                processedGroups++;

                Serilog.Log.Debug("Imported {MessageCount} messages from group: {GroupName}", 
                    groupMessages.Count(), Path.GetFileName(groupDir));
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to import Google Chat group: {GroupDir}", Path.GetFileName(groupDir));
            }
        }

        Serilog.Log.Information("Successfully imported {ProcessedCount} of {TotalCount} Google Chat group(s)", 
            processedGroups, totalGroups);

        return messages;
    }

    private async Task<Dictionary<string, (string name, string email)>> ParseGroupInfoAsync(string groupInfoPath)
    {
        Dictionary<string, (string name, string email)> participants = new Dictionary<string, (string, string)>();

        try
        {
            string jsonContent = await File.ReadAllTextAsync(groupInfoPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            // Parse members/participants from group_info.json
            // The exact structure may vary, so we handle common patterns
            if (root.TryGetProperty("members", out JsonElement members))
            {
                Serilog.Log.Debug("Parsing {MemberCount} members from group_info.json: {Path}",
                    members.GetArrayLength(), groupInfoPath);

                foreach (JsonElement member in members.EnumerateArray())
                {
                    string id = string.Empty;
                    string name = "Unknown";
                    string email = string.Empty;
                    string userType = string.Empty;

                    // Extract ID
                    if (member.TryGetProperty("id", out JsonElement idElement))
                    {
                        id = idElement.GetString() ?? string.Empty;
                    }
                    else if (member.TryGetProperty("user_id", out JsonElement userIdElement))
                    {
                        id = userIdElement.GetString() ?? string.Empty;
                    }
                    else if (member.TryGetProperty("gaia_id", out JsonElement gaiaIdElement))
                    {
                        id = gaiaIdElement.GetString() ?? string.Empty;
                    }

                    // Extract name
                    if (member.TryGetProperty("name", out JsonElement nameElement))
                    {
                        name = nameElement.GetString() ?? "Unknown";
                    }
                    else if (member.TryGetProperty("display_name", out JsonElement displayNameElement))
                    {
                        name = displayNameElement.GetString() ?? "Unknown";
                    }
                    else if (member.TryGetProperty("fallback_name", out JsonElement fallbackNameElement))
                    {
                        name = fallbackNameElement.GetString() ?? "Unknown";
                    }

                    // Extract email - try multiple fields
                    if (member.TryGetProperty("email", out JsonElement emailElement))
                    {
                        email = emailElement.GetString() ?? string.Empty;
                    }
                    else if (member.TryGetProperty("user_email", out JsonElement userEmailElement))
                    {
                        email = userEmailElement.GetString() ?? string.Empty;
                    }
                    else if (member.TryGetProperty("email_address", out JsonElement emailAddressElement))
                    {
                        email = emailAddressElement.GetString() ?? string.Empty;
                    }
                    // Sometimes the ID itself is an email
                    else if (!string.IsNullOrEmpty(id) && id.Contains("@"))
                    {
                        email = id;
                    }

                    // Extract user type (for logging/debugging)
                    if (member.TryGetProperty("user_type", out JsonElement userTypeElement))
                    {
                        userType = userTypeElement.GetString() ?? string.Empty;
                    }
                    else if (member.TryGetProperty("type", out JsonElement typeElement))
                    {
                        userType = typeElement.GetString() ?? string.Empty;
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        participants[id] = (name, email);
                        Serilog.Log.Debug("  Member: ID={Id}, Name={Name}, Email={Email}, Type={Type}",
                            id, name, email, userType);
                    }
                    else
                    {
                        Serilog.Log.Debug("  Skipping member with no ID: Name={Name}, Email={Email}", name, email);
                    }
                }

                Serilog.Log.Information("Loaded {Count} participants from group_info.json ({WithEmail} with emails)",
                    participants.Count, participants.Count(p => !string.IsNullOrEmpty(p.Value.email)));
            }
            else
            {
                Serilog.Log.Debug("No 'members' property found in group_info.json: {Path}", groupInfoPath);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to parse group_info.json: {Path}", groupInfoPath);
        }

        return participants;
    }

    private async Task<IEnumerable<Message>> ParseGoogleChatMessagesAsync(string messagesPath, Dictionary<string, (string name, string email)> participants)
    {
        List<Message> messages = new List<Message>();

        try
        {
            string jsonContent = await File.ReadAllTextAsync(messagesPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            // messages.json contains an array of messages
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement messageElement in root.EnumerateArray())
                {
                    Message? message = ParseGoogleChatMessage(messageElement, participants);
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }
            else if (root.TryGetProperty("messages", out JsonElement messagesArray))
            {
                foreach (JsonElement messageElement in messagesArray.EnumerateArray())
                {
                    Message? message = ParseGoogleChatMessage(messageElement, participants);
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to parse messages.json: {Path}", messagesPath);
        }

        return messages;
    }

    private Message? ParseGoogleChatMessage(JsonElement messageElement, Dictionary<string, (string name, string email)> participants)
    {
        try
        {
            // Get creator (sender) information - email is in the message itself!
            string creatorId = string.Empty;
            string creatorName = "Unknown";
            string creatorEmail = string.Empty;

            if (messageElement.TryGetProperty("creator", out JsonElement creator))
            {
                if (creator.TryGetProperty("user_id", out JsonElement userIdElement))
                {
                    creatorId = userIdElement.GetString() ?? string.Empty;
                }
                else if (creator.TryGetProperty("id", out JsonElement idElement))
                {
                    creatorId = idElement.GetString() ?? string.Empty;
                }

                if (creator.TryGetProperty("name", out JsonElement nameElement))
                {
                    creatorName = nameElement.GetString() ?? "Unknown";
                }
                else if (creator.TryGetProperty("display_name", out JsonElement displayNameElement))
                {
                    creatorName = displayNameElement.GetString() ?? "Unknown";
                }

                // Email is directly in the creator object
                if (creator.TryGetProperty("email", out JsonElement emailElement))
                {
                    creatorEmail = emailElement.GetString() ?? string.Empty;
                }
            }

            // Fallback: If we still don't have email, try participant lookup
            if (string.IsNullOrEmpty(creatorEmail) && !string.IsNullOrEmpty(creatorId) && participants.ContainsKey(creatorId))
            {
                if (creatorName == "Unknown")
                {
                    creatorName = participants[creatorId].name;
                }
                creatorEmail = participants[creatorId].email;
            }

            Serilog.Log.Debug("Message creator: Name={Name}, Email={Email}, ID={Id}", creatorName, creatorEmail, creatorId);

            // Get timestamp - handle both seconds and milliseconds
            long timestampValue = 0;
            if (messageElement.TryGetProperty("created_date", out JsonElement timestampElement))
            {
                string? timestampStr = timestampElement.GetString();
                if (!string.IsNullOrEmpty(timestampStr) && long.TryParse(timestampStr, out long ts))
                {
                    timestampValue = ts;
                }
            }
            else if (messageElement.TryGetProperty("timestamp", out JsonElement timestampElement2))
            {
                string? timestampStr = timestampElement2.GetString();
                if (!string.IsNullOrEmpty(timestampStr) && long.TryParse(timestampStr, out long ts))
                {
                    timestampValue = ts;
                }
            }

            // Convert timestamp using helper method
            DateTimeOffset timestamp = ParseTimestamp(timestampValue);

            // Get message text
            string messageText = string.Empty;
            if (messageElement.TryGetProperty("text", out JsonElement textElement))
            {
                messageText = textElement.GetString() ?? string.Empty;
            }
            else if (messageElement.TryGetProperty("content", out JsonElement contentElement))
            {
                messageText = contentElement.GetString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return null; // Skip empty messages
            }

            // Determine message direction based on whether creator is the user
            MessageDirection direction = MessageDirection.Unknown;
            if (!string.IsNullOrEmpty(_userEmail) && !string.IsNullOrEmpty(creatorEmail))
            {
                direction = creatorEmail.Equals(_userEmail, StringComparison.OrdinalIgnoreCase)
                    ? MessageDirection.Sent
                    : MessageDirection.Received;
            }

            // For Google Chat groups, sender is the creator, recipients are other participants
            Contact sender = !string.IsNullOrEmpty(creatorEmail)
                ? new Contact(creatorName, new HashSet<string>(), new HashSet<string> { creatorEmail })
                : Contact.FromName(creatorName);

            // Create a generic recipient representing the group
            // We could list all participants, but that would be complex for the recipient field
            Contact recipient = Contact.FromName("Google Chat Group");

            return new ChatMessage(
                "Google Chat",
                sender,
                recipient,
                timestamp,
                messageText,
                direction,
                new List<MediaAttachment>());
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Failed to parse individual Google Chat message");
            return null;
        }
    }

    private async Task<IEnumerable<Message>> ImportHangoutsAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Look for Hangouts.json in multiple locations (case-insensitive)
        string[] possiblePaths = new[]
        {
            Path.Combine(sourcePath, "Hangouts", "Hangouts.json"),
            Path.Combine(sourcePath, "hangouts", "hangouts.json"),
            Path.Combine(sourcePath, "Hangouts.json"),
            Path.Combine(sourcePath, "hangouts.json"),
            Path.Combine(sourcePath, "Google Chat", "Hangouts.json"),
            Path.Combine(sourcePath, "google chat", "hangouts.json"),
            Path.Combine(sourcePath, "Google Chat", "Groups", "Hangouts.json"),
            Path.Combine(sourcePath, "google chat", "groups", "hangouts.json")
        };

        string? hangoutsPath = null;
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                hangoutsPath = path;
                break;
            }
        }

        if (hangoutsPath == null)
        {
            return messages; // No Hangouts data
        }

        try
        {
            string jsonContent = await File.ReadAllTextAsync(hangoutsPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("conversation_state", out JsonElement conversationStates))
            {
                return messages;
            }

            foreach (JsonElement conversationState in conversationStates.EnumerateArray())
            {
                IEnumerable<Message> conversationMessages = ParseHangoutsConversation(conversationState);
                messages.AddRange(conversationMessages);
            }
        }
        catch (Exception)
        {
            // Skip if can't parse
        }

        return messages;
    }

    private IEnumerable<Message> ParseHangoutsConversation(JsonElement conversationState)
    {
        List<Message> messages = new List<Message>();

        try
        {
            // Get participants
            Dictionary<string, (string name, string email)> participants = new Dictionary<string, (string, string)>();
            if (conversationState.TryGetProperty("conversation", out JsonElement conversation) &&
                conversation.TryGetProperty("participant_data", out JsonElement participantData))
            {
                foreach (JsonElement participant in participantData.EnumerateArray())
                {
                    string gaiaId = string.Empty;
                    string name = "Unknown";
                    string email = string.Empty;

                    if (participant.TryGetProperty("id", out JsonElement idElement) &&
                        idElement.TryGetProperty("gaia_id", out JsonElement gaiaIdElement))
                    {
                        gaiaId = gaiaIdElement.GetString() ?? string.Empty;

                        // Check if there's an email in the ID element
                        if (idElement.TryGetProperty("chat_id", out JsonElement chatIdElement))
                        {
                            string chatId = chatIdElement.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(chatId) && chatId.Contains("@"))
                            {
                                email = chatId;
                            }
                        }
                    }

                    if (participant.TryGetProperty("fallback_name", out JsonElement nameElement))
                    {
                        name = nameElement.GetString() ?? "Unknown";
                    }

                    // Try to get email from various possible fields
                    if (string.IsNullOrEmpty(email) && participant.TryGetProperty("email", out JsonElement emailElement))
                    {
                        email = emailElement.GetString() ?? string.Empty;
                    }

                    if (!string.IsNullOrEmpty(gaiaId))
                    {
                        participants[gaiaId] = (name, email);
                    }
                }
            }

            // Parse events (messages)
            if (!conversationState.TryGetProperty("event", out JsonElement events))
            {
                return messages;
            }

            foreach (JsonElement eventElement in events.EnumerateArray())
            {
                Message? message = ParseHangoutsEvent(eventElement, participants);
                if (message != null)
                {
                    messages.Add(message);
                }
            }
        }
        catch (Exception)
        {
            // Skip malformed conversations
        }

        return messages;
    }

    private Message? ParseHangoutsEvent(JsonElement eventElement, Dictionary<string, (string name, string email)> participants)
    {
        try
        {
            // Get sender ID
            string senderGaiaId = string.Empty;
            if (eventElement.TryGetProperty("sender_id", out JsonElement senderId) &&
                senderId.TryGetProperty("gaia_id", out JsonElement gaiaIdElement))
            {
                senderGaiaId = gaiaIdElement.GetString() ?? string.Empty;
            }

            string senderName = "Unknown";
            string senderEmail = string.Empty;
            if (participants.ContainsKey(senderGaiaId))
            {
                senderName = participants[senderGaiaId].name;
                senderEmail = participants[senderGaiaId].email;
            }

            // Get timestamp (microseconds since Unix epoch)
            long timestampMicroseconds = 0;
            if (eventElement.TryGetProperty("timestamp", out JsonElement timestampElement))
            {
                if (long.TryParse(timestampElement.GetString(), out long ts))
                {
                    timestampMicroseconds = ts;
                }
            }
            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMicroseconds / 1000);

            // Get message content
            string content = string.Empty;
            if (eventElement.TryGetProperty("chat_message", out JsonElement chatMessage) &&
                chatMessage.TryGetProperty("message_content", out JsonElement messageContent) &&
                messageContent.TryGetProperty("segment", out JsonElement segments))
            {
                foreach (JsonElement segment in segments.EnumerateArray())
                {
                    if (segment.TryGetProperty("text", out JsonElement textElement))
                    {
                        content += textElement.GetString();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return null; // Skip empty messages
            }

            Contact sender = !string.IsNullOrEmpty(senderEmail)
                ? new Contact(senderName, new HashSet<string>(), new HashSet<string> { senderEmail })
                : Contact.FromName(senderName);
            Contact recipient = Contact.FromName("Hangouts Conversation");

            return new ChatMessage(
                "Google Hangouts",
                sender,
                recipient,
                timestamp,
                content,
                MessageDirection.Unknown,
                new List<MediaAttachment>());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<IEnumerable<Message>> ImportGoogleVoiceAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Look for Voice/Calls directory
        string voicePath = Path.Combine(sourcePath, "Voice", "Calls");
        
        if (!Directory.Exists(voicePath))
        {
            return messages; // No Google Voice data
        }

        // Find all HTML files
        string[] htmlFiles = Directory.GetFiles(voicePath, "*.html", SearchOption.TopDirectoryOnly);

        foreach (string htmlFile in htmlFiles)
        {
            IEnumerable<Message> fileMessages = await ParseGoogleVoiceHtmlAsync(htmlFile);
            messages.AddRange(fileMessages);
        }

        return messages;
    }

    private async Task<IEnumerable<Message>> ParseGoogleVoiceHtmlAsync(string htmlFilePath)
    {
        List<Message> messages = new List<Message>();

        try
        {
            string htmlContent = await File.ReadAllTextAsync(htmlFilePath);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Find message elements (this is a simplified parser - actual structure may vary)
            HtmlNodeCollection? messageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'message')]");
            
            if (messageNodes == null)
            {
                return messages;
            }

            foreach (HtmlNode messageNode in messageNodes)
            {
                Message? message = ParseGoogleVoiceMessage(messageNode);
                if (message != null)
                {
                    messages.Add(message);
                }
            }
        }
        catch (Exception)
        {
            // Skip files that can't be parsed
        }

        return messages;
    }

    private Message? ParseGoogleVoiceMessage(HtmlNode messageNode)
    {
        try
        {
            // Extract text content
            HtmlNode? textNode = messageNode.SelectSingleNode(".//div[contains(@class, 'text')]");
            string content = textNode?.InnerText?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            // Extract timestamp
            HtmlNode? timeNode = messageNode.SelectSingleNode(".//time");
            string timeStr = timeNode?.GetAttributeValue("datetime", string.Empty) ?? string.Empty;
            
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            if (!string.IsNullOrEmpty(timeStr))
            {
                DateTimeOffset.TryParse(timeStr, out timestamp);
            }

            // For Google Voice, we'll use generic contacts
            Contact sender = Contact.FromName("Contact");
            Contact recipient = Contact.FromName("Me");

            return Message.CreateTextMessage(
                "Google Voice",
                sender,
                recipient,
                timestamp,
                content,
                MessageDirection.Received);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Detect the user's email by finding the most common email across all Google Chat conversations.
    /// The user's email should appear in every conversation they participated in.
    /// </summary>
    private async Task<string?> DetectUserEmailAsync(string[] groupDirectories)
    {
        Dictionary<string, int> emailCounts = new Dictionary<string, int>();

        foreach (string groupDir in groupDirectories)
        {
            string messagesPath = Path.Combine(groupDir, "messages.json");
            if (!File.Exists(messagesPath))
            {
                continue;
            }

            try
            {
                string jsonContent = await File.ReadAllTextAsync(messagesPath);
                using JsonDocument doc = JsonDocument.Parse(jsonContent);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("messages", out JsonElement messagesArray))
                {
                    continue;
                }

                // Track unique emails per conversation
                HashSet<string> uniqueEmailsInConversation = new HashSet<string>();

                foreach (JsonElement messageElement in messagesArray.EnumerateArray())
                {
                    if (messageElement.TryGetProperty("creator", out JsonElement creator))
                    {
                        if (creator.TryGetProperty("email", out JsonElement emailElement))
                        {
                            string? email = emailElement.GetString();
                            if (!string.IsNullOrEmpty(email))
                            {
                                uniqueEmailsInConversation.Add(email);
                            }
                        }
                    }
                }

                // Increment count for each unique email found in this conversation
                foreach (string email in uniqueEmailsInConversation)
                {
                    if (!emailCounts.ContainsKey(email))
                    {
                        emailCounts[email] = 0;
                    }
                    emailCounts[email]++;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Failed to scan messages for user email detection: {Path}", messagesPath);
            }
        }

        // Find the email that appears in the most conversations (should be the user)
        if (emailCounts.Count == 0)
        {
            return null;
        }

        string? userEmail = emailCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        int conversationCount = emailCounts[userEmail];
        Serilog.Log.Debug("Email '{Email}' appears in {Count}/{Total} conversations",
            userEmail, conversationCount, groupDirectories.Length);

        return userEmail;
    }

    /// <summary>
    /// Helper method to parse timestamp value and convert to DateTimeOffset.
    /// Automatically handles both millisecond and second precision timestamps.
    /// </summary>
    /// <param name="timestampValue">Timestamp value (either seconds or milliseconds since Unix epoch)</param>
    /// <returns>DateTimeOffset representing the timestamp</returns>
    private static DateTimeOffset ParseTimestamp(long timestampValue)
    {
        // Check if timestamp is in milliseconds or seconds
        // If value is greater than MILLISECONDS_THRESHOLD (year 2286 in seconds), it's probably milliseconds
        if (timestampValue > MILLISECONDS_THRESHOLD)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestampValue);
        }
        else
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestampValue);
        }
    }
}
