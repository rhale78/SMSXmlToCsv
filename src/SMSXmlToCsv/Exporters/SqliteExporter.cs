using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SMSXmlToCsv.Exporters;
using SMSXmlToCsv.Models;
using Serilog;

namespace SMSXmlToCsv.Exporters;

/// <summary>
/// Exports messages to SQLite database format
/// </summary>
public class SqliteExporter : IDataExporter
{
    public string FileExtension => ".db";

    public async Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName)
    {
        List<Message> messageList = messages.ToList();
        Log.Information("Starting SQLite export for {MessageCount} messages", messageList.Count);

        string outputPath = Path.Combine(outputDirectory, $"{baseFileName}{FileExtension}");

        // Delete existing database if it exists
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        string connectionString = $"Data Source={outputPath}";

        using (SqliteConnection connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();

            // Create schema
            await CreateSchemaAsync(connection);

            // Insert data
            await InsertDataAsync(connection, messageList);
        }

        Log.Information("SQLite export completed: {OutputPath}", outputPath);
    }

    private async Task CreateSchemaAsync(SqliteConnection connection)
    {
        string createContactsTable = @"
            CREATE TABLE IF NOT EXISTS Contacts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                UNIQUE(Name)
            );";

        string createMessagesTable = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SourceApplication TEXT NOT NULL,
                FromContactId INTEGER NOT NULL,
                ToContactId INTEGER NOT NULL,
                TimestampUtc TEXT NOT NULL,
                Body TEXT,
                Direction TEXT NOT NULL,
                FOREIGN KEY (FromContactId) REFERENCES Contacts(Id),
                FOREIGN KEY (ToContactId) REFERENCES Contacts(Id)
            );";

        string createAttachmentsTable = @"
            CREATE TABLE IF NOT EXISTS Attachments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MessageId INTEGER NOT NULL,
                FileName TEXT,
                MimeType TEXT,
                FilePath TEXT,
                FOREIGN KEY (MessageId) REFERENCES Messages(Id)
            );";

        string createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_messages_timestamp ON Messages(TimestampUtc);
            CREATE INDEX IF NOT EXISTS idx_messages_from ON Messages(FromContactId);
            CREATE INDEX IF NOT EXISTS idx_messages_to ON Messages(ToContactId);
            CREATE INDEX IF NOT EXISTS idx_messages_direction ON Messages(Direction);
            CREATE INDEX IF NOT EXISTS idx_attachments_message ON Attachments(MessageId);";

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = createContactsTable;
            await command.ExecuteNonQueryAsync();

            command.CommandText = createMessagesTable;
            await command.ExecuteNonQueryAsync();

            command.CommandText = createAttachmentsTable;
            await command.ExecuteNonQueryAsync();

            command.CommandText = createIndexes;
            await command.ExecuteNonQueryAsync();
        }

        Log.Debug("SQLite schema created successfully");
    }

    private async Task<long> GetOrCreateContactIdAsync(SqliteConnection connection, Contact contact, Dictionary<string, long> contactCache)
    {
        string contactName = contact?.Name ?? "Unknown";

        if (contactCache.TryGetValue(contactName, out long cachedId))
        {
            return cachedId;
        }

        // Try to find existing contact
        string selectSql = "SELECT Id FROM Contacts WHERE Name = @Name";

        using (SqliteCommand selectCommand = connection.CreateCommand())
        {
            selectCommand.CommandText = selectSql;
            selectCommand.Parameters.AddWithValue("@Name", contactName);

            object? result = await selectCommand.ExecuteScalarAsync();

            if (result != null)
            {
                long existingId = Convert.ToInt64(result);
                contactCache[contactName] = existingId;
                return existingId;
            }
        }

        // Insert new contact
        string insertSql = "INSERT INTO Contacts (Name) VALUES (@Name); SELECT last_insert_rowid();";

        using (SqliteCommand insertCommand = connection.CreateCommand())
        {
            insertCommand.CommandText = insertSql;
            insertCommand.Parameters.AddWithValue("@Name", contactName);

            object? newIdResult = await insertCommand.ExecuteScalarAsync();
            long newId = Convert.ToInt64(newIdResult!);
            contactCache[contactName] = newId;

            return newId;
        }
    }

    private async Task InsertDataAsync(SqliteConnection connection, List<Message> messages)
    {
        Dictionary<string, long> contactCache = new Dictionary<string, long>();

        using (SqliteTransaction transaction = connection.BeginTransaction())
        {
            foreach (Message message in messages)
            {
                // Get or create contact IDs
                long fromContactId = await GetOrCreateContactIdAsync(connection, message.From, contactCache);
                long toContactId = await GetOrCreateContactIdAsync(connection, message.To, contactCache);

                // Insert message
                string insertMessageSql = @"
                    INSERT INTO Messages (SourceApplication, FromContactId, ToContactId, TimestampUtc, Body, Direction)
                    VALUES (@SourceApplication, @FromContactId, @ToContactId, @TimestampUtc, @Body, @Direction);
                    SELECT last_insert_rowid();";

                long messageId;

                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = insertMessageSql;
                    command.Parameters.AddWithValue("@SourceApplication", message.SourceApplication);
                    command.Parameters.AddWithValue("@FromContactId", fromContactId);
                    command.Parameters.AddWithValue("@ToContactId", toContactId);
                    command.Parameters.AddWithValue("@TimestampUtc", message.TimestampUtc.ToString("o"));
                    command.Parameters.AddWithValue("@Body", message.Body ?? string.Empty);
                    command.Parameters.AddWithValue("@Direction", message.Direction.ToString());

                    object? result = await command.ExecuteScalarAsync();
                    messageId = Convert.ToInt64(result!);
                }

                // Insert attachments
                foreach (MediaAttachment attachment in message.Attachments)
                {
                    string insertAttachmentSql = @"
                        INSERT INTO Attachments (MessageId, FileName, MimeType, FilePath)
                        VALUES (@MessageId, @FileName, @MimeType, @FilePath);";

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = insertAttachmentSql;
                        command.Parameters.AddWithValue("@MessageId", messageId);
                        command.Parameters.AddWithValue("@FileName", attachment.FileName ?? string.Empty);
                        command.Parameters.AddWithValue("@MimeType", attachment.MimeType ?? string.Empty);
                        command.Parameters.AddWithValue("@FilePath", attachment.OriginalSourcePath ?? string.Empty);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }

            await transaction.CommitAsync();
        }

        Log.Debug("Inserted {MessageCount} messages into SQLite database", messages.Count);
    }
}
