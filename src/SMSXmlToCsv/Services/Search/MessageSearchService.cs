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

namespace SMSXmlToCsv.Services.Search;

/// <summary>
/// Provides keyword search functionality across messages
/// </summary>
public class MessageSearchService
{
    private List<Message> _messages = new List<Message>();

    public void LoadMessages(IEnumerable<Message> messages)
    {
        _messages = messages.ToList();
        Log.Information("Loaded {MessageCount} messages for searching", _messages.Count);
    }

    /// <summary>
    /// Interactive search interface
    /// </summary>
    public void InteractiveSearch()
    {
        if (_messages.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No messages loaded. Please import messages first.[/]");
            return;
        }

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Message Search[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[grey]Searching across {_messages.Count:N0} messages[/]");
            AnsiConsole.WriteLine();

            string keyword = AnsiConsole.Ask<string>("Enter search keyword (or 'quit' to exit):");

            if (keyword.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            bool caseSensitive = !AnsiConsole.Confirm("Case-insensitive search?", true);
            bool includeContext = AnsiConsole.Confirm("Show surrounding messages?", true);

            List<SearchResult> results = Search(keyword, caseSensitive, includeContext ? 2 : 0);

            DisplayResults(results, keyword);

            if (results.Count > 0)
            {
                if (AnsiConsole.Confirm("Export results to JSON?"))
                {
                    string filename = $"search-results-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                    ExportResultsAsync(results, filename).Wait();
                    AnsiConsole.MarkupLine($"[green]✓[/] Results exported to: {filename}");
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Search for keyword in messages
    /// </summary>
    public List<SearchResult> Search(string keyword, bool caseSensitive = false, int contextMessages = 0)
    {
        Log.Information("Searching for '{Keyword}' (case-sensitive: {CaseSensitive})", keyword, caseSensitive);

        List<SearchResult> results = new List<SearchResult>();
        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < _messages.Count; i++)
        {
            Message message = _messages[i];

            if (message.Body.Contains(keyword, comparison))
            {
                SearchResult result = new SearchResult
                {
                    Message = message,
                    Index = i,
                    MatchPosition = message.Body.IndexOf(keyword, comparison),
                    Keyword = keyword
                };

                // Add context messages
                if (contextMessages > 0)
                {
                    int startIndex = Math.Max(0, i - contextMessages);
                    int endIndex = Math.Min(_messages.Count - 1, i + contextMessages);

                    for (int j = startIndex; j < i; j++)
                    {
                        result.ContextBefore.Add(_messages[j]);
                    }

                    for (int j = i + 1; j <= endIndex; j++)
                    {
                        result.ContextAfter.Add(_messages[j]);
                    }
                }

                results.Add(result);
            }
        }

        Log.Information("Found {ResultCount} matches for '{Keyword}'", results.Count, keyword);

        return results;
    }

    /// <summary>
    /// Search with contact filter
    /// </summary>
    public List<SearchResult> SearchByContact(string keyword, string contactName, bool caseSensitive = false)
    {
        List<Message> contactMessages = _messages
            .Where(m =>
                (m.From?.Name?.Equals(contactName, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (m.To?.Name?.Equals(contactName, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        MessageSearchService tempService = new MessageSearchService();
        tempService.LoadMessages(contactMessages);

        return tempService.Search(keyword, caseSensitive);
    }

    /// <summary>
    /// Display search results
    /// </summary>
    private void DisplayResults(List<SearchResult> results, string keyword)
    {
        AnsiConsole.WriteLine();

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No matches found.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Found {results.Count} match(es)[/]");
        AnsiConsole.WriteLine();

        int displayLimit = Math.Min(results.Count, 10);

        for (int i = 0; i < displayLimit; i++)
        {
            SearchResult result = results[i];

            Panel panel = new Panel(FormatSearchResult(result, keyword))
            {
                Header = new PanelHeader($"Match {i + 1}/{results.Count}"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        if (results.Count > displayLimit)
        {
            AnsiConsole.MarkupLine($"[grey]... and {results.Count - displayLimit} more result(s)[/]");
        }
    }

    /// <summary>
    /// Format a single search result for display
    /// </summary>
    private string FormatSearchResult(SearchResult result, string keyword)
    {
        StringBuilder sb = new StringBuilder();

        // Contact info
        string contactName = result.Message.From?.Name ?? "Unknown";
        string direction = result.Message.Direction == MessageDirection.Sent ? "→" : "←";

        sb.AppendLine($"[bold]{contactName}[/] {direction} [grey]{result.Message.TimestampUtc:yyyy-MM-dd HH:mm:ss}[/]");
        sb.AppendLine();

        // Context before
        if (result.ContextBefore.Count > 0)
        {
            foreach (Message msg in result.ContextBefore)
            {
                sb.AppendLine($"[dim]{msg.Body.EscapeMarkup()}[/]");
            }
            sb.AppendLine("[grey]---[/]");
        }

        // Highlighted match
        string highlightedBody = HighlightKeyword(result.Message.Body, keyword);
        sb.AppendLine(highlightedBody);

        // Context after
        if (result.ContextAfter.Count > 0)
        {
            sb.AppendLine("[grey]---[/]");
            foreach (Message msg in result.ContextAfter)
            {
                sb.AppendLine($"[dim]{msg.Body.EscapeMarkup()}[/]");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Highlight keyword in text
    /// </summary>
    private string HighlightKeyword(string text, string keyword)
    {
        string escaped = text.EscapeMarkup();
        string escapedKeyword = keyword.EscapeMarkup();

        int index = escaped.IndexOf(escapedKeyword, StringComparison.OrdinalIgnoreCase);

        if (index >= 0)
        {
            string before = escaped.Substring(0, index);
            string match = escaped.Substring(index, escapedKeyword.Length);
            string after = escaped.Substring(index + escapedKeyword.Length);

            return $"{before}[yellow on black]{match}[/]{after}";
        }

        return escaped;
    }

    /// <summary>
    /// Export search results to JSON
    /// </summary>
    public async Task ExportResultsAsync(List<SearchResult> results, string outputPath)
    {
        Log.Information("Exporting {ResultCount} search results to {Path}", results.Count, outputPath);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        List<object> exportData = results.Select(r => new
        {
            ContactName = r.Message.From?.Name ?? "Unknown",
            Timestamp = r.Message.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss"),
            Direction = r.Message.Direction.ToString(),
            MessageBody = r.Message.Body,
            Keyword = r.Keyword,
            MatchPosition = r.MatchPosition
        }).ToList<object>();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(outputPath, json);

        Log.Information("Search results exported successfully");
    }
}

/// <summary>
/// Search result with context
/// </summary>
public class SearchResult
{
    public Message Message { get; set; } = null!;
    public int Index { get; set; }
    public int MatchPosition { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public List<Message> ContextBefore { get; set; } = new List<Message>();
    public List<Message> ContextAfter { get; set; } = new List<Message>();
}
