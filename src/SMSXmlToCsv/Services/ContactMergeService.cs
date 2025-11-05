using System;
using System.Collections.Generic;
using System.Linq;
using SMSXmlToCsv.Models;
using Serilog;
using Spectre.Console;

namespace SMSXmlToCsv.Services;

/// <summary>
/// Service for merging duplicate contacts
/// </summary>
public class ContactMergeService
{
    /// <summary>
    /// Find potential duplicate contacts
    /// </summary>
    public List<ContactMergeCandidate> FindDuplicates(IEnumerable<Contact> contacts)
    {
        List<ContactMergeCandidate> candidates = new List<ContactMergeCandidate>();
        List<Contact> contactList = contacts.ToList();

        // Group by similar names
        Dictionary<string, List<Contact>> nameGroups = new Dictionary<string, List<Contact>>();

        foreach (Contact contact in contactList)
        {
            string normalizedName = NormalizeName(contact.Name);

            if (!nameGroups.ContainsKey(normalizedName))
            {
                nameGroups[normalizedName] = new List<Contact>();
            }

            nameGroups[normalizedName].Add(contact);
        }

        // Find duplicates within groups
        foreach (KeyValuePair<string, List<Contact>> group in nameGroups)
        {
            if (group.Value.Count > 1)
            {
                ContactMergeCandidate candidate = new ContactMergeCandidate
                {
                    Contacts = group.Value,
                    Reason = "Similar names"
                };

                candidates.Add(candidate);
            }
        }

        // Also check for same phone numbers or emails
        Dictionary<string, List<Contact>> phoneGroups = contactList
            .Where(c => c.PhoneNumbers.Any())
            .SelectMany(c => c.PhoneNumbers.Select(p => new { Contact = c, Phone = NormalizePhone(p) }))
            .GroupBy(x => x.Phone)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Contact).Distinct().ToList());

        foreach (KeyValuePair<string, List<Contact>> group in phoneGroups)
        {
            if (group.Value.Count > 1)
            {
                ContactMergeCandidate candidate = new ContactMergeCandidate
                {
                    Contacts = group.Value,
                    Reason = $"Same phone number: {group.Key}"
                };

                candidates.Add(candidate);
            }
        }

        Log.Information("Found {CandidateCount} potential duplicate contact groups", candidates.Count);

        return candidates;
    }

    /// <summary>
    /// Interactive merge interface
    /// </summary>
    public Dictionary<string, string> InteractiveMerge(List<ContactMergeCandidate> candidates)
    {
        Dictionary<string, string> mergeDecisions = new Dictionary<string, string>();

        if (candidates.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No duplicate contacts found.[/]");
            return mergeDecisions;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Contact Merge[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]Found {candidates.Count} potential duplicate contact group(s)[/]");
        AnsiConsole.WriteLine();

        int processedCount = 0;
        int mergedCount = 0;

        foreach (ContactMergeCandidate candidate in candidates)
        {
            processedCount++;

            Panel panel = new Panel(FormatCandidateInfo(candidate))
            {
                Header = new PanelHeader($"Duplicate Group {processedCount}/{candidates.Count}"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to do?[/]")
                    .AddChoices(new[]
                    {
                        "Merge all into first contact",
                        "Select primary contact",
                        "Skip (keep as separate)",
                        "Skip all remaining"
                    }));

            if (choice == "Skip all remaining")
            {
                AnsiConsole.MarkupLine("[grey]Skipping remaining duplicates...[/]");
                break;
            }
            else if (choice == "Skip (keep as separate)")
            {
                continue;
            }
            else if (choice == "Merge all into first contact")
            {
                Contact primary = candidate.Contacts[0];

                foreach (Contact contact in candidate.Contacts.Skip(1))
                {
                    mergeDecisions[contact.Name] = primary.Name;
                }

                mergedCount++;
                AnsiConsole.MarkupLine($"[green]✓[/] Merged {candidate.Contacts.Count - 1} contact(s) into '{primary.Name}'");
            }
            else if (choice == "Select primary contact")
            {
                string primaryName = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Select primary contact:[/]")
                        .AddChoices(candidate.Contacts.Select(c => c.Name)));

                Contact? primary = candidate.Contacts.FirstOrDefault(c => c.Name == primaryName);

                if (primary != null)
                {
                    foreach (Contact contact in candidate.Contacts.Where(c => c.Name != primaryName))
                    {
                        mergeDecisions[contact.Name] = primary.Name;
                    }

                    mergedCount++;
                    AnsiConsole.MarkupLine($"[green]✓[/] Merged {candidate.Contacts.Count - 1} contact(s) into '{primary.Name}'");
                }
            }

            AnsiConsole.WriteLine();
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Contact merge completed:[/]");
        AnsiConsole.MarkupLine($"  • Processed: {processedCount} group(s)");
        AnsiConsole.MarkupLine($"  • Merged: {mergedCount} group(s)");
        AnsiConsole.MarkupLine($"  • Total merges: {mergeDecisions.Count} contact(s)");

        return mergeDecisions;
    }

    /// <summary>
    /// Apply merge decisions to messages
    /// </summary>
    public IEnumerable<Message> ApplyMergeDecisions(IEnumerable<Message> messages, Dictionary<string, string> mergeDecisions)
    {
        if (mergeDecisions.Count == 0)
        {
            foreach (Message message in messages)
            {
                yield return message;
            }
            yield break;
        }

        Log.Information("Applying {MergeCount} contact merge decisions", mergeDecisions.Count);

        foreach (Message message in messages)
        {
            Contact updatedFrom = message.From;
            if (mergeDecisions.ContainsKey(updatedFrom.Name))
            {
                updatedFrom = new Contact(mergeDecisions[updatedFrom.Name], updatedFrom.PhoneNumbers, updatedFrom.Emails);
            }

            Contact updatedTo = message.To;
            if (mergeDecisions.ContainsKey(updatedTo.Name))
            {
                updatedTo = new Contact(mergeDecisions[updatedTo.Name], updatedTo.PhoneNumbers, updatedTo.Emails);
            }

            if (updatedFrom != message.From || updatedTo != message.To)
            {
                yield return message with { From = updatedFrom, To = updatedTo };
            }
            else
            {
                yield return message;
            }
        }

        Log.Information("Contact merge completed");
    }

    private string FormatCandidateInfo(ContactMergeCandidate candidate)
    {
        Table table = new Table();
        table.AddColumn("Contact Name");
        table.AddColumn("Phone Numbers");
        table.AddColumn("Emails");

        foreach (Contact contact in candidate.Contacts)
        {
            string phones = contact.PhoneNumbers.Any()
                ? string.Join(", ", contact.PhoneNumbers)
                : "[grey]none[/]";

            string emails = contact.Emails.Any()
                ? string.Join(", ", contact.Emails)
                : "[grey]none[/]";

            table.AddRow(contact.Name, phones, emails);
        }

        return $"[yellow]Reason:[/] {candidate.Reason}\n\n{table}";
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // Remove spaces, convert to lowercase, remove punctuation
        string normalized = name.ToLowerInvariant()
            .Replace(" ", "")
            .Replace(".", "")
            .Replace("-", "")
            .Replace("_", "");

        return normalized;
    }

    private string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        // Remove all non-digit characters
        string normalized = new string(phone.Where(char.IsDigit).ToArray());

        // Take last 10 digits (US phone numbers)
        if (normalized.Length > 10)
        {
            normalized = normalized.Substring(normalized.Length - 10);
        }

        return normalized;
    }
}

/// <summary>
/// Represents a group of potentially duplicate contacts
/// </summary>
public class ContactMergeCandidate
{
    public List<Contact> Contacts { get; set; } = new List<Contact>();
    public string Reason { get; set; } = string.Empty;
}
