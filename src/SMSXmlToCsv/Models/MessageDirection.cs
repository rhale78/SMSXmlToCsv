namespace SMSXmlToCsv.Models;

/// <summary>
/// Represents the direction of a message relative to the user.
/// </summary>
public enum MessageDirection
{
    /// <summary>
    /// Message was sent by the user.
    /// </summary>
    Sent,

    /// <summary>
    /// Message was received by the user.
    /// </summary>
    Received,

    /// <summary>
    /// Message direction is unknown or cannot be determined.
    /// </summary>
    Unknown
}
