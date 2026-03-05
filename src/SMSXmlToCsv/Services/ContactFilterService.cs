using System;
using System.Collections.Generic;
using System.Linq;
using SMSXmlToCsv.Models;
using Serilog;
using Spectre.Console;

namespace SMSXmlToCsv.Services;

/// <summary>
/// Contact information for filtering display
/// </summary>
public class ContactFilterInfo
{
    public string Name { get; set; } = string.Empty;
    public HashSet<string> Sources { get; set; } = new HashSet<string>(); // SMS, Facebook, Instagram, Gmail, etc. (SourceApplication)
    public HashSet<string> PhoneNumbers { get; set; } = new HashSet<string>();
    public HashSet<string> Emails { get; set; } = new HashSet<string>();
    public string Identifier { get; set; } = string.Empty; // Primary identifier
    public int SentMessages { get; set; }
    public int ReceivedMessages { get; set; }
    public int TotalMessages => SentMessages + ReceivedMessages;
    
    /// <summary>
    /// Get a display-friendly source string showing which importers created this contact's messages
    /// </summary>
    public string GetSourceDisplay()
    {
        if (Sources.Count == 0)
            return "Unknown";
        
        // Sort sources for consistent display and abbreviate common ones
        List<string> displayNames = Sources
            .Select(s => AbbreviateSourceName(s))
            .OrderBy(s => s)
            .ToList();
        
        return string.Join("+", displayNames);
    }
    
    /// <summary>
    /// Abbreviate long source names for compact display
    /// </summary>
    private string AbbreviateSourceName(string source)
    {
        return source switch
        {
            "Android SMS Backup" => "SMS",
            "Facebook Messenger" => "Facebook",
            "Instagram Messages" => "Instagram",
            "Google Takeout" => "Google",
            "Gmail" => "Gmail",
            _ => source
        };
    }
    
    /// <summary>
    /// Get contact identifier types (Phone, Email, Name-only, Mixed)
    /// </summary>
    public string GetIdentifierTypes()
    {
        bool hasPhone = PhoneNumbers.Count > 0;
        bool hasEmail = Emails.Count > 0;
        
        if (hasPhone && hasEmail)
            return "Mixed";
        else if (hasPhone)
            return "Phone";
        else if (hasEmail)
            return "Email";
        else
            return "Name-only";
    }
}

/// <summary>
/// Service for filtering messages based on contact criteria
/// </summary>
public class ContactFilterService
{
    /// <summary>
    /// Filter out messages from unknown contacts
    /// </summary>
    public IEnumerable<Message> FilterUnknownContacts(IEnumerable<Message> messages)
    {
        return messages.Where(m => 
            !IsUnknownContact(m.From) && 
            !IsUnknownContact(m.To));
    }

    /// <summary>
    /// Filter messages by contact names (whitelist)
    /// </summary>
    private string SanitizeContactName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }
        // Remove '[' , ']' and apostrophes
        var filtered = new string(name.Where(c => c != '[' && c != ']' && c != '\'').ToArray());
        return filtered.Trim();
    }

    public IEnumerable<Message> FilterByContacts(IEnumerable<Message> messages, HashSet<string> contactNames)
    {
        // Create a case-insensitive sanitized lookup for selected contacts
        HashSet<string> sanitizedSelectedNames = new HashSet<string>(
            contactNames.Select(SanitizeContactName),
            StringComparer.OrdinalIgnoreCase);

        return messages.Where(m =>
        {
            string fromNameSanitized = SanitizeContactName(m.From.Name);
            string toNameSanitized = SanitizeContactName(m.To.Name);
            
            return sanitizedSelectedNames.Contains(fromNameSanitized) ||
                   sanitizedSelectedNames.Contains(toNameSanitized);
        });
    }

    /// <summary>
    /// Interactive contact filtering with options
    /// </summary>
    public IEnumerable<Message> InteractiveFilter(IEnumerable<Message> messages)
    {
        List<Message> messageList = messages.ToList();

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]Contact Filtering[/]");
        AnsiConsole.WriteLine();

        // Show current statistics
        int unknownCount = messageList.Count(m => IsUnknownContact(m.From) || IsUnknownContact(m.To));
        int totalContacts = GetUniqueContacts(messageList).Count;

        AnsiConsole.MarkupLine($"Current messages: [cyan]{messageList.Count}[/]");
        AnsiConsole.MarkupLine($"Total unique contacts: [cyan]{totalContacts}[/]");
        AnsiConsole.MarkupLine($"Messages with unknown contacts: [yellow]{unknownCount}[/]");
        AnsiConsole.WriteLine();

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select filtering option:")
                .AddChoices(new[]
                {
                    "Remove Unknown Contacts",
                    "Select Specific Contacts",
                    "Back"
                }));

        switch (choice)
        {
            case "Remove Unknown Contacts":
                IEnumerable<Message> filtered = FilterUnknownContacts(messageList);
                int removed = messageList.Count - filtered.Count();
                AnsiConsole.MarkupLine($"[green]? Removed {removed} messages with unknown contacts[/]");
                return filtered;

            case "Select Specific Contacts":
                return InteractiveContactSelection(messageList);

            case "Back":
            default:
                return messageList;
        }
    }

    private IEnumerable<Message> InteractiveContactSelection(List<Message> messages)
    {
        // Build detailed contact info with source and message counts
        Dictionary<string, ContactFilterInfo> contactInfoDict = BuildContactInfo(messages);

        if (contactInfoDict.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No contacts found[/]");
            return messages;
        }

        // Create display strings with source and message counts
        // Sort by total messages (desc), then by name
        List<(string DisplayName, string ContactKey)> contactChoices = contactInfoDict.Values
            .OrderByDescending(c => c.TotalMessages)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c =>
            {
                string sourceDisplay = c.GetSourceDisplay();
                string identifierType = c.GetIdentifierTypes();
                
                // Build a compact identifier display showing counts
                string identifierDisplay = "";
                if (c.PhoneNumbers.Count > 0 || c.Emails.Count > 0)
                {
                    List<string> ids = new List<string>();
                    if (c.PhoneNumbers.Count > 0) ids.Add($"??{c.PhoneNumbers.Count}");
                    if (c.Emails.Count > 0) ids.Add($"??{c.Emails.Count}");
                    identifierDisplay = $" [{string.Join(" ", ids)}]";
                }
                
                string display = $"{c.Name.Substring(0, Math.Min(30, c.Name.Length)),-30} " +
                                $"[dim]({sourceDisplay}){identifierDisplay}[/] " +
                                $"[blue]?{c.SentMessages}[/] " +
                                $"[red]?{c.ReceivedMessages}[/] " +
                                $"[cyan]({c.TotalMessages} total)[/]";
                return (display, c.Name);
            })
            .ToList();

        if (contactChoices.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No valid contacts found[/]");
            return messages;
        }

        // Show selection prompt with enhanced display
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Legend: [blue]?Sent[/] [red]?Received[/] [cyan](Total)[/] ??Phones ??Emails[/]");
        AnsiConsole.WriteLine();

        List<string> selectedDisplayNames = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select contacts to [green]keep[/]:")
                .PageSize(20)
                .Required()
                .MoreChoicesText("[grey](Move up and down to see more contacts)[/]")
                .InstructionsText("[grey](Press [blue]Space[/] to toggle, [green]Enter[/] to accept)[/]")
                .AddChoices(contactChoices.Select(c => c.DisplayName)));

        // Map selected display names back to contact keys
        HashSet<string> selectedContactNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string displayName in selectedDisplayNames)
        {
            // Find matching contact by display name
            var matchingChoice = contactChoices.FirstOrDefault(c => c.DisplayName == displayName);
            if (matchingChoice != default)
            {
                selectedContactNames.Add(matchingChoice.ContactKey);
            }
        }

        // Use sanitized names for filtering (case-insensitive)
        IEnumerable<Message> filtered = FilterByContacts(messages, selectedContactNames);

        int removed = messages.Count - filtered.Count();
        AnsiConsole.MarkupLine($"[green]? Kept {filtered.Count()} messages from {selectedContactNames.Count} contact(s)[/]");
        AnsiConsole.MarkupLine($"[yellow]Removed {removed} messages[/]");

        return filtered;
    }

    private Dictionary<string, ContactFilterInfo> BuildContactInfo(List<Message> messages)
    {
        Dictionary<string, ContactFilterInfo> contactInfo = new Dictionary<string, ContactFilterInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (Message message in messages)
        {
            // Process From contact
            if (!IsUnknownContact(message.From))
            {
                string sanitizedName = SanitizeContactName(message.From.Name);
                if (!string.IsNullOrWhiteSpace(sanitizedName))
                {
                    if (!contactInfo.ContainsKey(sanitizedName))
                    {
                        contactInfo[sanitizedName] = new ContactFilterInfo
                        {
                            Name = sanitizedName,
                            Sources = new HashSet<string>(),
                            PhoneNumbers = new HashSet<string>(),
                            Emails = new HashSet<string>()
                        };
                    }

                    // Aggregate sources and identifiers
                    ContactFilterInfo info = contactInfo[sanitizedName];
                    
                    // Add the SOURCE APPLICATION (the importer that created this message)
                    if (!string.IsNullOrWhiteSpace(message.SourceApplication))
                    {
                        info.Sources.Add(message.SourceApplication);
                    }
                    
                    // Add phone numbers (identifier, not source)
                    if (message.From.PhoneNumbers != null)
                    {
                        foreach (string phone in message.From.PhoneNumbers)
                        {
                            if (!string.IsNullOrWhiteSpace(phone))
                                info.PhoneNumbers.Add(phone);
                        }
                    }
                    
                    // Add emails (identifier, not source)
                    if (message.From.Emails != null)
                    {
                        foreach (string email in message.From.Emails)
                        {
                            if (!string.IsNullOrWhiteSpace(email))
                                info.Emails.Add(email);
                        }
                    }
                    
                    // Set primary identifier (prefer phone, then email, then name)
                    if (string.IsNullOrEmpty(info.Identifier))
                    {
                        info.Identifier = GetIdentifier(message.From);
                    }

                    // This is a received message (from this contact)
                    if (message.Direction == MessageDirection.Received)
                    {
                        info.ReceivedMessages++;
                    }
                }
            }

            // Process To contact
            if (!IsUnknownContact(message.To))
            {
                string sanitizedName = SanitizeContactName(message.To.Name);
                if (!string.IsNullOrWhiteSpace(sanitizedName))
                {
                    if (!contactInfo.ContainsKey(sanitizedName))
                    {
                        contactInfo[sanitizedName] = new ContactFilterInfo
                        {
                            Name = sanitizedName,
                            Sources = new HashSet<string>(),
                            PhoneNumbers = new HashSet<string>(),
                            Emails = new HashSet<string>()
                        };
                    }

                    // Aggregate sources and identifiers
                    ContactFilterInfo info = contactInfo[sanitizedName];
                    
                    // Add the SOURCE APPLICATION (the importer that created this message)
                    if (!string.IsNullOrWhiteSpace(message.SourceApplication))
                    {
                        info.Sources.Add(message.SourceApplication);
                    }
                    
                    // Add phone numbers (identifier, not source)
                    if (message.To.PhoneNumbers != null)
                    {
                        foreach (string phone in message.To.PhoneNumbers)
                        {
                            if (!string.IsNullOrWhiteSpace(phone))
                                info.PhoneNumbers.Add(phone);
                        }
                    }
                    
                    // Add emails (identifier, not source)
                    if (message.To.Emails != null)
                    {
                        foreach (string email in message.To.Emails)
                        {
                            if (!string.IsNullOrWhiteSpace(email))
                                info.Emails.Add(email);
                        }
                    }
                    
                    // Set primary identifier (prefer phone, then email, then name)
                    if (string.IsNullOrEmpty(info.Identifier))
                    {
                        info.Identifier = GetIdentifier(message.To);
                    }

                    // This is a sent message (to this contact)
                    if (message.Direction == MessageDirection.Sent)
                    {
                        info.SentMessages++;
                    }
                }
            }
        }

        return contactInfo;
    }

    private string GetIdentifier(Contact contact)
    {
        // Prefer phone, then email, then name
        if (contact.PhoneNumbers != null && contact.PhoneNumbers.Count > 0)
            return contact.PhoneNumbers.First();
        else if (contact.Emails != null && contact.Emails.Count > 0)
            return contact.Emails.First();
        else
            return contact.Name;
    }

    private bool IsUnknownContact(Contact contact)
    {
        if (contact == null)
        {
            return true;
        }

        string? name = contact.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        // Check for common "unknown" patterns
        string lowerName = name.ToLowerInvariant();
        return lowerName == "unknown" ||
               lowerName == "(unknown)" ||
               lowerName == "none" ||
               lowerName == "n/a" ||
               lowerName.StartsWith("unknown") ||
               lowerName.StartsWith("(unknown");
    }

    private HashSet<string> GetUniqueContacts(List<Message> messages)
    {
        HashSet<string> contacts = new HashSet<string>();

        foreach (Message message in messages)
        {
            if (!IsUnknownContact(message.From))
            {
                contacts.Add(message.From.Name);
            }

            if (!IsUnknownContact(message.To))
            {
                contacts.Add(message.To.Name);
            }
        }

        return contacts;
    }
}
