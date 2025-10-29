using Parquet;
using Parquet.Data;
using Parquet.Schema;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// Parquet format exporter
    /// </summary>
    public class ParquetExporter : IMessageExporter
    {
        private HashSet<string>? _selectedColumns;

        public ParquetExporter(HashSet<string>? selectedColumns = null)
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

            // Build schema dynamically based on selected columns
            List<DataField> fields = new List<DataField>();

            if (_selectedColumns.Contains("FromName"))
            {
                fields.Add(new DataField<string>("from_name"));
            }

            if (_selectedColumns.Contains("FromPhone"))
            {
                fields.Add(new DataField<string>("from_phone"));
            }

            if (_selectedColumns.Contains("ToName"))
            {
                fields.Add(new DataField<string>("to_name"));
            }

            if (_selectedColumns.Contains("ToPhone"))
            {
                fields.Add(new DataField<string>("to_phone"));
            }

            if (_selectedColumns.Contains("Direction"))
            {
                fields.Add(new DataField<string>("direction"));
            }

            if (_selectedColumns.Contains("DateTime"))
            {
                fields.Add(new DataField<DateTime>("date_time"));
            }

            if (_selectedColumns.Contains("UnixTimestamp"))
            {
                fields.Add(new DataField<long>("unix_timestamp"));
            }

            if (_selectedColumns.Contains("MessageText"))
            {
                fields.Add(new DataField<string>("message_text"));
            }

            if (_selectedColumns.Contains("HasMMS"))
            {
                fields.Add(new DataField<bool>("has_mms"));
            }

            if (_selectedColumns.Contains("MMS_Files"))
            {
                fields.Add(new DataField<string>("mms_files"));
            }

            ParquetSchema schema = new ParquetSchema(fields.ToArray());

            // Prepare data arrays for selected columns only
            Dictionary<string, Array> columnData = new Dictionary<string, Array>();

            if (_selectedColumns.Contains("FromName"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].FromName;
                }

                columnData["from_name"] = data;
            }
            if (_selectedColumns.Contains("FromPhone"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].FromPhone;
                }

                columnData["from_phone"] = data;
            }
            if (_selectedColumns.Contains("ToName"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].ToName;
                }

                columnData["to_name"] = data;
            }
            if (_selectedColumns.Contains("ToPhone"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].ToPhone;
                }

                columnData["to_phone"] = data;
            }
            if (_selectedColumns.Contains("Direction"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].Direction;
                }

                columnData["direction"] = data;
            }
            if (_selectedColumns.Contains("DateTime"))
            {
                DateTime[] data = new DateTime[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].DateTime;
                }

                columnData["date_time"] = data;
            }
            if (_selectedColumns.Contains("UnixTimestamp"))
            {
                long[] data = new long[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].UnixTimestamp;
                }

                columnData["unix_timestamp"] = data;
            }
            if (_selectedColumns.Contains("MessageText"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].MessageText;
                }

                columnData["message_text"] = data;
            }
            if (_selectedColumns.Contains("HasMMS"))
            {
                bool[] data = new bool[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = mmsAttachments != null && mmsAttachments.ContainsKey(messages[i].UnixTimestamp);
                }
                columnData["has_mms"] = data;
            }
            if (_selectedColumns.Contains("MMS_Files"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(messages[i].UnixTimestamp);
                    data[i] = hasMms
                        ? string.Join("; ", mmsAttachments[messages[i].UnixTimestamp].Select(a => a.FilePath))
                        : string.Empty;
                }
                columnData["mms_files"] = data;
            }

            using (Stream fileStream = File.Create(outputPath))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(schema, fileStream))
                {
                    writer.CompressionMethod = CompressionMethod.Snappy;

                    using (ParquetRowGroupWriter groupWriter = writer.CreateRowGroup())
                    {
                        // Write columns in schema order
                        for (int fieldIdx = 0; fieldIdx < fields.Count; fieldIdx++)
                        {
                            DataField field = fields[fieldIdx];
                            Array data = columnData[field.Name];
                            await groupWriter.WriteColumnAsync(new DataColumn(field, data));
                        }
                    }
                }
            }
        }
    }
}
