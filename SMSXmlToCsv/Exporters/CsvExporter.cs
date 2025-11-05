using System.Text;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// CSV format exporter
    /// </summary>
    public class CsvExporter : IMessageExporter
    {
        private HashSet<string>? _selectedColumns;

        public CsvExporter(HashSet<string>? selectedColumns = null)
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

            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                // Build header from selected columns
                List<string> headers = new List<string>();
                if (_selectedColumns.Contains("FromName"))
                {
                    headers.Add("From_Name");
                }

                if (_selectedColumns.Contains("FromPhone"))
                {
                    headers.Add("From_Phone");
                }

                if (_selectedColumns.Contains("ToName"))
                {
                    headers.Add("To_Name");
                }

                if (_selectedColumns.Contains("ToPhone"))
                {
                    headers.Add("To_Phone");
                }

                if (_selectedColumns.Contains("Direction"))
                {
                    headers.Add("Direction");
                }

                if (_selectedColumns.Contains("DateTime"))
                {
                    headers.Add("DateTime");
                }

                if (_selectedColumns.Contains("UnixTimestamp"))
                {
                    headers.Add("UnixTimestamp");
                }

                if (_selectedColumns.Contains("MessageText"))
                {
                    headers.Add("MessageText");
                }

                if (_selectedColumns.Contains("HasMMS"))
                {
                    headers.Add("HasMMS");
                }

                if (_selectedColumns.Contains("MMS_Files"))
                {
                    headers.Add("MMS_Files");
                }

                await writer.WriteLineAsync(string.Join(",", headers));

                // Write data
                foreach (SmsMessage msg in messages)
                {
                    bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp);
                    string mmsFiles = string.Empty;

                    if (hasMms)
                    {
                        mmsFiles = string.Join("; ", mmsAttachments[msg.UnixTimestamp].Select(a => a.FilePath));
                    }

                    List<string> values = new List<string>();
                    if (_selectedColumns.Contains("FromName"))
                    {
                        values.Add(EscapeCsv(msg.FromName));
                    }

                    if (_selectedColumns.Contains("FromPhone"))
                    {
                        values.Add(EscapeCsv(msg.FromPhone));
                    }

                    if (_selectedColumns.Contains("ToName"))
                    {
                        values.Add(EscapeCsv(msg.ToName));
                    }

                    if (_selectedColumns.Contains("ToPhone"))
                    {
                        values.Add(EscapeCsv(msg.ToPhone));
                    }

                    if (_selectedColumns.Contains("Direction"))
                    {
                        values.Add(msg.Direction);
                    }

                    if (_selectedColumns.Contains("DateTime"))
                    {
                        values.Add(msg.DateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }

                    if (_selectedColumns.Contains("UnixTimestamp"))
                    {
                        values.Add(msg.UnixTimestamp.ToString());
                    }

                    if (_selectedColumns.Contains("MessageText"))
                    {
                        values.Add(EscapeCsv(msg.MessageText));
                    }

                    if (_selectedColumns.Contains("HasMMS"))
                    {
                        values.Add(hasMms.ToString());
                    }

                    if (_selectedColumns.Contains("MMS_Files"))
                    {
                        values.Add(EscapeCsv(mmsFiles));
                    }

                    await writer.WriteLineAsync(string.Join(",", values));
                }
            }
        }

        private string EscapeCsv(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
