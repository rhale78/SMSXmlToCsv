using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;
using Serilog;
using Spectre.Console;

namespace SMSXmlToCsv.Services.Analysis;

/// <summary>
/// Generates comprehensive statistics and analytics for messages
/// </summary>
public class StatisticsAnalyzer
{
    /// <summary>
    /// Analyze all messages and generate comprehensive statistics
    /// </summary>
    public MessageStatistics AnalyzeMessages(IEnumerable<Message> messages)
    {
        Log.Information("Generating advanced statistics");

        List<Message> messageList = messages.ToList();
        MessageStatistics stats = new MessageStatistics();

        // Basic counts
        stats.TotalMessages = messageList.Count;
        stats.SentMessages = messageList.Count(m => m.Direction == MessageDirection.Sent);
        stats.ReceivedMessages = messageList.Count(m => m.Direction == MessageDirection.Received);

        if (messageList.Count == 0)
        {
            return stats;
        }

        // Date range
        stats.FirstMessageDate = messageList.Min(m => m.TimestampUtc).DateTime;
        stats.LastMessageDate = messageList.Max(m => m.TimestampUtc).DateTime;
        stats.DateRange = stats.LastMessageDate - stats.FirstMessageDate;

        // Messages with attachments
        stats.MessagesWithAttachments = messageList.Count(m => m.Attachments.Count > 0);

        // Word count analysis
        List<int> wordCounts = messageList
            .Where(m => !string.IsNullOrWhiteSpace(m.Body))
            .Select(m => m.Body.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length)
            .ToList();

        if (wordCounts.Count > 0)
        {
            stats.AverageWordsPerMessage = wordCounts.Average();
            stats.TotalWords = wordCounts.Sum();
        }

        // Message length analysis
        List<int> messageLengths = messageList
            .Where(m => !string.IsNullOrWhiteSpace(m.Body))
            .Select(m => m.Body.Length)
            .ToList();

        if (messageLengths.Count > 0)
        {
            stats.AverageMessageLength = messageLengths.Average();
            stats.LongestMessage = messageLengths.Max();
            stats.ShortestMessage = messageLengths.Min();
        }

        // Per-contact statistics
        stats.ContactStatistics = AnalyzePerContactStats(messageList);

        // Time-based patterns
        stats.MessagesByHour = AnalyzeByHour(messageList);
        stats.MessagesByDayOfWeek = AnalyzeByDayOfWeek(messageList);
        stats.MessagesByMonth = AnalyzeByMonth(messageList);

        Log.Information("Statistics analysis completed: {TotalMessages} messages analyzed", stats.TotalMessages);

        return stats;
    }

    /// <summary>
    /// Display statistics in console using Spectre.Console
    /// </summary>
    public void DisplayStatistics(MessageStatistics stats)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Message Statistics[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        // Overview table
        Table overviewTable = new Table();
        overviewTable.AddColumn("Metric");
        overviewTable.AddColumn("Value");

        overviewTable.AddRow("Total Messages", stats.TotalMessages.ToString("N0"));
        overviewTable.AddRow("Sent Messages", $"{stats.SentMessages:N0} ({(double)stats.SentMessages / stats.TotalMessages * 100:F1}%)");
        overviewTable.AddRow("Received Messages", $"{stats.ReceivedMessages:N0} ({(double)stats.ReceivedMessages / stats.TotalMessages * 100:F1}%)");
        overviewTable.AddRow("Messages with Attachments", stats.MessagesWithAttachments.ToString("N0"));
        overviewTable.AddRow("Date Range", $"{stats.FirstMessageDate:yyyy-MM-dd} to {stats.LastMessageDate:yyyy-MM-dd}");
        overviewTable.AddRow("Duration", $"{stats.DateRange.Days} days");
        overviewTable.AddRow("Average Words/Message", $"{stats.AverageWordsPerMessage:F1}");
        overviewTable.AddRow("Average Message Length", $"{stats.AverageMessageLength:F0} characters");

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Top contacts table
        if (stats.ContactStatistics.Count > 0)
        {
            AnsiConsole.Write(new Rule("[yellow]Top 10 Contacts[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            Table contactTable = new Table();
            contactTable.AddColumn("Contact");
            contactTable.AddColumn("Total");
            contactTable.AddColumn("Sent");
            contactTable.AddColumn("Received");

            foreach (KeyValuePair<string, ContactStats> kvp in stats.ContactStatistics
                .OrderByDescending(x => x.Value.TotalMessages)
                .Take(10))
            {
                contactTable.AddRow(
                    kvp.Key,
                    kvp.Value.TotalMessages.ToString("N0"),
                    kvp.Value.SentMessages.ToString("N0"),
                    kvp.Value.ReceivedMessages.ToString("N0"));
            }

            AnsiConsole.Write(contactTable);
        }
    }

    /// <summary>
    /// Export statistics to JSON
    /// </summary>
    public async Task ExportToJsonAsync(MessageStatistics stats, string outputPath)
    {
        Log.Information("Exporting statistics to JSON: {Path}", outputPath);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(stats, options);
        await File.WriteAllTextAsync(outputPath, json);

        Log.Information("Statistics exported to JSON successfully");
    }

    /// <summary>
    /// Export statistics to Markdown
    /// </summary>
    public async Task ExportToMarkdownAsync(MessageStatistics stats, string outputPath)
    {
        Log.Information("Exporting statistics to Markdown: {Path}", outputPath);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        StringBuilder md = new StringBuilder();
        md.AppendLine("# Message Statistics Report");
        md.AppendLine();
        md.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        md.AppendLine();

        // Overview
        md.AppendLine("## Overview");
        md.AppendLine();
        md.AppendLine($"- **Total Messages**: {stats.TotalMessages:N0}");
        md.AppendLine($"- **Sent Messages**: {stats.SentMessages:N0} ({(double)stats.SentMessages / stats.TotalMessages * 100:F1}%)");
        md.AppendLine($"- **Received Messages**: {stats.ReceivedMessages:N0} ({(double)stats.ReceivedMessages / stats.TotalMessages * 100:F1}%)");
        md.AppendLine($"- **Messages with Attachments**: {stats.MessagesWithAttachments:N0}");
        md.AppendLine($"- **Date Range**: {stats.FirstMessageDate:yyyy-MM-dd} to {stats.LastMessageDate:yyyy-MM-dd} ({stats.DateRange.Days} days)");
        md.AppendLine($"- **Average Words per Message**: {stats.AverageWordsPerMessage:F1}");
        md.AppendLine($"- **Average Message Length**: {stats.AverageMessageLength:F0} characters");
        md.AppendLine();

        // Top contacts
        md.AppendLine("## Top Contacts");
        md.AppendLine();
        md.AppendLine("| Contact | Total | Sent | Received |");
        md.AppendLine("|---------|-------|------|----------|");

        foreach (KeyValuePair<string, ContactStats> kvp in stats.ContactStatistics
            .OrderByDescending(x => x.Value.TotalMessages)
            .Take(20))
        {
            md.AppendLine($"| {kvp.Key} | {kvp.Value.TotalMessages:N0} | {kvp.Value.SentMessages:N0} | {kvp.Value.ReceivedMessages:N0} |");
        }

        md.AppendLine();

        // Messages by hour
        md.AppendLine("## Messages by Hour of Day");
        md.AppendLine();
        md.AppendLine("| Hour | Message Count |");
        md.AppendLine("|------|---------------|");

        foreach (KeyValuePair<int, int> kvp in stats.MessagesByHour.OrderBy(x => x.Key))
        {
            md.AppendLine($"| {kvp.Key:D2}:00 | {kvp.Value:N0} |");
        }

        md.AppendLine();

        await File.WriteAllTextAsync(outputPath, md.ToString());

        Log.Information("Statistics exported to Markdown successfully");
    }

    private Dictionary<string, ContactStats> AnalyzePerContactStats(List<Message> messages)
    {
        Dictionary<string, ContactStats> stats = new Dictionary<string, ContactStats>();

        foreach (Message message in messages)
        {
            string contactName = GetContactName(message);

            if (!stats.ContainsKey(contactName))
            {
                stats[contactName] = new ContactStats { ContactName = contactName };
            }

            stats[contactName].TotalMessages++;

            if (message.Direction == MessageDirection.Sent)
            {
                stats[contactName].SentMessages++;
            }
            else if (message.Direction == MessageDirection.Received)
            {
                stats[contactName].ReceivedMessages++;
            }
        }

        return stats;
    }

    private Dictionary<int, int> AnalyzeByHour(List<Message> messages)
    {
        Dictionary<int, int> hourCounts = new Dictionary<int, int>();

        for (int hour = 0; hour < 24; hour++)
        {
            hourCounts[hour] = 0;
        }

        foreach (Message message in messages)
        {
            int hour = message.TimestampUtc.Hour;
            hourCounts[hour]++;
        }

        return hourCounts;
    }

    private Dictionary<DayOfWeek, int> AnalyzeByDayOfWeek(List<Message> messages)
    {
        Dictionary<DayOfWeek, int> dayCounts = new Dictionary<DayOfWeek, int>();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            dayCounts[day] = 0;
        }

        foreach (Message message in messages)
        {
            DayOfWeek day = message.TimestampUtc.DayOfWeek;
            dayCounts[day]++;
        }

        return dayCounts;
    }

    private Dictionary<string, int> AnalyzeByMonth(List<Message> messages)
    {
        Dictionary<string, int> monthCounts = new Dictionary<string, int>();

        foreach (Message message in messages)
        {
            string monthKey = message.TimestampUtc.ToString("yyyy-MM");

            if (!monthCounts.ContainsKey(monthKey))
            {
                monthCounts[monthKey] = 0;
            }

            monthCounts[monthKey]++;
        }

        return monthCounts;
    }

    private string GetContactName(Message message)
    {
        if (!string.IsNullOrEmpty(message.From?.Name))
        {
            return message.From.Name;
        }

        if (!string.IsNullOrEmpty(message.To?.Name))
        {
            return message.To.Name;
        }

        return "Unknown";
    }
}

/// <summary>
/// Overall message statistics
/// </summary>
public class MessageStatistics
{
    public int TotalMessages { get; set; }
    public int SentMessages { get; set; }
    public int ReceivedMessages { get; set; }
    public int MessagesWithAttachments { get; set; }
    public DateTime FirstMessageDate { get; set; }
    public DateTime LastMessageDate { get; set; }
    public TimeSpan DateRange { get; set; }
    public double AverageWordsPerMessage { get; set; }
    public int TotalWords { get; set; }
    public double AverageMessageLength { get; set; }
    public int LongestMessage { get; set; }
    public int ShortestMessage { get; set; }
    public Dictionary<string, ContactStats> ContactStatistics { get; set; } = new Dictionary<string, ContactStats>();
    public Dictionary<int, int> MessagesByHour { get; set; } = new Dictionary<int, int>();
    public Dictionary<DayOfWeek, int> MessagesByDayOfWeek { get; set; } = new Dictionary<DayOfWeek, int>();
    public Dictionary<string, int> MessagesByMonth { get; set; } = new Dictionary<string, int>();
}

/// <summary>
/// Per-contact statistics
/// </summary>
public class ContactStats
{
    public string ContactName { get; set; } = string.Empty;
    public int TotalMessages { get; set; }
    public int SentMessages { get; set; }
    public int ReceivedMessages { get; set; }
}
