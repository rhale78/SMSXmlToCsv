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
/// Supports Google Hangouts (JSON) and Google Voice (HTML).
/// </summary>
public class GoogleTakeoutImporter : IDataImporter
{
    public string SourceName => "Google Takeout";

    public bool CanImport(string sourcePath)
    {
        if (!Directory.Exists(sourcePath))
        {
            return false;
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

        // Try to import Hangouts
        IEnumerable<Message> hangoutsMessages = await ImportHangoutsAsync(sourcePath);
        messages.AddRange(hangoutsMessages);

        // Try to import Google Voice
        IEnumerable<Message> voiceMessages = await ImportGoogleVoiceAsync(sourcePath);
        messages.AddRange(voiceMessages);

        return messages;
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
}
