using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Placeholder for Signal Desktop backup importer.
/// 
/// IMPORTANT: This importer requires external decryption tools or libraries
/// to decrypt Signal's encrypted database backup. Signal uses strong encryption
/// and requires the user's 30-word backup passphrase to decrypt the backup.
/// 
/// Implementation requirements:
/// 1. User must provide their 30-word Signal backup passphrase
/// 2. Requires external tool (e.g., signal-back) or decryption library
/// 3. Once decrypted, the SQLite database can be queried for messages
/// 4. Tables of interest: sms, mms, part (for attachments)
/// 
/// This is NOT IMPLEMENTED as it requires:
/// - Secure passphrase handling
/// - External decryption tools/libraries
/// - SQLite database parsing after decryption
/// 
/// For security and ethical reasons, this importer does not attempt to
/// break Signal's encryption - it only works with user-provided credentials.
/// </summary>
public class SignalBackupImporter : IDataImporter
{
    public string SourceName => "Signal Desktop Backup";

    public Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        // Return empty collection with a warning logged
        // This is a placeholder that needs proper implementation with external decryption tools
        return Task.FromResult<IEnumerable<Message>>(new List<Message>());
    }
}
