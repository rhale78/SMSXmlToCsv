using System.Text;

using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// Exports messages to Markdown format
    /// </summary>
    public class MarkdownExporter : IMessageExporter
    {
        private readonly string _userPhone;
        private readonly HashSet<string>? _selectedColumns;

        public MarkdownExporter(string userPhone, HashSet<string>? selectedColumns = null)
        {
            _userPhone = userPhone;
            _selectedColumns = selectedColumns ?? FeatureConfiguration.GetDefaultColumns();
        }

        public async Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null)
        {
            AppLogger.Information($"Exporting {messages.Count} messages to Markdown: {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                // Write header
                await WriteHeaderAsync(writer, messages);

                // Group by contact and write conversations
                Dictionary<string, List<SmsMessage>> byContact = GroupByContact(messages);

                foreach (KeyValuePair<string, List<SmsMessage>> kvp in byContact.OrderByDescending(k => k.Value.Count))
                {
                    await WriteContactConversationAsync(writer, kvp.Key, kvp.Value, mmsAttachments);
                }
            }

            AppLogger.Information("Markdown export complete");
        }

        private async Task WriteHeaderAsync(StreamWriter writer, List<SmsMessage> messages)
        {
            await writer.WriteLineAsync("# SMS Conversation Export");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"**Exported**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync($"**Total Messages**: {messages.Count:N0}");
            await writer.WriteLineAsync($"**Date Range**: {messages.Min(m => m.DateTime):yyyy-MM-dd} to {messages.Max(m => m.DateTime):yyyy-MM-dd}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("---");
            await writer.WriteLineAsync();
        }

        private async Task WriteContactConversationAsync(
            StreamWriter writer,
            string contactKey,
            List<SmsMessage> messages,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            string[] parts = contactKey.Split('|');
            string contactName = parts[0];
            string contactPhone = parts[1];

            await writer.WriteLineAsync($"## {contactName}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"**Phone**: {MaskPhone(contactPhone)}");
            await writer.WriteLineAsync($"**Messages**: {messages.Count:N0}");
            await writer.WriteLineAsync($"**Period**: {messages.Min(m => m.DateTime):yyyy-MM-dd} to {messages.Max(m => m.DateTime):yyyy-MM-dd}");
            await writer.WriteLineAsync();

            // Write conversation
            DateTime? lastDate = null;

            foreach (SmsMessage msg in messages.OrderBy(m => m.UnixTimestamp))
            {
                // Add date separator if day changed
                if (lastDate == null || msg.DateTime.Date != lastDate.Value.Date)
                {
                    await writer.WriteLineAsync($"### {msg.DateTime:yyyy-MM-dd dddd}");
                    await writer.WriteLineAsync();
                    lastDate = msg.DateTime;
                }

                // Write message
                string sender = msg.Direction == "Sent" ? "**You**" : $"**{contactName}**";
                string time = msg.DateTime.ToString("HH:mm");

                await writer.WriteLineAsync($"**{time}** {sender}:");
                await writer.WriteLineAsync();

                // Format message text
                string[] lines = msg.MessageText.Split('\n');
                foreach (string line in lines)
                {
                    await writer.WriteLineAsync($"> {line}");
                }

                // Add MMS attachments if present
                if (mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp))
                {
                    await writer.WriteLineAsync(">");
                    await writer.WriteLineAsync("> ?? **Attachments**:");

                    foreach (MmsAttachment attachment in mmsAttachments[msg.UnixTimestamp])
                    {
                        string fileName = Path.GetFileName(attachment.FilePath);
                        await writer.WriteLineAsync($"> - `{fileName}`");
                    }
                }

                await writer.WriteLineAsync();
            }

            await writer.WriteLineAsync("---");
            await writer.WriteLineAsync();
        }

        private Dictionary<string, List<SmsMessage>> GroupByContact(List<SmsMessage> messages)
        {
            Dictionary<string, List<SmsMessage>> grouped = new Dictionary<string, List<SmsMessage>>();

            foreach (SmsMessage msg in messages)
            {
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;

                if (contactPhone == _userPhone)
                {
                    continue;
                }

                string key = $"{contactName}|{contactPhone}";

                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = new List<SmsMessage>();
                }

                grouped[key].Add(msg);
            }

            return grouped;
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
