using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MimeKit;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Filtering;
using Serilog;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Imports emails from Google Takeout Mail export (.mbox format).
/// Uses MimeKit for parsing mbox files.
/// </summary>
public class GoogleMailImporter : IDataImporter
{
    private readonly EmailMessageFilter _emailFilter;
    private readonly bool _filterBusinessEmails;
    private readonly bool _removeDuplicates;

    // Track likely user-owned email addresses discovered during import
    private readonly HashSet<string> _userEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public GoogleMailImporter(bool filterBusinessEmails = true, bool removeDuplicates = true)
    {
        _emailFilter = new EmailMessageFilter();
        _filterBusinessEmails = filterBusinessEmails;
        _removeDuplicates = removeDuplicates;
    }

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

    private static string NormalizeDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        string trimmed = name.Trim();

        // If exactly one comma exists, assume "Last, First [Middle]" and normalize
        int firstComma = trimmed.IndexOf(',');
        if (firstComma > 0 && trimmed.IndexOf(',', firstComma + 1) == -1)
        {
            string last = trimmed.Substring(0, firstComma).Trim();
            string firstMiddle = trimmed.Substring(firstComma + 1).Trim();

            if (!string.IsNullOrEmpty(last) && !string.IsNullOrEmpty(firstMiddle))
            {
                trimmed = $"{firstMiddle} {last}";
            }
        }

        // Collapse multiple spaces
        trimmed = Regex.Replace(trimmed, "\\s+", " ");
        return trimmed;
    }

    public async Task<IEnumerable<Message>> ImportAsync(string sourcePath)
    {
        List<Message> messages = new List<Message>();
        int totalMboxFiles = 0;

        Debug.WriteLine($"[GoogleMailImporter] Starting import from: {sourcePath}");

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
            string[] mboxFiles = Directory.GetFiles(mboxPath, "*.mbox", SearchOption.AllDirectories);
            
            if (mboxFiles.Length == 0)
            {
                Debug.WriteLine($"[GoogleMailImporter] ERROR: No .mbox files found in {mboxPath}");
                throw new FileNotFoundException($"No .mbox files found in {mboxPath}");
            }

            totalMboxFiles = mboxFiles.Length;
            Debug.WriteLine($"[GoogleMailImporter] Found {totalMboxFiles} .mbox file(s) to process");

            // Parse all mbox files
            int currentFile = 0;
            foreach (string mboxFile in mboxFiles)
            {
                currentFile++;
                Debug.WriteLine($"[GoogleMailImporter] Processing file {currentFile}/{totalMboxFiles}: {Path.GetFileName(mboxFile)}");
                
                IEnumerable<Message> fileMessages = await ParseMboxFileAsync(mboxFile);
                messages.AddRange(fileMessages);
                
                Debug.WriteLine($"[GoogleMailImporter] File {currentFile}/{totalMboxFiles} complete. Messages so far: {messages.Count}");
            }
        }
        else if (File.Exists(sourcePath) && sourcePath.EndsWith(".mbox", StringComparison.OrdinalIgnoreCase))
        {
            // Single mbox file
            totalMboxFiles = 1;
            Debug.WriteLine($"[GoogleMailImporter] Processing single .mbox file: {Path.GetFileName(sourcePath)}");
            
            IEnumerable<Message> fileMessages = await ParseMboxFileAsync(sourcePath);
            messages.AddRange(fileMessages);
        }
        else
        {
            Debug.WriteLine($"[GoogleMailImporter] ERROR: Could not find .mbox file at {sourcePath}");
            throw new FileNotFoundException($"Could not find .mbox file at {sourcePath}");
        }

        Debug.WriteLine($"[GoogleMailImporter] Import complete. Total messages parsed: {messages.Count} from {totalMboxFiles} file(s)");

        // TEMPORARY: Disable filtering & duplicate removal for diagnostic purposes.
        // Set this to false (or remove block) to restore normal behavior.
        bool disableFilteringTemporarily = true;
        if (disableFilteringTemporarily || Environment.GetEnvironmentVariable("DISABLE_EMAIL_FILTERS") == "1")
        {
            Log.Warning("Email filtering & duplicate removal DISABLED (temporary). Total messages retained: {Count}", messages.Count);
            return messages; // Return unfiltered
        }

        // Apply filtering (normal path)
        int originalCount = messages.Count;
        IEnumerable<Message> filteredMessages = messages;

        if (_filterBusinessEmails)
        {
            Log.Information("Filtering out business and automated emails...");
            filteredMessages = _emailFilter.FilterPersonalEmailsOnly(filteredMessages);
            filteredMessages = _emailFilter.FilterValidEmailAddressesOnly(filteredMessages);
        }

        if (_removeDuplicates)
        {
            Log.Information("Removing duplicate messages...");
            filteredMessages = _emailFilter.RemoveDuplicates(filteredMessages);
        }

        List<Message> finalMessages = filteredMessages.ToList();
        
        if (finalMessages.Count < originalCount)
        {
            Log.Information("Email filtering complete: {OriginalCount} messages ? {FinalCount} messages ({RemovedCount} removed)",
                originalCount, finalMessages.Count, originalCount - finalMessages.Count);
        }

        return finalMessages;
    }

    private async Task<IEnumerable<Message>> ParseMboxFileAsync(string mboxFilePath)
    {
        List<Message> messages = new List<Message>();
        int totalMessages = 0;
        int successfulMessages = 0;
        int failedMessages = 0;
        int encodingErrors = 0;
        int skippedMessages = 0;

        // Buffer skipped log lines to avoid flooding output; still emit individually for diagnostics
        List<string> skippedDetails = new List<string>();

        await Task.Run(() =>
        {
            try
            {
                using FileStream stream = File.OpenRead(mboxFilePath);
                MimeParser parser = new MimeParser(stream, MimeFormat.Mbox);

                // Progress interval will adjust later once we know scale; start with 100
                int progressInterval = 100;

                while (!parser.IsEndOfStream)
                {
                    totalMessages++;
                    try
                    {
                        MimeMessage mimeMessage = parser.ParseMessage();

                        // Update discovered user emails before creating the message
                        UpdateUserEmails(mimeMessage, mboxFilePath);

                        Message? message = ParseMimeMessage(mimeMessage, mboxFilePath, out bool hadEncodingError);
                        
                        if (message != null)
                        {
                            messages.Add(message);
                            successfulMessages++;
                            
                            if (hadEncodingError)
                            {
                                encodingErrors++;
                            }

                            // Adjust interval when crossing 10k parsed
                            if (successfulMessages == 10000)
                            {
                                progressInterval = 1000; // switch to less frequent logging for large imports
                            }

                            if (successfulMessages % progressInterval == 0)
                            {
                                Debug.WriteLine($"[GoogleMailImporter] Parsed {successfulMessages:N0} emails so far...");
                            }
                        }
                        else
                        {
                            failedMessages++;
                            skippedMessages++;
                            string fromAddr = mimeMessage.From.Mailboxes.FirstOrDefault()?.Address ?? "(none)";
                            string toAddr = mimeMessage.To.Mailboxes.FirstOrDefault()?.Address ?? "(none)";
                            string subject = mimeMessage.Subject ?? string.Empty;
                            string reason = "ParseMimeMessage returned null"; // catch already logged deeper if exception
                            string line = $"Skipped email: From={fromAddr} To={toAddr} Subject='{subject}' Reason={reason}";
                            skippedDetails.Add(line);
                            Log.Debug(line);
                            Debug.WriteLine("[GoogleMailImporter] " + line);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedMessages++;
                        skippedMessages++;
                        // Attempt minimal header extraction if possible (parser already advanced; may not be available)
                        string reason = $"Exception: {ex.GetType().Name}: {ex.Message}";
                        string line = $"Skipped raw email (unable to parse MimeMessage). Reason={reason}";
                        skippedDetails.Add(line);
                        Log.Debug(line);
                        Debug.WriteLine("[GoogleMailImporter] " + line);
                        // Only log failures every 100 to reduce noise
                        if (failedMessages % 100 == 0)
                        {
                            Debug.WriteLine($"[GoogleMailImporter] Failed to parse {failedMessages} messages so far. Last error: {ex.Message}");
                        }
                    }
                }

                Debug.WriteLine($"[GoogleMailImporter] File: {Path.GetFileName(mboxFilePath)}");
                Debug.WriteLine($"[GoogleMailImporter]   Total messages encountered: {totalMessages:N0}");
                Debug.WriteLine($"[GoogleMailImporter]   Successfully parsed: {successfulMessages:N0}");
                Debug.WriteLine($"[GoogleMailImporter]   Failed to parse: {failedMessages:N0}");
                Debug.WriteLine($"[GoogleMailImporter]   Encoding errors recovered: {encodingErrors:N0}");
                Debug.WriteLine($"[GoogleMailImporter]   Skipped (not imported): {skippedMessages:N0}");
                if (skippedDetails.Count > 0)
                {
                    Log.Information("Skipped {Count} messages in file {File}. See debug output for details.", skippedMessages, Path.GetFileName(mboxFilePath));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GoogleMailImporter] ERROR parsing file {Path.GetFileName(mboxFilePath)}: {ex.Message}");
                // Skip files that can't be parsed
            }
        });

        return messages;
    }

    private static bool LooksLikeSentFolder(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.ToLowerInvariant();
        // Use word boundaries when possible to avoid matching words like "consent"
        return Regex.IsMatch(lower, @"\b(sent|sent mail|sent items|outbox|draft|drafts)\b");
    }

    private static bool LooksLikeInboxFolder(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.ToLowerInvariant();
        return Regex.IsMatch(lower, @"\b(inbox|received|archive|all mail)\b");
    }

    private void UpdateUserEmails(MimeMessage mimeMessage, string mboxFilePath)
    {
        // 1) Delivered/Original recipient headers often indicate the account's mailbox
        var headerKeys = new[]
        {
            "Delivered-To",
            "X-Delivered-To",
            "X-Original-To",
            "X-Envelope-To",
            "X-Forwarded-To"
        };
        foreach (var key in headerKeys)
        {
            foreach (var header in mimeMessage.Headers.Where(h => h.Field.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                TryAddAddresses(_userEmails, header.Value);
            }
        }

        // 2) For sent/drafts folders, the From address is likely the account address
        if (IsLikelySent(mimeMessage, mboxFilePath))
        {
            var from = mimeMessage.From.Mailboxes.FirstOrDefault()?.Address;
            if (!string.IsNullOrEmpty(from))
            {
                _userEmails.Add(from);
            }
        }

        // 2b) Include Reply-To / Return-Path if they look like user mailboxes (avoids missing alias addresses)
        var replyTo = mimeMessage.ReplyTo.Mailboxes.FirstOrDefault()?.Address;
        if (!string.IsNullOrEmpty(replyTo) && replyTo.Contains('@'))
        {
            _userEmails.Add(replyTo);
        }
        var returnPathHeader = mimeMessage.Headers.FirstOrDefault(h => h.Field.Equals("Return-Path", StringComparison.OrdinalIgnoreCase));
        if (returnPathHeader != null)
        {
            TryAddAddresses(_userEmails, returnPathHeader.Value);
        }

        // 3) If message clearly addressed to a single address that appears in Delivered-To, include it
        // Already captured via headers, so no-op here.
    }

    private static void TryAddAddresses(HashSet<string> set, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            // Use MimeKit parser to extract addresses safely
            if (InternetAddressList.TryParse(raw, out var list))
            {
                foreach (var addr in list.Mailboxes)
                {
                    if (!string.IsNullOrEmpty(addr.Address))
                        set.Add(addr.Address);
                }
            }
            else
            {
                // Fallback: split on commas/semicolons and add simple tokens matching email pattern
                foreach (var token in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = token.Trim();
                    if (trimmed.Contains('@')) set.Add(trimmed);
                }
            }
        }
        catch
        {
            // Ignore bad header formats
        }
    }

    private static bool IsLikelySent(MimeMessage mimeMessage, string mboxFilePath)
    {
        // Check labels/headers
        string[] headerKeys = new[] { "X-GM-LABELS", "X-Gmail-Labels", "X-GMAIL-LABELS", "X-Labels", "X-Folder", "X-Mailbox" };
        foreach (var key in headerKeys)
        {
            var val = mimeMessage.Headers[key];
            if (!string.IsNullOrEmpty(val) && LooksLikeSentFolder(val))
                return true;
        }
        // Check path
        string fileName = Path.GetFileNameWithoutExtension(mboxFilePath);
        string fullPath = mboxFilePath.Replace('\\', '/');
        return LooksLikeSentFolder(fileName) || LooksLikeSentFolder(fullPath);
    }

    private MessageDirection InferDirection(MimeMessage mimeMessage, string mboxFilePath)
    {
        // 1) Prefer exporter-provided labels/headers
        string[] headerKeys = new[]
        {
            "X-GM-LABELS",        // Gmail IMAP labels
            "X-Gmail-Labels",     // Google Takeout common
            "X-GMAIL-LABELS",     // case variants
            "X-Labels",
            "X-Folder",
            "X-Mailbox"
        };
        foreach (var key in headerKeys)
        {
            var headerValue = mimeMessage.Headers[key];
            if (!string.IsNullOrEmpty(headerValue))
            {
                if (LooksLikeSentFolder(headerValue))
                    return MessageDirection.Sent;
                if (LooksLikeInboxFolder(headerValue))
                    return MessageDirection.Received;
            }
        }

        // 2) Use the mbox filename/path
        string fileName = Path.GetFileNameWithoutExtension(mboxFilePath);
        string fullPath = mboxFilePath.Replace('\\', '/');
        if (LooksLikeSentFolder(fileName) || LooksLikeSentFolder(fullPath))
            return MessageDirection.Sent;
        if (LooksLikeInboxFolder(fileName) || LooksLikeInboxFolder(fullPath))
            return MessageDirection.Received;

        // 3) Address-based inference using discovered user emails
        string? fromAddr = mimeMessage.From.Mailboxes.FirstOrDefault()?.Address;
        var toAddrs = mimeMessage.To.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrEmpty(a)).ToList();
        var ccAddrs = mimeMessage.Cc.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrEmpty(a)).ToList();
        var bccAddrs = mimeMessage.Bcc.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrEmpty(a)).ToList();

        if (!string.IsNullOrEmpty(fromAddr) && _userEmails.Contains(fromAddr))
            return MessageDirection.Sent;
        if (toAddrs.Any(a => _userEmails.Contains(a)) || ccAddrs.Any(a => _userEmails.Contains(a)) || bccAddrs.Any(a => _userEmails.Contains(a)))
            return MessageDirection.Received;

        // 4) Fallback heuristic
        return MessageDirection.Received;
    }

    private static MailboxAddress? ChoosePrimaryNonUserRecipient(MimeMessage mimeMessage, HashSet<string> userEmails)
    {
        // Prefer To, then Cc, then Bcc, excluding any addresses that match user emails
        IEnumerable<MailboxAddress> Enumerate()
        {
            foreach (var m in mimeMessage.To.Mailboxes) yield return m;
            foreach (var m in mimeMessage.Cc.Mailboxes) yield return m;
            foreach (var m in mimeMessage.Bcc.Mailboxes) yield return m;
        }

        var firstNonUser = Enumerate().FirstOrDefault(m => !string.IsNullOrEmpty(m.Address) && !userEmails.Contains(m.Address));
        if (firstNonUser != null) return firstNonUser;

        // Fall back to Reply-To if available
        var replyTo = mimeMessage.ReplyTo.Mailboxes.FirstOrDefault(m => !string.IsNullOrEmpty(m.Address) && !userEmails.Contains(m.Address));
        if (replyTo != null) return replyTo;

        // As last resort, return first To (could be user in weird cases)
        return mimeMessage.To.Mailboxes.FirstOrDefault();
    }

    private Message? ParseMimeMessage(MimeMessage mimeMessage, string mboxFilePath, out bool hadEncodingError)
    {
        hadEncodingError = false;
        try
        {
            // Collect addresses for later logic
            string? fromAddr = mimeMessage.From.Mailboxes.FirstOrDefault()?.Address;
            var toAddrs = mimeMessage.To.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrEmpty(a)).ToList();
            var ccAddrs = mimeMessage.Cc.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrEmpty(a)).ToList();
            var bccAddrs = mimeMessage.Bcc.Mailboxes.Select(m => m.Address).Where(a => !string.IsNullOrEmpty(a)).ToList();

            // Find a user-owned address present in this message (first match)
            string? userAddressInMessage = new[] { fromAddr }
                .Concat(toAddrs).Concat(ccAddrs).Concat(bccAddrs)
                .FirstOrDefault(a => a != null && _userEmails.Contains(a));

            // Infer direction using existing heuristic first
            var direction = InferDirection(mimeMessage, mboxFilePath);

            // Adjust direction with stronger rules if userAddress found
            if (userAddressInMessage != null)
            {
                bool fromIsUser = fromAddr != null && _userEmails.Contains(fromAddr);
                bool anyRecipientIsUser = toAddrs.Any(a => _userEmails.Contains(a)) || ccAddrs.Any(a => _userEmails.Contains(a)) || bccAddrs.Any(a => _userEmails.Contains(a));

                if (fromIsUser && !anyRecipientIsUser)
                {
                    direction = MessageDirection.Sent; // user sending to others
                }
                else if (!fromIsUser && anyRecipientIsUser)
                {
                    direction = MessageDirection.Received; // user receiving
                }
                // If ambiguous (user both sender and recipient, e.g. BCC/self), keep previous heuristic
            }

            // Choose sender mailbox (prefer Sender header for received mail)
            MailboxAddress? rawSenderMailbox = (direction == MessageDirection.Received && mimeMessage.Sender != null)
                ? mimeMessage.Sender
                : mimeMessage.From.Mailboxes.FirstOrDefault();

            // Ensure sender represents non-user for received messages
            if (direction == MessageDirection.Received && rawSenderMailbox != null && rawSenderMailbox.Address != null && _userEmails.Contains(rawSenderMailbox.Address))
            {
                // If the sender is the user (rare for received classification), flip to Sent
                direction = MessageDirection.Sent;
            }

            // Re-evaluate sender mailbox after potential flip
            rawSenderMailbox = (direction == MessageDirection.Received && mimeMessage.Sender != null)
                ? mimeMessage.Sender
                : mimeMessage.From.Mailboxes.FirstOrDefault();

            // Build From contact
            string fromName = rawSenderMailbox?.Name ?? rawSenderMailbox?.Address ?? (direction == MessageDirection.Sent ? "Me" : "Unknown");
            string fromEmail = rawSenderMailbox?.Address ?? string.Empty;
            fromName = NormalizeDisplayName(fromName);

            // If direction is Sent and sender isn't recognized as user but a user address exists in recipients, use that user address as From
            if (direction == MessageDirection.Sent && (fromEmail == string.Empty || !_userEmails.Contains(fromEmail)) && userAddressInMessage != null)
            {
                fromEmail = userAddressInMessage;
                fromName = "Me";
            }

            Contact fromContact = !string.IsNullOrEmpty(fromEmail)
                ? Contact.FromEmail(fromName, fromEmail)
                : Contact.FromName(fromName);

            // Determine recipient mailbox
            MailboxAddress? toMailbox;
            if (direction == MessageDirection.Sent)
            {
                // Choose primary non-user recipient
                toMailbox = ChoosePrimaryNonUserRecipient(mimeMessage, _userEmails);
            }
            else // Received
            {
                // For received messages the user should be the recipient ("Me"), other party is sender
                // Find a user address among recipients
                string? userRecipient = toAddrs.Concat(ccAddrs).Concat(bccAddrs).FirstOrDefault(a => _userEmails.Contains(a));
                if (userRecipient != null)
                {
                    toMailbox = new MailboxAddress("Me", userRecipient);
                }
                else
                {
                    // Fallback: first To mailbox or userAddressInMessage if only in From
                    toMailbox = mimeMessage.To.Mailboxes.FirstOrDefault() ?? (userAddressInMessage != null ? new MailboxAddress("Me", userAddressInMessage) : null);
                }
            }

            string toName = toMailbox?.Name ?? (direction == MessageDirection.Received ? "Me" : toMailbox?.Address ?? "Unknown");
            string toEmail = toMailbox?.Address ?? string.Empty;
            toName = NormalizeDisplayName(toName);

            Contact toContact = !string.IsNullOrEmpty(toEmail)
                ? Contact.FromEmail(toName, toEmail)
                : Contact.FromName(toName);

            // Timestamp
            DateTimeOffset timestamp = mimeMessage.Date;

            // Subject and body extraction
            string subject = mimeMessage.Subject ?? string.Empty;
            string body = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(mimeMessage.TextBody))
                {
                    body = mimeMessage.TextBody;
                }
                else if (!string.IsNullOrEmpty(mimeMessage.HtmlBody))
                {
                    body = mimeMessage.HtmlBody;
                }
            }
            catch (ArgumentException ex)
            {
                hadEncodingError = true;
                Debug.WriteLine($"[GoogleMailImporter] Encoding error in message (Subject: '{subject}'): {ex.Message}");
                if (mimeMessage.Body is TextPart textPart)
                {
                    body = textPart.Text ?? string.Empty;
                }
                else if (mimeMessage.Body is Multipart multipart)
                {
                    var firstTextPart = multipart.OfType<TextPart>().FirstOrDefault();
                    body = firstTextPart?.Text ?? string.Empty;
                }
            }

            string content = string.IsNullOrEmpty(subject) ? body : $"Subject: {subject}\n\n{body}";

            // Attachments
            List<MediaAttachment> attachments = new();
            foreach (var attachment in mimeMessage.Attachments)
            {
                if (attachment is MimePart mimePart)
                {
                    string fileName = mimePart.FileName ?? $"attachment_{Guid.NewGuid():N}";
                    string mimeType = mimePart.ContentType?.MimeType ?? "application/octet-stream";
                    attachments.Add(new MediaAttachment(fileName, mimeType));
                }
            }

            return new EmailMessage(
                SourceName,
                fromContact,
                toContact,
                timestamp,
                content,
                direction,
                attachments);
        }
        catch (Exception ex)
        {
            // Detailed skip logging
            string fromAddr = mimeMessage.From.Mailboxes.FirstOrDefault()?.Address ?? "(none)";
            string toAddr = mimeMessage.To.Mailboxes.FirstOrDefault()?.Address ?? "(none)";
            string subject = mimeMessage.Subject ?? string.Empty;
            string reason = $"Exception in ParseMimeMessage: {ex.GetType().Name}: {ex.Message}";
            Log.Debug("Skipped email: From={From} To={To} Subject='{Subject}' Reason={Reason}", fromAddr, toAddr, subject, reason);
            Debug.WriteLine($"[GoogleMailImporter] Skipped email: From={fromAddr} To={toAddr} Subject='{subject}' Reason={reason}");
            return null;
        }
    }
}
