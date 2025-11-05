using System;
using System.Collections.Generic;
using System.Linq;
using SMSXmlToCsv.Models;
using Serilog;
using Spectre.Console;

namespace SMSXmlToCsv.Services;

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
    public IEnumerable<Message> FilterByContacts(IEnumerable<Message> messages, HashSet<string> contactNames)
    {
        return messages.Where(m =>
            contactNames.Contains(m.From.Name) ||
            contactNames.Contains(m.To.Name));
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
                AnsiConsole.MarkupLine($"[green]✓ Removed {removed} messages with unknown contacts[/]");
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
        HashSet<string> uniqueContacts = GetUniqueContacts(messages);

        if (uniqueContacts.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No contacts found[/]");
            return messages;
        }

        List<string> selectedContacts = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select contacts to [green]keep[/]:")
                .PageSize(20)
                .Required()
                .MoreChoicesText("[grey](Move up and down to see more contacts)[/]")
                .InstructionsText("[grey](Press [blue]Space[/] to toggle, [green]Enter[/] to accept)[/]")
                .AddChoices(uniqueContacts.OrderBy(c => c)));

        HashSet<string> selectedSet = new HashSet<string>(selectedContacts);
        IEnumerable<Message> filtered = FilterByContacts(messages, selectedSet);

        int removed = messages.Count - filtered.Count();
        AnsiConsole.MarkupLine($"[green]✓ Kept {filtered.Count()} messages from {selectedContacts.Count} contact(s)[/]");
        AnsiConsole.MarkupLine($"[yellow]Removed {removed} messages[/]");

        return filtered;
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
