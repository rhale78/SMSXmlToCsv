using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Imports emails from Google Takeout Mail export (.mbox format).
/// Uses MimeKit for parsing mbox files.
/// </summary>
public class GoogleMailImporter : IDataImporter
{
    public string SourceName => "Google Mail (Mbox)";

    public bool CanImport(string sourcePath)
    {
        // Check if it's an mbox file
        if (File.Exists(sourcePath) && sourcePath.EndsWith(".mbox", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if it's a directory containing mbox files
        if (Directory.Exists(sourcePath))
        {
            string mailPath = Path.Combine(sourcePath, "Mail");
            if (Directory.Exists(mailPath))
            {
                return Directory.GetFiles(mailPath, "*.mbox").Any();
            }
            return Directory.GetFiles(sourcePath, "*.mbox").Any();
        }

        return false;
    }

    public async Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();

        // Look for .mbox files
        string mboxPath = sourcePath;
        
        if (Directory.Exists(sourcePath))
        {
            // If it's a directory, look for Mail folder or .mbox files
            string mailPath = Path.Combine(sourcePath, "Mail");
            if (Directory.Exists(mailPath))
            {
                mboxPath = mailPath;
            }

            // Find all .mbox files
            string[] mboxFiles = Directory.GetFiles(mboxPath, "*.mbox", SearchOption.TopDirectoryOnly);
            
            if (mboxFiles.Length == 0)
            {
                throw new FileNotFoundException($"No .mbox files found in {mboxPath}");
            }

            // Parse all mbox files
            foreach (string mboxFile in mboxFiles)
            {
                IEnumerable<Message> fileMessages = await ParseMboxFileAsync(mboxFile);
                messages.AddRange(fileMessages);
            }
        }
        else if (File.Exists(sourcePath) && sourcePath.EndsWith(".mbox", StringComparison.OrdinalIgnoreCase))
        {
            // Single mbox file
            IEnumerable<Message> fileMessages = await ParseMboxFileAsync(sourcePath);
            messages.AddRange(fileMessages);
        }
        else
        {
            throw new FileNotFoundException($"Could not find .mbox file at {sourcePath}");
        }

        return messages;
    }

    private async Task<IEnumerable<Message>> ParseMboxFileAsync(string mboxFilePath)
    {
        List<Message> messages = new List<Message>();

        await Task.Run(() =>
        {
            try
            {
                using FileStream stream = File.OpenRead(mboxFilePath);
                MimeParser parser = new MimeParser(stream, MimeFormat.Mbox);

                while (!parser.IsEndOfStream)
                {
                    MimeMessage mimeMessage = parser.ParseMessage();
                    Message? message = ParseMimeMessage(mimeMessage);
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }
            catch (Exception)
            {
                // Skip files that can't be parsed
            }
        });

        return messages;
    }

    private Message? ParseMimeMessage(MimeMessage mimeMessage)
    {
        try
        {
            // Get sender
            MailboxAddress? fromMailbox = mimeMessage.From.Mailboxes.FirstOrDefault();
            string fromName = fromMailbox?.Name ?? fromMailbox?.Address ?? "Unknown";
            string fromEmail = fromMailbox?.Address ?? string.Empty;
            
            Contact fromContact = !string.IsNullOrEmpty(fromEmail)
                ? Contact.FromEmail(fromName, fromEmail)
                : Contact.FromName(fromName);

            // Get recipients
            List<MailboxAddress> toMailboxes = mimeMessage.To.Mailboxes.ToList();
            MailboxAddress? toMailbox = toMailboxes.FirstOrDefault();
            string toName = toMailbox?.Name ?? toMailbox?.Address ?? "Me";
            string toEmail = toMailbox?.Address ?? string.Empty;
            
            Contact toContact = !string.IsNullOrEmpty(toEmail)
                ? Contact.FromEmail(toName, toEmail)
                : Contact.FromName(toName);

            // Get timestamp
            DateTimeOffset timestamp = mimeMessage.Date;

            // Get subject and body
            string subject = mimeMessage.Subject ?? string.Empty;
            string body = string.Empty;

            // Try to get plain text body first, fall back to HTML
            if (!string.IsNullOrEmpty(mimeMessage.TextBody))
            {
                body = mimeMessage.TextBody;
            }
            else if (!string.IsNullOrEmpty(mimeMessage.HtmlBody))
            {
                body = mimeMessage.HtmlBody;
            }

            // Combine subject and body
            string content = string.IsNullOrEmpty(subject) 
                ? body 
                : $"Subject: {subject}\n\n{body}";

            // Get attachments
            List<MediaAttachment> attachments = new List<MediaAttachment>();
            foreach (MimeEntity attachment in mimeMessage.Attachments)
            {
                if (attachment is MimePart mimePart)
                {
                    string fileName = mimePart.FileName ?? $"attachment_{Guid.NewGuid():N}";
                    string mimeType = mimePart.ContentType?.MimeType ?? "application/octet-stream";
                    attachments.Add(new MediaAttachment(fileName, mimeType));
                }
            }

            return new Message(
                SourceName,
                fromContact,
                toContact,
                timestamp,
                content,
                MessageDirection.Unknown, // Email direction can be ambiguous
                attachments);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
