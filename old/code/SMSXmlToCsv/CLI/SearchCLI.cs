using SMSXmlToCsv.Analysis;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.CLI
{
    /// <summary>
    /// Interactive command-line interface for keyword search
    /// </summary>
    public class SearchCLI
    {
        private readonly List<SmsMessage> _messages;
        private readonly KeywordSearchEngine _searchEngine;

        public SearchCLI(List<SmsMessage> messages)
        {
            _messages = messages;
            _searchEngine = new KeywordSearchEngine();
        }

        /// <summary>
        /// Run interactive search mode
        /// </summary>
        public async Task RunInteractiveSearchAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("??????????????????????????????????????????????????????????????????");
            Console.WriteLine("?          ?? INTERACTIVE KEYWORD SEARCH                        ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????????");
            Console.ResetColor();
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("\nSearch Options:");
                Console.WriteLine("  1. Simple keyword search");
                Console.WriteLine("  2. Advanced search (with filters)");
                Console.WriteLine("  3. Regex pattern search");
                Console.WriteLine("  4. Keyword frequency analysis");
                Console.WriteLine("  5. Export last results");
                Console.WriteLine("  0. Exit search mode");
                Console.Write("\nYour choice: ");

                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await SimpleSearchAsync();
                        break;
                    case "2":
                        await AdvancedSearchAsync();
                        break;
                    case "3":
                        await RegexSearchAsync();
                        break;
                    case "4":
                        await FrequencyAnalysisAsync();
                        break;
                    case "5":
                        await ExportResultsAsync();
                        break;
                    case "0":
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid choice. Try again.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        private SearchResults? _lastResults;

        private async Task SimpleSearchAsync()
        {
            Console.Write("\nEnter keywords (comma-separated): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            List<string> keywords = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();

            Console.Write("Case sensitive? (y/n) [n]: ");
            bool caseSensitive = Console.ReadLine()?.Trim().ToLower() == "y";

            Console.Write("Match ALL keywords (AND) or ANY keyword (OR)? (all/any) [any]: ");
            bool matchAll = Console.ReadLine()?.Trim().ToLower() == "all";

            SearchQuery query = new SearchQuery
            {
                Keywords = keywords,
                CaseSensitive = caseSensitive,
                MatchAll = matchAll
            };

            Console.WriteLine("\n?? Searching...");
            _lastResults = _searchEngine.Search(_messages, query);

            DisplayResults(_lastResults);
        }

        private async Task AdvancedSearchAsync()
        {
            Console.Write("\nEnter keywords (comma-separated): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            List<string> keywords = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();

            SearchQuery query = new SearchQuery { Keywords = keywords };

            Console.Write("Case sensitive? (y/n) [n]: ");
            query.CaseSensitive = Console.ReadLine()?.Trim().ToLower() == "y";

            Console.Write("Match ALL keywords? (y/n) [n]: ");
            query.MatchAll = Console.ReadLine()?.Trim().ToLower() == "y";

            Console.Write("Date from (YYYY-MM-DD) [press Enter to skip]: ");
            string? dateFrom = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime from))
            {
                query.DateFrom = from;
            }

            Console.Write("Date to (YYYY-MM-DD) [press Enter to skip]: ");
            string? dateTo = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime to))
            {
                query.DateTo = to;
            }

            Console.Write("Direction (sent/received) [press Enter for both]: ");
            string? direction = Console.ReadLine()?.Trim().ToLower();
            if (direction == "sent" || direction == "received")
            {
                query.Direction = char.ToUpper(direction[0]) + direction.Substring(1);
            }

            Console.WriteLine("\n?? Searching...");
            _lastResults = _searchEngine.Search(_messages, query);

            DisplayResults(_lastResults);
        }

        private async Task RegexSearchAsync()
        {
            Console.Write("\nEnter regex pattern: ");
            string? pattern = Console.ReadLine();
            if (string.IsNullOrEmpty(pattern))
            {
                return;
            }

            Console.Write("Case sensitive? (y/n) [n]: ");
            bool caseSensitive = Console.ReadLine()?.Trim().ToLower() == "y";

            SearchQuery query = new SearchQuery
            {
                CaseSensitive = caseSensitive
            };

            try
            {
                Console.WriteLine("\n?? Searching...");
                _lastResults = _searchEngine.SearchRegex(_messages, pattern, query);

                DisplayResults(_lastResults);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n? Invalid regex pattern: {ex.Message}");
                Console.ResetColor();
            }
        }

        private async Task FrequencyAnalysisAsync()
        {
            Console.Write("\nEnter keywords to analyze (comma-separated): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            List<string> keywords = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();

            Console.Write("Case sensitive? (y/n) [n]: ");
            bool caseSensitive = Console.ReadLine()?.Trim().ToLower() == "y";

            Console.WriteLine("\n?? Analyzing frequency...");
            Dictionary<string, int> frequency = _searchEngine.GetKeywordFrequency(_messages, keywords, caseSensitive);

            Console.WriteLine("\n=== Keyword Frequency ===\n");
            foreach (KeyValuePair<string, int> kvp in frequency.OrderByDescending(k => k.Value))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{kvp.Key,-30}");
                Console.ResetColor();
                Console.WriteLine($" {kvp.Value:N0} occurrences");
            }

            Console.WriteLine();
        }

        private async Task ExportResultsAsync()
        {
            if (_lastResults == null || _lastResults.Matches.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n??  No search results to export.");
                Console.ResetColor();
                return;
            }

            Console.Write("\nEnter output file path (e.g., search_results.json): ");
            string? path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                await _searchEngine.ExportResultsAsync(_lastResults, path);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"? Results exported to: {path}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"? Export failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void DisplayResults(SearchResults results)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"?????????????????????????????????????????????????????????????????");
            Console.WriteLine($"?  Found {results.TotalMatches} matches in {results.Duration.TotalSeconds:F2}s");
            Console.WriteLine($"?????????????????????????????????????????????????????????????????");
            Console.ResetColor();

            if (results.Matches.Count == 0)
            {
                Console.WriteLine("\nNo messages found matching your search criteria.");
                return;
            }

            int displayCount = Math.Min(10, results.Matches.Count);
            Console.WriteLine($"\nShowing first {displayCount} results:\n");

            for (int i = 0; i < displayCount; i++)
            {
                SearchMatch match = results.Matches[i];

                Console.ForegroundColor = match.Message.Direction == "Sent" ? ConsoleColor.Blue : ConsoleColor.Red;
                Console.Write($"[{match.Message.Direction}] ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{match.Message.DateTime:yyyy-MM-dd HH:mm} ");
                Console.ResetColor();

                Console.Write($"{match.Message.FromName} ? {match.Message.ToName}\n");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  Matched: {string.Join(", ", match.MatchedKeywords)}");
                Console.ResetColor();

                Console.WriteLine($"  {match.Context}");
                Console.WriteLine();
            }

            if (results.Matches.Count > displayCount)
            {
                Console.WriteLine($"... and {results.Matches.Count - displayCount} more results");
            }

            Console.WriteLine($"\n?? Tip: Use option 5 to export all {results.TotalMatches} results to a file.");
        }
    }
}
