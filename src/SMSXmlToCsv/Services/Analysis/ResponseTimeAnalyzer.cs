using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;
using Serilog;

namespace SMSXmlToCsv.Services.Analysis;

/// <summary>
/// Analyzes response times between sent and received messages
/// </summary>
public class ResponseTimeAnalyzer
{
    /// <summary>
    /// Analyze response times for all messages
    /// </summary>
    public ResponseTimeReport AnalyzeResponseTimes(IEnumerable<Message> messages)
    {
        Log.Information("Starting response time analysis");

        List<Message> sortedMessages = messages.OrderBy(m => m.TimestampUtc).ToList();
        Dictionary<string, List<TimeSpan>> contactResponseTimes = new Dictionary<string, List<TimeSpan>>();
        List<ResponseTimeEntry> allEntries = new List<ResponseTimeEntry>();

        // Group by contact
        IEnumerable<IGrouping<string, Message>> contactGroups = sortedMessages
            .GroupBy(m => GetContactKey(m));

        foreach (IGrouping<string, Message> contactGroup in contactGroups)
        {
            List<Message> contactMessages = contactGroup.OrderBy(m => m.TimestampUtc).ToList();
            List<TimeSpan> responseTimes = new List<TimeSpan>();

            for (int i = 0; i < contactMessages.Count - 1; i++)
            {
                Message current = contactMessages[i];
                Message next = contactMessages[i + 1];

                // Check if this is a received message following a sent message (or vice versa)
                bool isResponsePair = (current.Direction == MessageDirection.Sent && next.Direction == MessageDirection.Received) ||
                                      (current.Direction == MessageDirection.Received && next.Direction == MessageDirection.Sent);

                if (isResponsePair)
                {
                    TimeSpan responseTime = next.TimestampUtc - current.TimestampUtc;

                    // Only count reasonable response times (less than 24 hours)
                    if (responseTime.TotalHours < 24)
                    {
                        responseTimes.Add(responseTime);

                        ResponseTimeEntry entry = new ResponseTimeEntry
                        {
                            ContactName = contactGroup.Key,
                            ResponseTime = responseTime,
                            FirstMessageTimestamp = current.TimestampUtc.DateTime,
                            SecondMessageTimestamp = next.TimestampUtc.DateTime,
                            FirstMessageDirection = current.Direction,
                            SecondMessageDirection = next.Direction
                        };

                        allEntries.Add(entry);
                    }
                }
            }

            if (responseTimes.Count > 0)
            {
                contactResponseTimes[contactGroup.Key] = responseTimes;
            }
        }

        // Calculate overall statistics
        List<TimeSpan> allResponseTimes = contactResponseTimes.Values.SelectMany(x => x).ToList();

        ResponseTimeReport report = new ResponseTimeReport
        {
            TotalResponses = allResponseTimes.Count,
            ContactStatistics = new Dictionary<string, ContactResponseStatistics>(),
            AllEntries = allEntries
        };

        if (allResponseTimes.Count > 0)
        {
            report.AverageResponseTime = TimeSpan.FromSeconds(allResponseTimes.Average(t => t.TotalSeconds));
            report.MedianResponseTime = GetMedian(allResponseTimes);
            report.MinResponseTime = allResponseTimes.Min();
            report.MaxResponseTime = allResponseTimes.Max();
        }

        // Calculate per-contact statistics
        foreach (KeyValuePair<string, List<TimeSpan>> kvp in contactResponseTimes)
        {
            ContactResponseStatistics stats = new ContactResponseStatistics
            {
                ResponseCount = kvp.Value.Count,
                AverageResponseTime = TimeSpan.FromSeconds(kvp.Value.Average(t => t.TotalSeconds)),
                MedianResponseTime = GetMedian(kvp.Value),
                MinResponseTime = kvp.Value.Min(),
                MaxResponseTime = kvp.Value.Max()
            };

            report.ContactStatistics[kvp.Key] = stats;
        }

        Log.Information("Response time analysis completed: {TotalResponses} response pairs analyzed", report.TotalResponses);

        return report;
    }

    /// <summary>
    /// Export response time report to JSON
    /// </summary>
    public async Task ExportReportAsync(ResponseTimeReport report, string outputPath)
    {
        Log.Information("Exporting response time report to {Path}", outputPath);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create export-friendly format
        object exportData = new
        {
            Summary = new
            {
                TotalResponses = report.TotalResponses,
                AverageResponseTime = FormatTimeSpan(report.AverageResponseTime),
                MedianResponseTime = FormatTimeSpan(report.MedianResponseTime),
                MinResponseTime = FormatTimeSpan(report.MinResponseTime),
                MaxResponseTime = FormatTimeSpan(report.MaxResponseTime)
            },
            ContactStatistics = report.ContactStatistics.Select(kvp => new
            {
                ContactName = kvp.Key,
                ResponseCount = kvp.Value.ResponseCount,
                AverageResponseTime = FormatTimeSpan(kvp.Value.AverageResponseTime),
                MedianResponseTime = FormatTimeSpan(kvp.Value.MedianResponseTime),
                MinResponseTime = FormatTimeSpan(kvp.Value.MinResponseTime),
                MaxResponseTime = FormatTimeSpan(kvp.Value.MaxResponseTime)
            }).OrderByDescending(c => c.ResponseCount)
        };

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(outputPath, json);

        Log.Information("Response time report exported successfully");
    }

    private string GetContactKey(Message message)
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

    private TimeSpan GetMedian(List<TimeSpan> times)
    {
        List<TimeSpan> sorted = times.OrderBy(t => t).ToList();
        int count = sorted.Count;

        if (count == 0)
        {
            return TimeSpan.Zero;
        }

        if (count % 2 == 0)
        {
            return TimeSpan.FromSeconds((sorted[count / 2 - 1].TotalSeconds + sorted[count / 2].TotalSeconds) / 2);
        }
        else
        {
            return sorted[count / 2];
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalMinutes < 1)
        {
            return $"{timeSpan.TotalSeconds:F1} seconds";
        }
        else if (timeSpan.TotalHours < 1)
        {
            return $"{timeSpan.TotalMinutes:F1} minutes";
        }
        else
        {
            return $"{timeSpan.TotalHours:F1} hours";
        }
    }
}

/// <summary>
/// Response time analysis report
/// </summary>
public class ResponseTimeReport
{
    public int TotalResponses { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public Dictionary<string, ContactResponseStatistics> ContactStatistics { get; set; } = new Dictionary<string, ContactResponseStatistics>();
    public List<ResponseTimeEntry> AllEntries { get; set; } = new List<ResponseTimeEntry>();
}

/// <summary>
/// Per-contact response time statistics
/// </summary>
public class ContactResponseStatistics
{
    public int ResponseCount { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
}

/// <summary>
/// Individual response time entry
/// </summary>
public class ResponseTimeEntry
{
    public string ContactName { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public DateTime FirstMessageTimestamp { get; set; }
    public DateTime SecondMessageTimestamp { get; set; }
    public MessageDirection FirstMessageDirection { get; set; }
    public MessageDirection SecondMessageDirection { get; set; }
}
