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
                Dictionary<string, string> participants = new Dictionary<string, string>();
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

    private async Task<Dictionary<string, string>> ParseGroupInfoAsync(string groupInfoPath)
    {
        Dictionary<string, string> participants = new Dictionary<string, string>();

        try
        {
            string jsonContent = await File.ReadAllTextAsync(groupInfoPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            // Parse members/participants from group_info.json
            // The exact structure may vary, so we handle common patterns
            if (root.TryGetProperty("members", out JsonElement members))
            {
                foreach (JsonElement member in members.EnumerateArray())
                {
                    string id = string.Empty;
                    string name = "Unknown";

                    if (member.TryGetProperty("id", out JsonElement idElement))
                    {
                        id = idElement.GetString() ?? string.Empty;
                    }

                    if (member.TryGetProperty("name", out JsonElement nameElement))
                    {
                        name = nameElement.GetString() ?? "Unknown";
                    }
                    else if (member.TryGetProperty("display_name", out JsonElement displayNameElement))
                    {
                        name = displayNameElement.GetString() ?? "Unknown";
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        participants[id] = name;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Failed to parse group_info.json: {Path}", groupInfoPath);
        }

        return participants;
    }

    private async Task<IEnumerable<Message>> ParseGoogleChatMessagesAsync(string messagesPath, Dictionary<string, string> participants)
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

    private Message? ParseGoogleChatMessage(JsonElement messageElement, Dictionary<string, string> participants)
    {
        try
        {
            // Get creator (sender) information
            string creatorId = string.Empty;
            string creatorName = "Unknown";

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
            }

            // If creator name is still Unknown, try to look it up in participants
            if (creatorName == "Unknown" && !string.IsNullOrEmpty(creatorId) && participants.ContainsKey(creatorId))
            {
                creatorName = participants[creatorId];
            }

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

            // For Google Chat groups, sender is the creator, recipients are other participants
            Contact sender = Contact.FromName(creatorName);
            
            // Create a generic recipient representing the group
            // We could list all participants, but that would be complex for the recipient field
            Contact recipient = Contact.FromName("Google Chat Group");

            return Message.CreateTextMessage(
                "Google Chat",
                sender,
                recipient,
                timestamp,
                messageText,
                MessageDirection.Unknown); // Direction is unknown in group chats
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
            Dictionary<string, string> participants = new Dictionary<string, string>();
            if (conversationState.TryGetProperty("conversation", out JsonElement conversation) &&
                conversation.TryGetProperty("participant_data", out JsonElement participantData))
            {
                foreach (JsonElement participant in participantData.EnumerateArray())
                {
                    string gaiaId = string.Empty;
                    string name = "Unknown";

                    if (participant.TryGetProperty("id", out JsonElement idElement) &&
                        idElement.TryGetProperty("gaia_id", out JsonElement gaiaIdElement))
                    {
                        gaiaId = gaiaIdElement.GetString() ?? string.Empty;
                    }

                    if (participant.TryGetProperty("fallback_name", out JsonElement nameElement))
                    {
                        name = nameElement.GetString() ?? "Unknown";
                    }

                    if (!string.IsNullOrEmpty(gaiaId))
                    {
                        participants[gaiaId] = name;
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

    private Message? ParseHangoutsEvent(JsonElement eventElement, Dictionary<string, string> participants)
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

            string senderName = participants.ContainsKey(senderGaiaId) 
                ? participants[senderGaiaId] 
                : "Unknown";

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

            Contact sender = Contact.FromName(senderName);
            Contact recipient = Contact.FromName("Hangouts Conversation");

            return Message.CreateTextMessage(
                "Google Hangouts",
                sender,
                recipient,
                timestamp,
                content,
                MessageDirection.Unknown);
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
