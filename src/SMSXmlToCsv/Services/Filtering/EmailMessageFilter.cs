using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using SMSXmlToCsv.Models;
using Serilog;

namespace SMSXmlToCsv.Services.Filtering;

/// <summary>
/// Filters email messages to focus on personal emails and remove business/automated messages
/// </summary>
public class EmailMessageFilter
{
    // Known business/automated sender domains (brands, marketing platforms, etc.)
    private static readonly HashSet<string> BusinessDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Streaming services
        "netflix.com", "hulu.com", "disneyplus.com", "hbomax.com", "primevideo.com",
        "spotify.com", "youtube.com",
        
        // E-commerce / brands
        "amazon.com", "ebay.com", "etsy.com", "shopify.com", "walmart.com", "target.com",
        
        // Social media notifications
        "facebook.com", "facebookmail.com", "instagram.com", "x.com", "twitter.com", "linkedin.com",
        "tiktok.com", "snapchat.com", "pinterest.com",
        
        // Financial
        "paypal.com", "venmo.com", "stripe.com", "square.com", "chase.com", "bankofamerica.com",
        "wellsfargo.com", "citi.com", "amex.com", "americanexpress.com", "discover.com", "capitalone.com",
        
        // Travel
        "airbnb.com", "uber.com", "lyft.com", "expedia.com", "booking.com", "hotels.com",
        
        // Marketing/Newsletters platforms
        "mailchimp.com", "sendgrid.net", "constantcontact.com", "campaignmonitor.com"
    };

    // Major consumer mailbox providers - do NOT treat as business by domain alone
    private static readonly HashSet<string> ConsumerMailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "outlook.com", "hotmail.com", "live.com", "msn.com",
        "yahoo.com", "icloud.com", "me.com", "proton.me", "protonmail.com", "gmx.com", "aol.com"
    };

    private static readonly HashSet<string> BusinessKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "noreply", "no-reply", "donotreply", "do-not-reply", "unsubscribe",
        "notifications", "newsletter", "automated", "auto-reply",
        "mailer-daemon", "postmaster", "support", "help", "admin",
        "sales", "billing", "invoice", "receipt", "order", "shipping", "delivery",
        "alert", "notification", "service", "team"
    };

    private static readonly Regex EmailAddressRegex = new(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled);

    /// <summary>
    /// Filter out business and automated emails, keeping only personal emails
    /// </summary>
    public IEnumerable<Message> FilterPersonalEmailsOnly(IEnumerable<Message> messages)
    {
        List<Message> messageList = messages.ToList();
        int originalCount = messageList.Count;

        // Keep messages where the sender appears to be a personal email address
        List<Message> filtered = messageList
            .Where(m => !IsBusinessEmailAddress(m.From))
            .ToList();

        int removed = originalCount - filtered.Count;
        if (removed > 0)
        {
            Log.Information("Email filtering: Removed {RemovedCount} business/automated emails, kept {KeptCount} personal emails", 
                removed, filtered.Count);
        }

        return filtered;
    }

    /// <summary>
    /// Remove duplicate messages based on stricter equality (less aggressive)
    /// </summary>
    public IEnumerable<Message> RemoveDuplicates(IEnumerable<Message> messages)
    {
        List<Message> messageList = messages.ToList();
        int originalCount = messageList.Count;

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<Message> unique = new List<Message>(messageList.Count);

        foreach (Message message in messageList)
        {
            string key = GenerateConservativeMessageKey(message);

            if (!seen.Contains(key))
            {
                seen.Add(key);
                unique.Add(message);
            }
        }

        int duplicatesRemoved = originalCount - unique.Count;
        if (duplicatesRemoved > 0)
        {
            Log.Information("Duplicate removal: Removed {DuplicateCount} duplicates, kept {UniqueCount} unique messages",
                duplicatesRemoved, unique.Count);
        }

        return unique;
    }

    /// <summary>
    /// Extract only email addresses from contacts (ignoring phone numbers for email imports)
    /// </summary>
    public Contact GetEmailContactOnly(Contact contact)
    {
        if (contact.Emails.Count > 0)
        {
            // Keep only the email, remove phone numbers
            return new Contact(contact.Name, new HashSet<string>(), contact.Emails);
        }

        return contact;
    }

    /// <summary>
    /// Filter messages to include only those with valid personal sender email
    /// </summary>
    public IEnumerable<Message> FilterValidEmailAddressesOnly(IEnumerable<Message> messages)
    {
        // Require From (sender) to be a valid personal email; do not require To
        return messages.Where(m => HasValidPersonalEmail(m.From));
    }

    /// <summary>
    /// Check if a contact appears to be a business or automated sender
    /// Evaluates each email on the contact; if any email looks personal, returns false.
    /// </summary>
    private bool IsBusinessEmailAddress(Contact contact)
    {
        if (contact == null || contact.Emails.Count == 0)
        {
            return false;
        }

        bool anyBusiness = false;

        foreach (string email in contact.Emails)
        {
            string emailLower = email.ToLowerInvariant();
            string? domain = ExtractDomain(emailLower);
            string? localPart = ExtractLocalPart(emailLower);

            if (domain == null || localPart == null)
            {
                continue;
            }

            // Treat consumer mailbox providers as personal unless local part has business keywords
            if (ConsumerMailDomains.Contains(domain))
            {
                if (BusinessKeywords.Any(k => localPart.Contains(k)))
                {
                    anyBusiness = true;
                    continue;
                }

                // Clearly personal
                return false;
            }

            // Explicit business sender domains
            if (BusinessDomains.Contains(domain))
            {
                anyBusiness = true;
                continue;
            }

            // Other domains: check local-part for business keywords
            if (BusinessKeywords.Any(k => localPart.Contains(k)))
            {
                anyBusiness = true;
                continue;
            }

            // Otherwise consider personal
            return false;
        }

        // If we only saw business-like emails and never early-returned as personal
        return anyBusiness;
    }

    /// <summary>
    /// Check if a contact has at least one valid personal email address
    /// </summary>
    private bool HasValidPersonalEmail(Contact contact)
    {
        if (contact == null || contact.Emails.Count == 0)
        {
            return false;
        }

        foreach (string email in contact.Emails)
        {
            string lower = email.ToLowerInvariant();
            if (!EmailAddressRegex.IsMatch(lower))
            {
                continue;
            }

            string? domain = ExtractDomain(lower);
            string? local = ExtractLocalPart(lower);
            if (domain == null || local == null)
            {
                continue;
            }

            bool isConsumer = ConsumerMailDomains.Contains(domain);
            bool hasBizKeyword = BusinessKeywords.Any(k => local.Contains(k));
            bool isBizDomain = BusinessDomains.Contains(domain);

            // Personal if: consumer domain without biz keywords, or non-biz domain without biz keywords
            if ((isConsumer && !hasBizKeyword) || (!isBizDomain && !hasBizKeyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extract domain from email address
    /// </summary>
    private string? ExtractDomain(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex >= email.Length - 1)
        {
            return null;
        }

        return email[(atIndex + 1)..];
    }

    /// <summary>
    /// Extract local part (before @) from email address
    /// </summary>
    private string? ExtractLocalPart(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return null;
        }

        return email[..atIndex];
    }

    /// <summary>
    /// Generate a conservative duplicate key: exact second timestamp + sender email + full content hash
    /// </summary>
    private string GenerateConservativeMessageKey(Message message)
    {
        string senderEmail = message.From.Emails.FirstOrDefault() ?? "unknown";

        // Truncate to second precision
        DateTime ts = new DateTime(
            message.TimestampUtc.Year,
            message.TimestampUtc.Month,
            message.TimestampUtc.Day,
            message.TimestampUtc.Hour,
            message.TimestampUtc.Minute,
            message.TimestampUtc.Second,
            message.TimestampUtc.Offset == TimeSpan.Zero ? DateTimeKind.Utc : DateTimeKind.Unspecified);

        string content = message.Body ?? string.Empty;
        string contentHash = ComputeSha256Hex(content);

        return $"{senderEmail}|{ts:O}|{contentHash}";
    }

    private static string ComputeSha256Hex(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        byte[] hash = SHA256.HashData(data);
        StringBuilder sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}

/// <summary>
/// Statistics for email filtering operations
/// </summary>
public class EmailFilterStatistics
{
    public int OriginalCount { get; set; }
    public int FilteredCount { get; set; }
    public int RemovedCount { get; set; }
    public double RemovalPercentage { get; set; }
}
