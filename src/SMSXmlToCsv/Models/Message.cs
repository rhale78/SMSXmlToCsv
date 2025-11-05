using System;
using System.Collections.Generic;

namespace SMSXmlToCsv.Models;

/// <summary>
/// Represents a single communication instance across any platform.
/// All timestamps are normalized to UTC.
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
}
