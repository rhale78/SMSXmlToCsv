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
/// Analyzes conversation threads based on time gaps between messages
/// </summary>
public class ThreadAnalyzer
{
    private readonly TimeSpan _threadTimeout;
    private readonly int _minimumThreadLength;

    public ThreadAnalyzer(int threadTimeoutMinutes = 30, int minimumThreadLength = 2)
    {
        _threadTimeout = TimeSpan.FromMinutes(threadTimeoutMinutes);
        _minimumThreadLength = minimumThreadLength;
    }

    /// <summary>
    /// Detect conversation threads in messages
    /// </summary>
    public List<ConversationThread> DetectThreads(IEnumerable<Message> messages)
    {
        List<ConversationThread> threads = new List<ConversationThread>();
        Message[] sortedMessages = messages.OrderBy(m => m.TimestampUtc).ToArray();

        Log.Information("Analyzing {MessageCount} messages for conversation threads", sortedMessages.Length);

        // Group by contact first
        IEnumerable<IGrouping<string, Message>> contactGroups = sortedMessages
            .GroupBy(m => GetContactKey(m));

        foreach (IGrouping<string, Message> contactGroup in contactGroups)
        {
            ConversationThread? currentThread = null;
            DateTimeOffset lastMessageTime = DateTimeOffset.MinValue;

            foreach (Message message in contactGroup.OrderBy(m => m.TimestampUtc))
            {
                TimeSpan timeSinceLastMessage = message.TimestampUtc - lastMessageTime;

                // Start new thread if timeout exceeded or first message
                if (currentThread == null || timeSinceLastMessage > _threadTimeout)
                {
                    // Save previous thread if it meets minimum length
                    if (currentThread != null && currentThread.MessageCount >= _minimumThreadLength)
                    {
                        threads.Add(currentThread);
                    }

                    // Start new thread
                    currentThread = new ConversationThread
                    {
                        ThreadId = Guid.NewGuid().ToString(),
                        ContactName = contactGroup.Key,
                        StartTime = message.TimestampUtc.DateTime,
                        EndTime = message.TimestampUtc.DateTime,
                        MessageCount = 0,
                        Messages = new List<Message>()
                    };
                }

                // Add message to current thread
                currentThread.Messages.Add(message);
                currentThread.MessageCount++;
                currentThread.EndTime = message.TimestampUtc.DateTime;
                lastMessageTime = message.TimestampUtc;
            }

            // Add final thread for this contact
            if (currentThread != null && currentThread.MessageCount >= _minimumThreadLength)
            {
                threads.Add(currentThread);
            }
        }

        Log.Information("Detected {ThreadCount} conversation threads", threads.Count);
        return threads;
    }

    /// <summary>
    /// Calculate thread statistics
    /// </summary>
    public ThreadStatistics CalculateStatistics(List<ConversationThread> threads)
    {
        if (threads.Count == 0)
        {
            return new ThreadStatistics();
        }

        return new ThreadStatistics
        {
            TotalThreads = threads.Count,
            AverageThreadLength = threads.Average(t => t.MessageCount),
            LongestThread = threads.Max(t => t.MessageCount),
            ShortestThread = threads.Min(t => t.MessageCount),
            AverageThreadDuration = TimeSpan.FromSeconds(threads.Average(t => t.Duration.TotalSeconds)),
            TotalMessages = threads.Sum(t => t.MessageCount)
        };
    }

    /// <summary>
    /// Export threads to JSON format
    /// </summary>
    public async Task ExportThreadsAsync(List<ConversationThread> threads, string outputPath)
    {
        Log.Information("Exporting {ThreadCount} threads to {Path}", threads.Count, outputPath);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create simplified thread data for export (without full message objects)
        List<object> exportData = threads.Select(t => new
        {
            t.ThreadId,
            t.ContactName,
            StartTime = t.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = t.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
            t.MessageCount,
            DurationMinutes = t.Duration.TotalMinutes
        }).ToList<object>();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(outputPath, json);

        Log.Information("Thread analysis exported successfully");
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
}

/// <summary>
/// Represents a conversation thread
/// </summary>
public class ConversationThread
{
    public string ThreadId { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MessageCount { get; set; }
    public List<Message> Messages { get; set; } = new List<Message>();

    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Statistics about conversation threads
/// </summary>
public class ThreadStatistics
{
    public int TotalThreads { get; set; }
    public double AverageThreadLength { get; set; }
    public int LongestThread { get; set; }
    public int ShortestThread { get; set; }
    public TimeSpan AverageThreadDuration { get; set; }
    public int TotalMessages { get; set; }
}
