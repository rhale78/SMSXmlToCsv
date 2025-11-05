using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Imports messages from Instagram data export JSON files.
/// Very similar structure to Facebook Messenger.
/// </summary>
public class InstagramMessageImporter : IDataImporter
{
    public string SourceName => "Instagram";

    public async Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Look for messages/inbox directory
        string inboxPath = Path.Combine(sourcePath, "messages", "inbox");
        
        if (!Directory.Exists(inboxPath))
        {
            // Try alternative path structure
            if (Directory.Exists(Path.Combine(sourcePath, "inbox")))
            {
                inboxPath = Path.Combine(sourcePath, "inbox");
            }
            else
            {
                throw new DirectoryNotFoundException($"Could not find Instagram messages inbox directory at {inboxPath}");
            }
        }

        // Find all message_*.json files
        string[] messageFiles = Directory.GetFiles(inboxPath, "message_*.json", SearchOption.AllDirectories);

        foreach (string messageFile in messageFiles)
        {
            IEnumerable<Message> conversationMessages = await ParseMessageFile(messageFile);
            messages.AddRange(conversationMessages);
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
        catch (JsonException)
        {
            // Skip files with invalid JSON format
        }
        catch (IOException)
        {
            // Skip files that can't be read
        }
        catch (UnauthorizedAccessException)
        {
            // Skip files without read permission
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

            // Get message content
            string content = string.Empty;
            if (msgElement.TryGetProperty("content", out JsonElement contentElement))
            {
                content = contentElement.GetString() ?? string.Empty;
            }

            // Check for shares (Instagram posts, reels, etc.)
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
            Contact recipient = Contact.FromName(conversationTitle);

            // Determine direction
            MessageDirection direction = senderName.Equals("Me", StringComparison.OrdinalIgnoreCase)
                ? MessageDirection.Sent
                : MessageDirection.Received;

            return new Message(
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
}
