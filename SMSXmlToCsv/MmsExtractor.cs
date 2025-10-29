using System.Text;
using System.Xml;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv
{
    /// <summary>
    /// Handles MMS attachment extraction and organization
    /// </summary>
    public class MmsExtractor
    {
        private readonly string _basePath;
        private readonly string _contactsFolderName;
        private readonly string _userName;
        private readonly object _lock = new object();

        public MmsExtractor(string basePath, string contactsFolderName, string userName)
        {
            _basePath = basePath;
            _contactsFolderName = contactsFolderName;
            _userName = userName;
        }

        /// <summary>
        /// Extract MMS attachments from XML and save to contact-specific folder structure
        /// </summary>
        public async Task<Dictionary<long, List<MmsAttachment>>> ExtractMmsAttachmentsAsync(string xmlFilePath)
        {
            Dictionary<long, List<MmsAttachment>> mmsData = new Dictionary<long, List<MmsAttachment>>();
            int mmsCount = 0;
            int skippedCount = 0;

            Console.WriteLine("\n?? Extracting MMS attachments...");

            using (FileStream fs = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            using (XmlReader reader = XmlReader.Create(sr, new XmlReaderSettings
            {
                Async = true,
                IgnoreWhitespace = true,
                IgnoreComments = true
            }))
            {
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "mms")
                    {
                        (MmsData? mms, int skipped) = await ParseMmsElementAsync(reader);
                        if (mms != null && mms.Attachments.Count > 0)
                        {
                            mmsData[mms.Timestamp] = mms.Attachments;
                            mmsCount++;
                            skippedCount += skipped;

                            if (mmsCount % 50 == 0)
                            {
                                Console.Write($"\r  ?? Extracted {mmsCount} MMS ({skippedCount} skipped - already exist)...    ");
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"\r? Extracted {mmsCount} MMS messages ({skippedCount} files skipped - already exist)    ");
            return mmsData;
        }

        /// <summary>
        /// Parse a single MMS element from XML
        /// </summary>
        private async Task<(MmsData?, int skippedCount)> ParseMmsElementAsync(XmlReader reader)
        {
            string? address = reader.GetAttribute("address");
            string? contactName = reader.GetAttribute("contact_name");
            string? date = reader.GetAttribute("date");
            string? msgBox = reader.GetAttribute("msg_box");

            if (string.IsNullOrEmpty(date))
            {
                return (null, 0);
            }

            long timestamp = long.Parse(date);
            bool isSent = msgBox == "2"; // 2 = sent, 1 = received
            string contact = contactName ?? address ?? "Unknown";

            MmsData mmsData = new MmsData
            {
                Timestamp = timestamp,
                ContactName = contact,
                IsSent = isSent,
                Attachments = new List<MmsAttachment>()
            };

            int skippedCount = 0;

            // Read the parts of the MMS
            int depth = reader.Depth;
            while (await reader.ReadAsync())
            {
                if (reader.Depth <= depth)
                {
                    break; // Exited the mms element
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "part")
                {
                    (MmsAttachment? attachment, bool wasSkipped) = ParseMmsPart(reader, mmsData);
                    if (attachment != null)
                    {
                        mmsData.Attachments.Add(attachment);
                        if (wasSkipped)
                        {
                            skippedCount++;
                        }
                    }
                }
            }

            return (mmsData, skippedCount);
        }

        /// <summary>
        /// Parse an MMS part (attachment)
        /// </summary>
        private (MmsAttachment?, bool wasSkipped) ParseMmsPart(XmlReader reader, MmsData mmsData)
        {
            string? contentType = reader.GetAttribute("ct");
            string? fileName = reader.GetAttribute("cl") ?? reader.GetAttribute("name");
            string? data = reader.GetAttribute("data");
            string? text = reader.GetAttribute("text");

            // Skip SMIL files (they're just layout/presentation instructions)
            if (!string.IsNullOrEmpty(contentType) &&
                (contentType.Contains("application/smil") || contentType.Contains("smil")))
            {
                return (null, false);
            }

            // Skip text-only parts - they should be in the main message body
            // However, if there's text WITH multimedia, we might want to save it
            if (string.IsNullOrEmpty(contentType) || contentType.StartsWith("text/plain"))
            {
                // Only save text if it's part of a multimedia message with actual attachments
                if (!string.IsNullOrEmpty(text) && mmsData.Attachments.Count > 0)
                {
                    // Save as a .txt file alongside the media
                    return SaveMmsTextPart(text, mmsData);
                }
                return (null, false);
            }

            // Skip if no actual data
            if (string.IsNullOrEmpty(data) && string.IsNullOrEmpty(text))
            {
                return (null, false);
            }

            // Determine file extension from content type
            string extension = GetExtensionFromContentType(contentType);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"attachment{extension}";
            }
            else if (!fileName.Contains('.'))
            {
                fileName += extension;
            }

            // Create sanitized file name with timestamp
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(mmsData.Timestamp).DateTime;
            string sanitizedContact = SanitizeFolderName(mmsData.ContactName);
            string direction = mmsData.IsSent ? "Sent" : "Received";
            string timestamp = dateTime.ToString("yyyyMMdd_HHmmss");
            string safeFileName = $"{timestamp}_{SanitizeFileName(fileName)}";

            // Create directory structure in Contacts folder
            string contactFolder = Path.Combine(_basePath, _contactsFolderName, sanitizedContact, "MMS", direction);
            Directory.CreateDirectory(contactFolder);

            // Check if file already exists (skip if it does)
            string filePath = Path.Combine(contactFolder, safeFileName);

            if (File.Exists(filePath))
            {
                // File already exists, skip extraction but return the attachment info
                return (new MmsAttachment
                {
                    FileName = safeFileName,
                    ContentType = contentType,
                    FilePath = Path.Combine("MMS", direction, safeFileName)
                }, true); // wasSkipped = true
            }

            // Save the attachment
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    byte[] bytes = Convert.FromBase64String(data);
                    File.WriteAllBytes(filePath, bytes);
                }
                else if (!string.IsNullOrEmpty(text))
                {
                    File.WriteAllText(filePath, text, Encoding.UTF8);
                }

                // Set file creation and modification time to match message date/time
                File.SetCreationTime(filePath, dateTime);
                File.SetLastWriteTime(filePath, dateTime);

                return (new MmsAttachment
                {
                    FileName = safeFileName,
                    ContentType = contentType,
                    FilePath = Path.Combine("MMS", direction, safeFileName)
                }, false); // wasSkipped = false
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n??  Warning: Failed to save MMS attachment: {ex.Message}");
                return (null, false);
            }
        }

        /// <summary>
        /// Save text part of an MMS message
        /// </summary>
        private (MmsAttachment?, bool wasSkipped) SaveMmsTextPart(string text, MmsData mmsData)
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(mmsData.Timestamp).DateTime;
            string sanitizedContact = SanitizeFolderName(mmsData.ContactName);
            string direction = mmsData.IsSent ? "Sent" : "Received";
            string timestamp = dateTime.ToString("yyyyMMdd_HHmmss");
            string safeFileName = $"{timestamp}_message.txt";

            // Create directory structure in Contacts folder
            string contactFolder = Path.Combine(_basePath, _contactsFolderName, sanitizedContact, "MMS", direction);
            Directory.CreateDirectory(contactFolder);

            string filePath = Path.Combine(contactFolder, safeFileName);

            // Check if file already exists
            if (File.Exists(filePath))
            {
                return (new MmsAttachment
                {
                    FileName = safeFileName,
                    ContentType = "text/plain",
                    FilePath = Path.Combine("MMS", direction, safeFileName)
                }, true);
            }

            try
            {
                // Save the text with UTF-8 encoding
                File.WriteAllText(filePath, text, Encoding.UTF8);

                // Set file timestamps
                File.SetCreationTime(filePath, dateTime);
                File.SetLastWriteTime(filePath, dateTime);

                return (new MmsAttachment
                {
                    FileName = safeFileName,
                    ContentType = "text/plain",
                    FilePath = Path.Combine("MMS", direction, safeFileName)
                }, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n??  Warning: Failed to save MMS text part: {ex.Message}");
                return (null, false);
            }
        }

        /// <summary>
        /// Get file extension from MIME content type
        /// </summary>
        private string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                string ct when ct.Contains("image/jpeg") => ".jpg",
                string ct when ct.Contains("image/jpg") => ".jpg",
                string ct when ct.Contains("image/png") => ".png",
                string ct when ct.Contains("image/gif") => ".gif",
                string ct when ct.Contains("image/bmp") => ".bmp",
                string ct when ct.Contains("image/webp") => ".webp",
                string ct when ct.Contains("video/mp4") => ".mp4",
                string ct when ct.Contains("video/3gpp") => ".3gp",
                string ct when ct.Contains("video/mpeg") => ".mpg",
                string ct when ct.Contains("audio/mpeg") => ".mp3",
                string ct when ct.Contains("audio/mp4") => ".m4a",
                string ct when ct.Contains("audio/3gpp") => ".3ga",
                string ct when ct.Contains("audio/amr") => ".amr",
                string ct when ct.Contains("application/pdf") => ".pdf",
                string ct when ct.Contains("text/") => ".txt",
                string ct when ct.Contains("application/vcard") => ".vcf",
                _ => ".dat"
            };
        }

        /// <summary>
        /// Sanitize a folder name to be file-system safe
        /// </summary>
        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unknown";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

            // Remove leading/trailing spaces and dots (Windows doesn't allow these)
            sanitized = sanitized.Trim(' ', '.');

            // Replace spaces with underscores for consistency
            sanitized = sanitized.Replace(' ', '_');

            // Remove any multiple consecutive underscores
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');

            // Limit length and ensure no trailing special chars after truncation
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50).TrimEnd('_', '.', ' ');
            }

            return string.IsNullOrEmpty(sanitized) ? "Unknown" : sanitized;
        }

        /// <summary>
        /// Sanitize a file name to be file-system safe
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "attachment.dat";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

            // Ensure no trailing periods or spaces
            sanitized = sanitized.TrimEnd(' ', '.');

            return string.IsNullOrEmpty(sanitized) ? "attachment.dat" : sanitized;
        }

        /// <summary>
        /// Internal class to hold MMS data during parsing
        /// </summary>
        private class MmsData
        {
            public long Timestamp { get; set; }
            public string ContactName { get; set; } = string.Empty;
            public bool IsSent { get; set; }
            public List<MmsAttachment> Attachments { get; set; } = new List<MmsAttachment>();
        }
    }
}
