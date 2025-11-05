namespace SMSXmlToCsv.Core.Models;

/// <summary>
/// Represents a contact with whom messages are exchanged.
/// </summary>
public class Contact
{
    /// <summary>
    /// Unique identifier for the contact.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the contact.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the contact.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}
