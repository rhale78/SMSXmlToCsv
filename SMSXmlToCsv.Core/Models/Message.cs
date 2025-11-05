namespace SMSXmlToCsv.Core.Models;

/// <summary>
/// Represents a single message (SMS, MMS, etc.) in the system.
/// </summary>
public class Message
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The contact associated with this message.
    /// </summary>
    public Contact? Contact { get; set; }

    /// <summary>
    /// The message body/content.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was sent or received.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Type of message (e.g., SMS, MMS).
    /// </summary>
    public string Type { get; set; } = "SMS";

    /// <summary>
    /// Whether the message was sent or received.
    /// </summary>
    public bool IsSent { get; set; }

    /// <summary>
    /// Phone number associated with the message.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}
