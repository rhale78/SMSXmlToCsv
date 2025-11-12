using System;
using System.Collections.Generic;
using System.Linq;

namespace SMSXmlToCsv.Models;

/// <summary>
/// Represents a single communication instance across any platform.
/// All timestamps are normalized to UTC.
/// Base class with virtual methods for platform-specific behavior.
/// </summary>
public record Message(
    string SourceApplication,
    Contact From,
    Contact To,
    DateTimeOffset TimestampUtc,
    string Body,
    MessageDirection Direction,
    List<MediaAttachment> Attachments
)
{
    /// <summary>
    /// Creates a simple text message with no attachments.
    /// </summary>
    public static Message CreateTextMessage(
        string sourceApplication,
        Contact from,
        Contact to,
        DateTimeOffset timestampUtc,
        string body,
        MessageDirection direction)
    {
        return new Message(
            sourceApplication,
            from,
            to,
            timestampUtc,
            body,
            direction,
            new List<MediaAttachment>());
    }
    
    /// <summary>
    /// Determines if this message has valid, known contacts (not "Unknown")
    /// </summary>
    public virtual bool IsValid() => !IsUnknownContact(From) && !IsUnknownContact(To);
    
    /// <summary>
    /// Alias for IsValid() for semantic clarity
    /// </summary>
    public virtual bool HasKnownContact() => IsValid();
    
    /// <summary>
    /// Get the contact identifier for this message based on direction
    /// </summary>
    /// <param name="direction">Which direction to extract (Sent = To, Received = From)</param>
    /// <returns>Contact identifier (phone, email, or name)</returns>
    public virtual string GetContactIdentifier(MessageDirection direction)
    {
        Contact contact = direction == MessageDirection.Sent ? To : From;
        return GetContactIdentifierFromContact(contact);
    }
    
    /// <summary>
    /// Get the contact name for this message based on direction
    /// </summary>
    /// <param name="direction">Which direction to extract (Sent = To, Received = From)</param>
    /// <returns>Contact name or "Unknown"</returns>
    public virtual string GetContactName(MessageDirection direction)
    {
        Contact contact = direction == MessageDirection.Sent ? To : From;
        string name = contact?.Name;
        
        // Fallback to email if name is missing or "Unknown"
        if (string.IsNullOrWhiteSpace(name) || name.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            if (contact?.Emails?.Count > 0)
            {
                name = contact.Emails.First();
            }
        }
        
        return !string.IsNullOrWhiteSpace(name) ? name : "Unknown";
    }
    
    /// <summary>
    /// Get the optimal batch size for AI processing based on message type
    /// Override in subclasses for platform-specific sizing
    /// </summary>
    public virtual int GetOptimalBatchSize() => 75; // Default for SMS/chat
    
    /// <summary>
    /// Extract contact identifier from a Contact object
    /// Default priority: phone -> email -> name
    /// Override in subclasses for different priorities
    /// </summary>
    protected virtual string GetContactIdentifierFromContact(Contact contact)
    {
        if (contact == null)
            return "Unknown";
        
        // Default: phone -> email -> name
        if (contact.PhoneNumbers?.Count > 0)
            return contact.PhoneNumbers.First();
        if (contact.Emails?.Count > 0)
            return contact.Emails.First();
        return contact.Name ?? "Unknown";
    }
    
    /// <summary>
    /// Check if a contact is unknown or invalid
    /// </summary>
    protected bool IsUnknownContact(Contact contact)
    {
        if (contact == null) 
            return true;
        
        string name = contact.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) 
            return true;
        
        string lower = name.ToLowerInvariant();
        return lower == "unknown" || 
               lower == "(unknown)" || 
               lower == "none" ||
               lower == "n/a" ||
               lower.StartsWith("unknown") ||
               lower.StartsWith("(unknown");
    }
}

/// <summary>
/// SMS Message - requires phone number, short messages
/// </summary>
public record SmsMessage(
    string SourceApplication,
    Contact From,
    Contact To,
    DateTimeOffset TimestampUtc,
    string Body,
    MessageDirection Direction,
    List<MediaAttachment> Attachments
) : Message(SourceApplication, From, To, TimestampUtc, Body, Direction, Attachments)
{
    public override bool IsValid() => 
        (From?.PhoneNumbers?.Count > 0 || To?.PhoneNumbers?.Count > 0) && 
        base.IsValid();
    
    public override string GetContactIdentifier(MessageDirection direction)
    {
        Contact contact = direction == MessageDirection.Sent ? To : From;
        return contact?.PhoneNumbers?.FirstOrDefault() ?? "Unknown";
    }
    
    public override int GetOptimalBatchSize() => 100; // SMS are short, can batch more
}

/// <summary>
/// Email Message - uses email address, messages are typically long, needs smaller batches
/// </summary>
public record EmailMessage(
    string SourceApplication,
    Contact From,
    Contact To,
    DateTimeOffset TimestampUtc,
    string Body,
    MessageDirection Direction,
    List<MediaAttachment> Attachments
) : Message(SourceApplication, From, To, TimestampUtc, Body, Direction, Attachments)
{
    public override bool IsValid() => 
        (From?.Emails?.Count > 0 || To?.Emails?.Count > 0) && 
        base.IsValid();
    
    protected override string GetContactIdentifierFromContact(Contact contact)
    {
        if (contact == null)
            return "Unknown";
        
        // Email priority: email -> name -> phone
        return contact.Emails?.FirstOrDefault() ?? 
               contact.Name ?? 
               contact.PhoneNumbers?.FirstOrDefault() ?? 
               "Unknown";
    }
    
    public override int GetOptimalBatchSize() => 25; // Emails are LONG - use smaller batches!
}

/// <summary>
/// Social Media Message (Facebook/Instagram) - name-only, no phone/email required
/// </summary>
public record SocialMediaMessage(
    string SourceApplication,
    Contact From,
    Contact To,
    DateTimeOffset TimestampUtc,
    string Body,
    MessageDirection Direction,
    List<MediaAttachment> Attachments
) : Message(SourceApplication, From, To, TimestampUtc, Body, Direction, Attachments)
{
    public override bool IsValid() => 
        !string.IsNullOrWhiteSpace(From?.Name) && 
        !string.IsNullOrWhiteSpace(To?.Name) &&
        base.IsValid();
    
    protected override string GetContactIdentifierFromContact(Contact contact)
    {
        if (contact == null)
            return "Unknown";
        
        // Social media priority: email -> name (rarely have phone)
        return contact.Emails?.FirstOrDefault() ?? 
               contact.Name ?? 
               "Unknown";
    }
    
    public override int GetOptimalBatchSize() => 75; // Medium length
}

/// <summary>
/// Chat/Hangouts Message - email-based identification
/// </summary>
public record ChatMessage(
    string SourceApplication,
    Contact From,
    Contact To,
    DateTimeOffset TimestampUtc,
    string Body,
    MessageDirection Direction,
    List<MediaAttachment> Attachments
) : Message(SourceApplication, From, To, TimestampUtc, Body, Direction, Attachments)
{
    protected override string GetContactIdentifierFromContact(Contact contact)
    {
        if (contact == null)
            return "Unknown";
        
        // Chat priority: email -> name -> phone
        return contact.Emails?.FirstOrDefault() ?? 
               contact.Name ?? 
               contact.PhoneNumbers?.FirstOrDefault() ?? 
               "Unknown";
    }
    
    public override int GetOptimalBatchSize() => 80; // Similar to social media
}
