using System.Text;

using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// Exports messages to MySQL-compatible SQL script
    /// </summary>
    public class MySQLExporter : IMessageExporter
    {
        private readonly string _tableName;

        public MySQLExporter(string tableName = "sms_messages")
        {
            _tableName = tableName;
        }

        public async Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null)
        {
            AppLogger.Information($"Exporting {messages.Count} messages to MySQL script: {outputPath}");

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

            AppLogger.Information("MySQL export complete");
        }

        private async Task WriteSchemaAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync("-- SMS Messages MySQL Schema");
            await writer.WriteLineAsync($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync($"DROP TABLE IF EXISTS `{_tableName}`;");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync($"CREATE TABLE `{_tableName}` (");
            await writer.WriteLineAsync("    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,");
            await writer.WriteLineAsync("    `from_name` VARCHAR(255) NOT NULL,");
            await writer.WriteLineAsync("    `from_phone` VARCHAR(50) NOT NULL,");
            await writer.WriteLineAsync("    `to_name` VARCHAR(255) NOT NULL,");
            await writer.WriteLineAsync("    `to_phone` VARCHAR(50) NOT NULL,");
            await writer.WriteLineAsync("    `direction` ENUM('Sent', 'Received') NOT NULL,");
            await writer.WriteLineAsync("    `message_datetime` DATETIME NOT NULL,");
            await writer.WriteLineAsync("    `unix_timestamp` BIGINT NOT NULL,");
            await writer.WriteLineAsync("    `message_text` TEXT NOT NULL,");
            await writer.WriteLineAsync("    `has_mms` BOOLEAN DEFAULT FALSE,");
            await writer.WriteLineAsync("    `mms_files` TEXT,");
            await writer.WriteLineAsync("    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,");
            await writer.WriteLineAsync("    KEY `idx_datetime` (`message_datetime`),");
            await writer.WriteLineAsync("    KEY `idx_unix_timestamp` (`unix_timestamp`)");
            await writer.WriteLineAsync(") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            await writer.WriteLineAsync();
        }

        private async Task WriteBatchAsync(
            StreamWriter writer,
            List<SmsMessage> messages,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            await writer.WriteLineAsync($"INSERT INTO `{_tableName}`");
            await writer.WriteLineAsync("(`from_name`, `from_phone`, `to_name`, `to_phone`, `direction`, `message_datetime`, `unix_timestamp`, `message_text`, `has_mms`, `mms_files`)");
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
                    $"{(hasMms ? "1" : "0")}, {EscapeSql(mmsFiles)}){comma}");
            }

            await writer.WriteLineAsync();
        }

        private async Task WriteIndexesAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync("-- Create additional indexes for better query performance");
            await writer.WriteLineAsync($"CREATE INDEX `idx_from_phone` ON `{_tableName}`(`from_phone`);");
            await writer.WriteLineAsync($"CREATE INDEX `idx_to_phone` ON `{_tableName}`(`to_phone`);");
            await writer.WriteLineAsync($"CREATE INDEX `idx_direction` ON `{_tableName}`(`direction`);");
            await writer.WriteLineAsync($"CREATE INDEX `idx_has_mms` ON `{_tableName}`(`has_mms`);");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("-- Full-text search index");
            await writer.WriteLineAsync($"CREATE FULLTEXT INDEX `idx_message_text` ON `{_tableName}`(`message_text`);");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("-- Optimize table");
            await writer.WriteLineAsync($"OPTIMIZE TABLE `{_tableName}`;");
            await writer.WriteLineAsync();
        }

        private string EscapeSql(string value)
        {
            return string.IsNullOrEmpty(value)
                ? "NULL"
                : "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r") + "'";
        }
    }
}
