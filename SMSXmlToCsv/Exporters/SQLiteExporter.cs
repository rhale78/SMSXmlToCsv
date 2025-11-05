using Microsoft.Data.Sqlite;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters
{
    /// <summary>
    /// SQLite database exporter
    /// </summary>
    public class SQLiteExporter : IMessageExporter
    {
        public async Task ExportAsync(
            List<SmsMessage> messages,
            string outputPath,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null)
        {
            // Ensure .db extension
            if (!outputPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                outputPath += ".db";
            }

            // Delete existing database
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            string connectionString = $"Data Source={outputPath}";

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                // Create schema
                await CreateSchemaAsync(connection, messages);

                // Insert messages
                await InsertMessagesAsync(connection, messages, mmsAttachments);

                // Insert MMS attachments
                if (mmsAttachments != null && mmsAttachments.Count > 0)
                {
                    await InsertMmsAttachmentsAsync(connection, mmsAttachments);
                }

                // Create indexes
                await CreateIndexesAsync(connection);
            }
        }

        private async Task CreateSchemaAsync(SqliteConnection connection, List<SmsMessage> messages)
        {
            using (SqliteCommand command = connection.CreateCommand())
            {
                // Build dynamic column list
                List<string> columns = new List<string>
                {
                    "id INTEGER PRIMARY KEY AUTOINCREMENT",
                    "from_name TEXT",
                    "from_phone TEXT",
                    "to_name TEXT",
                    "to_phone TEXT",
                    "direction TEXT",
                    "date_time TEXT",
                    "unix_timestamp INTEGER",
                    "message_text TEXT"
                };

                // Add additional fields
                if (messages.Count > 0 && messages[0].AdditionalFields.Count > 0)
                {
                    foreach (string key in messages[0].AdditionalFields.Keys)
                    {
                        string columnName = key.ToLowerInvariant().Replace(" ", "_");
                        columns.Add($"{columnName} TEXT");
                    }
                }

                columns.Add("has_mms BOOLEAN DEFAULT 0");

                // Create messages table
                command.CommandText = $@"
                    CREATE TABLE messages (
                        {string.Join(",\n                        ", columns)}
                    )";
                await command.ExecuteNonQueryAsync();

                // Create MMS attachments table
                command.CommandText = @"
                    CREATE TABLE mms_attachments (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        message_id INTEGER,
                        file_name TEXT,
                        file_path TEXT,
                        content_type TEXT,
                        FOREIGN KEY(message_id) REFERENCES messages(id)
                    )";
                await command.ExecuteNonQueryAsync();

                // Create contacts table
                command.CommandText = @"
                    CREATE TABLE contacts (
                        phone TEXT PRIMARY KEY,
                        name TEXT,
                        total_messages INTEGER,
                        first_message_date TEXT,
                        last_message_date TEXT
                    )";
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertMessagesAsync(
            SqliteConnection connection,
            List<SmsMessage> messages,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                using (SqliteCommand command = connection.CreateCommand())
                {
                    // Build parameter list
                    List<string> paramNames = new List<string>
                    {
                        "@from_name", "@from_phone", "@to_name", "@to_phone",
                        "@direction", "@date_time", "@unix_timestamp", "@message_text"
                    };

                    // Add additional field parameters
                    List<string> additionalFieldKeys = new List<string>();
                    if (messages.Count > 0 && messages[0].AdditionalFields.Count > 0)
                    {
                        foreach (string key in messages[0].AdditionalFields.Keys)
                        {
                            additionalFieldKeys.Add(key);
                            paramNames.Add($"@{key.ToLowerInvariant().Replace(" ", "_")}");
                        }
                    }

                    paramNames.Add("@has_mms");

                    command.CommandText = $@"
                        INSERT INTO messages ({string.Join(", ", paramNames.Select(p => p.TrimStart('@')))})
                        VALUES ({string.Join(", ", paramNames)})";

                    // Add parameters
                    foreach (string paramName in paramNames)
                    {
                        command.Parameters.Add(new SqliteParameter(paramName, SqliteType.Text));
                    }

                    // Insert each message
                    foreach (SmsMessage msg in messages)
                    {
                        command.Parameters["@from_name"].Value = msg.FromName;
                        command.Parameters["@from_phone"].Value = msg.FromPhone;
                        command.Parameters["@to_name"].Value = msg.ToName;
                        command.Parameters["@to_phone"].Value = msg.ToPhone;
                        command.Parameters["@direction"].Value = msg.Direction;
                        command.Parameters["@date_time"].Value = msg.DateTime.ToString("yyyy-MM-ddTHH:mm:ss");
                        command.Parameters["@unix_timestamp"].Value = msg.UnixTimestamp;
                        command.Parameters["@message_text"].Value = msg.MessageText;

                        // Additional fields
                        foreach (string key in additionalFieldKeys)
                        {
                            string paramName = $"@{key.ToLowerInvariant().Replace(" ", "_")}";
                            command.Parameters[paramName].Value = msg.AdditionalFields.GetValueOrDefault(key, string.Empty);
                        }

                        // MMS flag
                        bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp);
                        command.Parameters["@has_mms"].Value = hasMms ? 1 : 0;

                        await command.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
            }
        }

        private async Task InsertMmsAttachmentsAsync(
            SqliteConnection connection,
            Dictionary<long, List<MmsAttachment>> mmsAttachments)
        {
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                // Get message IDs
                Dictionary<long, long> timestampToId = new Dictionary<long, long>();
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT id, unix_timestamp FROM messages";
                    using (SqliteDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            long id = reader.GetInt64(0);
                            long timestamp = reader.GetInt64(1);
                            timestampToId[timestamp] = id;
                        }
                    }
                }

                // Insert attachments
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO mms_attachments (message_id, file_name, file_path, content_type)
                        VALUES (@message_id, @file_name, @file_path, @content_type)";

                    command.Parameters.Add(new SqliteParameter("@message_id", SqliteType.Integer));
                    command.Parameters.Add(new SqliteParameter("@file_name", SqliteType.Text));
                    command.Parameters.Add(new SqliteParameter("@file_path", SqliteType.Text));
                    command.Parameters.Add(new SqliteParameter("@content_type", SqliteType.Text));

                    foreach (KeyValuePair<long, List<MmsAttachment>> kvp in mmsAttachments)
                    {
                        if (timestampToId.TryGetValue(kvp.Key, out long messageId))
                        {
                            foreach (MmsAttachment attachment in kvp.Value)
                            {
                                command.Parameters["@message_id"].Value = messageId;
                                command.Parameters["@file_name"].Value = attachment.FileName;
                                command.Parameters["@file_path"].Value = attachment.FilePath;
                                command.Parameters["@content_type"].Value = attachment.ContentType;

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                transaction.Commit();
            }
        }

        private async Task CreateIndexesAsync(SqliteConnection connection)
        {
            using (SqliteCommand command = connection.CreateCommand())
            {
                // Index on phone numbers
                command.CommandText = "CREATE INDEX idx_contact ON messages(from_phone, to_phone)";
                await command.ExecuteNonQueryAsync();

                // Index on timestamp
                command.CommandText = "CREATE INDEX idx_date ON messages(unix_timestamp)";
                await command.ExecuteNonQueryAsync();

                // Index on direction
                command.CommandText = "CREATE INDEX idx_direction ON messages(direction)";
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
