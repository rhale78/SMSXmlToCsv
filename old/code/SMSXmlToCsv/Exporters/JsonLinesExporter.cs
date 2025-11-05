using System.Globalization;
using System.Text;
using System.Text.Json;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// JSON Lines format exporter
    /// </summary>
    public class JsonLinesExporter : IMessageExporter
    {
        private HashSet<string>? _selectedColumns;

        public JsonLinesExporter(HashSet<string>? selectedColumns = null)
        {
            _selectedColumns = selectedColumns;
        }

        public async Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null)
        {
            // Use default columns if none specified
            if (_selectedColumns == null || _selectedColumns.Count == 0)
            {
                _selectedColumns = FeatureConfiguration.GetDefaultColumns();
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                foreach (SmsMessage msg in messages)
                {
                    bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp);
                    List<string>? mmsFiles = null;

                    if (hasMms)
                    {
                        mmsFiles = mmsAttachments[msg.UnixTimestamp].Select(a => a.FilePath).ToList();
                    }

                    // Build dynamic object with only selected columns
                    Dictionary<string, object?> jsonMsg = new Dictionary<string, object?>();

                    if (_selectedColumns.Contains("FromName"))
                    {
                        jsonMsg["fromName"] = msg.FromName;
                    }

                    if (_selectedColumns.Contains("FromPhone"))
                    {
                        jsonMsg["fromPhone"] = msg.FromPhone;
                    }

                    if (_selectedColumns.Contains("ToName"))
                    {
                        jsonMsg["toName"] = msg.ToName;
                    }

                    if (_selectedColumns.Contains("ToPhone"))
                    {
                        jsonMsg["toPhone"] = msg.ToPhone;
                    }

                    if (_selectedColumns.Contains("Direction"))
                    {
                        jsonMsg["direction"] = msg.Direction;
                    }

                    if (_selectedColumns.Contains("DateTime"))
                    {
                        jsonMsg["dateTime"] = msg.DateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                    }

                    if (_selectedColumns.Contains("UnixTimestamp"))
                    {
                        jsonMsg["unixTimestamp"] = msg.UnixTimestamp;
                    }

                    if (_selectedColumns.Contains("MessageText"))
                    {
                        jsonMsg["messageText"] = msg.MessageText;
                    }

                    if (_selectedColumns.Contains("HasMMS"))
                    {
                        jsonMsg["hasMMS"] = hasMms;
                    }

                    if (_selectedColumns.Contains("MMS_Files"))
                    {
                        jsonMsg["mmsFiles"] = mmsFiles;
                    }

                    string json = JsonSerializer.Serialize(jsonMsg, options);
                    await writer.WriteLineAsync(json);
                }
            }
        }
    }
}
