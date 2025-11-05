using System.Text;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// HTML chat-style exporter
    /// </summary>
    public class HtmlExporter : IMessageExporter
    {
        private readonly string _userName;
        private readonly string _userPhone;

        public HtmlExporter(string userName, string userPhone)
        {
            _userName = userName;
            _userPhone = userPhone;
        }

        public async Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null)
        {
            // FIX: HTML files should be placed in the same folder structure as ContactSplitter
            // Group by contact
            Dictionary<string, List<SmsMessage>> contactMessages = new Dictionary<string, List<SmsMessage>>();
            foreach (SmsMessage msg in messages)
            {
                // FIX: Correct the logic for determining contact based on message direction
                // If we SENT the message, the contact is the recipient (ToName/ToPhone)
                // If we RECEIVED the message, the contact is the sender (FromName/FromPhone)
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;

                if (contactPhone == _userPhone)
                {
                    continue;
                }

                string key = $"{contactName}|{contactPhone}";
                if (!contactMessages.ContainsKey(key))
                {
                    contactMessages[key] = new List<SmsMessage>();
                }
                contactMessages[key].Add(msg);
            }

            // FIX: Determine base directory from outputPath
            // outputPath typically looks like: "C:\...\something\filename" when called from Program.cs
            // We want HTML files in the Contacts folder alongside CSV/JSON/Parquet files
            string baseDir = Path.GetDirectoryName(outputPath) ?? ".";

            // Check if we're already in a Contacts folder structure
            bool isInContactsFolder = baseDir.Contains("\\Contacts\\") || baseDir.Contains("/Contacts/") ||
 baseDir.Contains("\\Contacts") || baseDir.Contains("/Contacts");

            string htmlDir;
            if (isInContactsFolder)
            {
                // We're being called from ContactSplitter - use the Contacts folder directly
                // Extract the Contacts folder path
                int contactsIndex = baseDir.LastIndexOf("Contacts");
                if (contactsIndex >= 0)
                {
                    // Use the Contacts folder itself
                    string contactsFolderPath = baseDir.Substring(0, contactsIndex + "Contacts".Length);
                    htmlDir = contactsFolderPath;
                }
                else
                {
                    htmlDir = baseDir;
                }
            }
            else
            {
                // Not in Contacts structure - create Contacts folder and use that
                // This ensures HTML files are always in Contacts, never in a separate HTML folder
                htmlDir = Path.Combine(baseDir, "Contacts");
                Directory.CreateDirectory(htmlDir);
            }

            // Create individual conversation HTML files in their respective contact folders
            foreach (KeyValuePair<string, List<SmsMessage>> kvp in contactMessages)
            {
                string[] parts = kvp.Key.Split('|');
                string contactName = parts[0];
                string contactPhone = parts[1];

                // FIX: Create HTML in the contact's own folder
                string sanitizedContactName = SanitizeName(contactName);
                string contactFolder = Path.Combine(htmlDir, sanitizedContactName);
                Directory.CreateDirectory(contactFolder);

                await CreateConversationPageAsync(
                    contactFolder,
                    contactName,
                    contactPhone,
                    kvp.Value,
                    mmsAttachments);
            }

            // Create index page in the Contacts folder root
            await CreateIndexPageAsync(htmlDir, contactMessages);
        }

        private async Task CreateIndexPageAsync(
            string outputDir,
            Dictionary<string, List<SmsMessage>> contactMessages)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>SMS Conversations</title>");
            html.AppendLine(GetSharedCss());
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine("        <h1>SMS Conversations</h1>");
            html.AppendLine("        <div class=\"contact-list\">");

            // Sort by message count
            List<KeyValuePair<string, List<SmsMessage>>> sorted = contactMessages
                .OrderByDescending(kvp => kvp.Value.Count)
                .ToList();

            foreach (KeyValuePair<string, List<SmsMessage>> kvp in sorted)
            {
                string[] parts = kvp.Key.Split('|');
                string contactName = parts[0];
                string contactPhone = parts[1];
                int messageCount = kvp.Value.Count;

                string sanitizedContactName = SanitizeName(contactName);
                // FIX: Match the simplified filename format
                string fileName = SanitizeFileName($"{contactName}.html");
                string relativePath = $"{sanitizedContactName}/{fileName}";

                html.AppendLine($"            <div class=\"contact-card\" onclick=\"window.location='{relativePath}'\">");
                html.AppendLine($"                <div class=\"contact-name\">{HtmlEncode(contactName)}</div>");
                html.AppendLine($"                <div class=\"contact-phone\">{MaskPhone(contactPhone)}</div>");
                html.AppendLine($"                <div class=\"contact-count\">{messageCount:N0} messages</div>");
                html.AppendLine("            </div>");
            }

            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), html.ToString());
        }

        private async Task CreateConversationPageAsync(
            string outputDir,
            string contactName,
            string contactPhone,
            List<SmsMessage> messages,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            // FIX: Use same filename as CSV/JSON/Parquet - no phone number
            string fileName = SanitizeFileName($"{contactName}.html");
            StringBuilder html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine($"    <title>Conversation with {HtmlEncode(contactName)}</title>");
            html.AppendLine(GetSharedCss());
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine($"        <div class=\"header\">");
            html.AppendLine($"            <a href=\"../index.html\" class=\"back-link\">? Back to Contacts</a>");
            html.AppendLine($"            <h1>{HtmlEncode(contactName)}</h1>");
            html.AppendLine($"            <div class=\"subtitle\">{MaskPhone(contactPhone)}</div>");
            html.AppendLine("        </div>");
            html.AppendLine("        <div class=\"conversation\">");

            // Group messages by date
            DateTime? currentDate = null;

            foreach (SmsMessage msg in messages)
            {
                // Add date separator if new day
                if (currentDate == null || currentDate.Value.Date != msg.DateTime.Date)
                {
                    currentDate = msg.DateTime.Date;
                    html.AppendLine($"            <div class=\"date-separator\">{currentDate.Value:dddd, MMMM d, yyyy}</div>");
                }

                // CRITICAL FIX: Determine actual direction based on phone numbers, not just Direction field
                // The Direction field might be from the contact's perspective in some backups
                bool actuallyFromUser;

                // Check: if FromPhone matches user's phone, YOU sent it
                // If ToPhone matches user's phone, you RECEIVED it
                if (!string.IsNullOrEmpty(msg.FromPhone) && !string.IsNullOrEmpty(_userPhone))
                {
                    // Compare last 10 digits to handle country code differences
                    string fromDigits = new string(msg.FromPhone.Where(char.IsDigit).ToArray());
                    string userDigits = new string(_userPhone.Where(char.IsDigit).ToArray());
                    string from10 = fromDigits.Length >= 10 ? fromDigits.Substring(fromDigits.Length - 10) : fromDigits;
                    string user10 = userDigits.Length >= 10 ? userDigits.Substring(userDigits.Length - 10) : userDigits;

                    actuallyFromUser = (from10 == user10);
                }
                else
                {
                    // Fallback to Direction field if phone comparison fails
                    actuallyFromUser = (msg.Direction == "Sent");
                }

                // Now set the bubble class based on actual sender
                string bubbleClass = actuallyFromUser ? "message sent" : "message received";
                string dataSender = actuallyFromUser ? "" : $" data-sender=\"{HtmlEncode(contactName)}\"";

                html.AppendLine($"            <div class=\"{bubbleClass}\"{dataSender}>");
                html.AppendLine($"                <div class=\"message-text\">{HtmlEncode(msg.MessageText)}</div>");

                // MMS attachments
                if (mmsAttachments != null && mmsAttachments.TryGetValue(msg.UnixTimestamp, out List<MmsAttachment>? attachments))
                {
                    foreach (MmsAttachment attachment in attachments)
                    {
                        // FIX: HTML is now in Contacts/ContactName/ folder, MMS is in Contacts/ContactName/MMS/
                        // So MMS path is just relative: MMS/Sent/file.jpg or MMS/Received/file.jpg
                        string relativePath = attachment.FilePath;

                        if (attachment.ContentType.StartsWith("image/"))
                        {
                            html.AppendLine($"                <img src=\"{relativePath}\" class=\"mms-image\" alt=\"MMS attachment\">");
                        }
                        else if (attachment.ContentType.StartsWith("video/"))
                        {
                            html.AppendLine($"                <video src=\"{relativePath}\" class=\"mms-video\" controls></video>");
                        }
                        else
                        {
                            html.AppendLine($"                <a href=\"{relativePath}\" class=\"mms-file\">?? {HtmlEncode(attachment.FileName)}</a>");
                        }
                    }
                }

                html.AppendLine($"                <div class=\"message-time\">{msg.DateTime:h:mm tt}</div>");
                html.AppendLine("            </div>");
            }

            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            await File.WriteAllTextAsync(Path.Combine(outputDir, fileName), html.ToString());
        }

        private string GetSharedCss()
        {
            return @"
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: #f0f0f5;
            padding: 20px;
        }
        .container {
            max-width: 800px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        h1 {
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            margin: 0;
        }
        .subtitle {
            color: rgba(255,255,255,0.8);
            font-size: 14px;
            margin-top: 5px;
        }
        .header {
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }
        .back-link {
            color: white;
            text-decoration: none;
            display: inline-block;
            margin-bottom: 10px;
            opacity: 0.9;
        }
        .back-link:hover { opacity: 1; }
        .contact-list {
            padding: 20px;
        }
        .contact-card {
            padding: 15px;
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            margin-bottom: 10px;
            cursor: pointer;
            transition: all 0.2s;
        }
        .contact-card:hover {
            background: #f8f8f8;
            transform: translateY(-2px);
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        .contact-name {
            font-weight: 600;
            font-size: 16px;
            color: #333;
        }
        .contact-phone {
            color: #666;
            font-size: 14px;
            margin-top: 2px;
        }
        .contact-count {
            color: #667eea;
            font-size: 13px;
            margin-top: 5px;
        }
        .conversation {
            padding: 20px;
            min-height: 400px;
        }
        .date-separator {
            text-align: center;
            color: #999;
            font-size: 13px;
            margin: 20px 0;
            position: relative;
            font-weight: bold;
            clear: both;  /* FIX: Clear floats so date separator stays with messages */
        }
        .date-separator::before,
        .date-separator::after {
            content: '';
            position: absolute;
            top: 50%;
            width: 30%;
            height: 1px;
            background: #ddd;
        }
        .date-separator::before { left: 0; }
        .date-separator::after { right: 0; }
        .message {
            max-width: 70%;
            padding: 12px 16px;
            border-radius: 18px;
            margin: 8px 0;
            position: relative;
            clear: both;
            /* ISSUE #7 FIX: Add labels for copy/paste */
            display: block;
        }
        /* ISSUE #7 FIX: Add visible sender labels that copy well */
        .message.sent::before {
            content: 'You: ';
            font-weight: bold;
            display: inline;
        }
        .message.received::before {
            content: attr(data-sender) ': ';
            font-weight: bold;
            display: inline;
        }
        .message.sent {
            background: #007AFF;
            color: white;
            float: right;
            margin-left: auto;
        }
        .message.received {
            background: #E5E5EA;
            color: black;
            float: left;
            margin-right: auto;
        }
        .message-text {
            word-wrap: break-word;
            line-height: 1.4;
            display: inline;
        }
        .message-time {
            font-size: 11px;
            opacity: 0.7;
            margin-top: 4px;
            text-align: right;
            display: block;
        }
        /* ISSUE #7 FIX: Add brackets to timestamp for clarity when copied */
        .message-time::before {
            content: '[';
        }
        .message-time::after {
            content: ']';
        }
        .mms-image {
            max-width: 100%;
            border-radius: 8px;
            margin-top: 8px;
            display: block;
        }
        .mms-video {
            max-width: 100%;
            border-radius: 8px;
            margin-top: 8px;
            display: block;
        }
        .mms-file {
            display: block;
            margin-top: 8px;
            color: inherit;
            text-decoration: none;
        }
        /* ISSUE #7 FIX: Add separator between messages when copied */
        .message::after {
            content: '\A';
            white-space: pre;
        }
        /* ISSUE #7 FIX: Better copy/paste formatting */
        @media print {
            .message {
                page-break-inside: avoid;
                border: 1px solid #ddd;
                margin: 10px 0;
                float: none !important;
                max-width: 100%;
            }
            .date-separator::before,
            .date-separator::after {
                display: none;
            }
        }
        @media (max-width: 768px) {
            body { padding: 10px; }
            .message { max-width: 85%; }
        }
    </style>
";
        }

        private string HtmlEncode(string text)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }

        /// <summary>
        /// Sanitize contact name for folder naming (matches ContactSplitter logic)
        /// </summary>
        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "(Unknown)")
            {
                return "Unknown";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

            // Remove leading/trailing spaces and dots (Windows doesn't allow these)
            sanitized = sanitized.Trim(' ', '.');

            // Replace spaces with underscores
            sanitized = sanitized.Replace(' ', '_');

            // Remove any multiple consecutive underscores
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');

            // Limit length
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50).TrimEnd('_', '.', ' ');
            }

            return string.IsNullOrEmpty(sanitized) ? "Unknown" : sanitized;
        }

        private string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            {
                return "+****";
            }

            string digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 ? $"+****{digits.Substring(digits.Length - 4)}" : "+****";
        }
    }
}
