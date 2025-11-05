using System.Text;

using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// Exports messages to PostgreSQL-compatible SQL script
    /// </summary>
    public class PostgreSQLExporter : IMessageExporter
    {
        private readonly string _tableName;

        public PostgreSQLExporter(string tableName = "sms_messages")
        {
            _tableName = tableName;
        }

        public async Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null)
        {
            AppLogger.Information($"Exporting {messages.Count} messages to PostgreSQL script: {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                // Write schema
                await WriteSchemaAsync(writer);

                // Write data in batches of 1000
                const int batchSize = 1000;
                for (int i = 0; i < messages.Count; i += batchSize)
                {
                    List<SmsMessage> batch = messages.Skip(i).Take(batchSize).ToList();
                    await WriteBatchAsync(writer, batch, mmsAttachments);
                }

                // Write indexes
                await WriteIndexesAsync(writer);
            }

            AppLogger.Information("PostgreSQL export complete");
        }

        private async Task WriteSchemaAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync("-- SMS Messages PostgreSQL Schema");
            await writer.WriteLineAsync($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync($"DROP TABLE IF EXISTS {_tableName} CASCADE;");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync($"CREATE TABLE {_tableName} (");
            await writer.WriteLineAsync("    id BIGSERIAL PRIMARY KEY,");
            await writer.WriteLineAsync("    from_name VARCHAR(255) NOT NULL,");
            await writer.WriteLineAsync("    from_phone VARCHAR(50) NOT NULL,");
            await writer.WriteLineAsync("    to_name VARCHAR(255) NOT NULL,");
            await writer.WriteLineAsync("    to_phone VARCHAR(50) NOT NULL,");
            await writer.WriteLineAsync("    direction VARCHAR(10) NOT NULL CHECK (direction IN ('Sent', 'Received')),");
            await writer.WriteLineAsync("    message_datetime TIMESTAMP NOT NULL,");
            await writer.WriteLineAsync("    unix_timestamp BIGINT NOT NULL,");
            await writer.WriteLineAsync("    message_text TEXT NOT NULL,");
            await writer.WriteLineAsync("    has_mms BOOLEAN DEFAULT FALSE,");
            await writer.WriteLineAsync("    mms_files TEXT,");
            await writer.WriteLineAsync("    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP");
            await writer.WriteLineAsync(");");
            await writer.WriteLineAsync();
        }

        private async Task WriteBatchAsync(
            StreamWriter writer,
            List<SmsMessage> messages,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            await writer.WriteLineAsync($"INSERT INTO {_tableName}");
            await writer.WriteLineAsync("(from_name, from_phone, to_name, to_phone, direction, message_datetime, unix_timestamp, message_text, has_mms, mms_files)");
            await writer.WriteLineAsync("VALUES");

            for (int i = 0; i < messages.Count; i++)
            {
                SmsMessage msg = messages[i];
                bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp);
                string mmsFiles = string.Empty;

                if (hasMms)
                {
                    mmsFiles = string.Join("; ", mmsAttachments[msg.UnixTimestamp].Select(a => a.FilePath));
                }

                string comma = i < messages.Count - 1 ? "," : ";";

                await writer.WriteLineAsync($"    ({EscapeSql(msg.FromName)}, {EscapeSql(msg.FromPhone)}, " +
                    $"{EscapeSql(msg.ToName)}, {EscapeSql(msg.ToPhone)}, " +
                    $"{EscapeSql(msg.Direction)}, '{msg.DateTime:yyyy-MM-dd HH:mm:ss}', " +
                    $"{msg.UnixTimestamp}, {EscapeSql(msg.MessageText)}, " +
                    $"{hasMms.ToString().ToUpper()}, {EscapeSql(mmsFiles)}){comma}");
            }

            await writer.WriteLineAsync();
        }

        private async Task WriteIndexesAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync("-- Create indexes for better query performance");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_from_phone ON {_tableName}(from_phone);");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_to_phone ON {_tableName}(to_phone);");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_direction ON {_tableName}(direction);");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_datetime ON {_tableName}(message_datetime);");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_unix_timestamp ON {_tableName}(unix_timestamp);");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_has_mms ON {_tableName}(has_mms);");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("-- Full-text search index");
            await writer.WriteLineAsync($"CREATE INDEX idx_{_tableName}_message_text ON {_tableName} USING GIN(to_tsvector('english', message_text));");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("-- Analyze table for query optimization");
            await writer.WriteLineAsync($"ANALYZE {_tableName};");
            await writer.WriteLineAsync();
        }

        private string EscapeSql(string value)
        {
            return string.IsNullOrEmpty(value) ? "NULL" : "'" + value.Replace("'", "''").Replace("\\", "\\\\") + "'";
        }
    }
}
