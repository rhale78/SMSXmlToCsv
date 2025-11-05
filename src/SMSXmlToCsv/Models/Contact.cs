using System;
using System.Collections.Generic;

namespace SMSXmlToCsv.Models;

/// <summary>
/// Represents a person or entity involved in a message exchange.
/// Supports multiple identifiers to allow contact merging.
/// </summary>
public record Contact(
    string Name,
    HashSet<string> PhoneNumbers,
    HashSet<string> Emails
)
{
    /// <summary>
    /// Creates a Contact with a single phone number.
    /// </summary>
    public static Contact FromPhoneNumber(string name, string phoneNumber)
    {
        return new Contact(name, new HashSet<string> { phoneNumber }, new HashSet<string>());
    }

    /// <summary>
    /// Creates a Contact with a single email address.
    /// </summary>
    public static Contact FromEmail(string name, string email)
    {
        return new Contact(name, new HashSet<string>(), new HashSet<string> { email });
    }

    /// <summary>
    /// Creates a Contact with just a name (no contact details).
    /// </summary>
    public static Contact FromName(string name)
    {
        return new Contact(name, new HashSet<string>(), new HashSet<string>());
    }
}
