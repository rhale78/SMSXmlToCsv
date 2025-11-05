namespace SMSXmlToCsv.Core.Models;

/// <summary>
/// Defines different strategies for organizing exported data.
/// </summary>
public enum ExportStrategy
{
    /// <summary>
    /// Export all messages to a single file, regardless of contact.
    /// </summary>
    AllInOne,

    /// <summary>
    /// Export messages in separate files per contact, organized in folders.
    /// Each contact gets their own subfolder under a main "contacts" directory.
    /// </summary>
    PerContact
}
