using System.Text.RegularExpressions;

using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Analysis
{
    /// <summary>
    /// Keyword search across messages with advanced filtering
    /// </summary>
    public class KeywordSearchEngine
    {
        /// <summary>
        /// Search for keywords in messages
        /// </summary>
        public SearchResults Search(List<SmsMessage> messages, SearchQuery query)
        {
            AppLogger.Information($"Searching for keywords: {string.Join(", ", query.Keywords)}");

            SearchResults results = new SearchResults
            {
                Query = query,
                StartTime = DateTime.Now
            };

            foreach (SmsMessage msg in messages)
            {
                if (MatchesQuery(msg, query))
                {
                    results.Matches.Add(new SearchMatch
                    {
                        Message = msg,
                        MatchedKeywords = GetMatchedKeywords(msg, query.Keywords),
                        Context = GetContext(msg.MessageText, query.Keywords, query.ContextLength)
                    });
                }
            }

            results.EndTime = DateTime.Now;
            results.Duration = results.EndTime - results.StartTime;

            AppLogger.Information($"Search complete: {results.Matches.Count} matches found in {results.Duration.TotalSeconds:F2}s");
            return results;
        }

        /// <summary>
        /// Search with regex pattern
        /// </summary>
        public SearchResults SearchRegex(List<SmsMessage> messages, string pattern, SearchQuery query)
        {
            AppLogger.Information($"Searching with regex: {pattern}");

            Regex regex = new Regex(pattern, query.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            SearchResults results = new SearchResults
            {
                Query = query,
                StartTime = DateTime.Now
            };

            foreach (SmsMessage msg in messages)
            {
                if (ApplyFilters(msg, query) && regex.IsMatch(msg.MessageText))
                {
                    MatchCollection matches = regex.Matches(msg.MessageText);
                    results.Matches.Add(new SearchMatch
                    {
                        Message = msg,
                        MatchedKeywords = matches.Select(m => m.Value).Distinct().ToList(),
                        Context = GetRegexContext(msg.MessageText, matches, query.ContextLength)
                    });
                }
            }

            results.EndTime = DateTime.Now;
            results.Duration = results.EndTime - results.StartTime;

            AppLogger.Information($"Regex search complete: {results.Matches.Count} matches found");
            return results;
        }

        /// <summary>
        /// Get keyword frequency statistics
        /// </summary>
        public Dictionary<string, int> GetKeywordFrequency(List<SmsMessage> messages, List<string> keywords, bool caseSensitive = false)
        {
            Dictionary<string, int> frequency = keywords.ToDictionary(k => k, k => 0);

            foreach (SmsMessage msg in messages)
            {
                foreach (string keyword in keywords)
                {
                    int count = CountOccurrences(msg.MessageText, keyword, caseSensitive);
                    frequency[keyword] += count;
                }
            }

            return frequency;
        }

        /// <summary>
        /// Export search results to JSON
        /// </summary>
        public async Task ExportResultsAsync(SearchResults results, string outputPath)
        {
            AppLogger.Information($"Exporting {results.Matches.Count} search results to {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync($"  \"query\": {{");
                await writer.WriteLineAsync($"    \"keywords\": [{string.Join(", ", results.Query.Keywords.Select(k => $"\"{k}\""))}],");
                await writer.WriteLineAsync($"    \"caseSensitive\": {results.Query.CaseSensitive.ToString().ToLower()},");
                await writer.WriteLineAsync($"    \"matchAll\": {results.Query.MatchAll.ToString().ToLower()}");
                await writer.WriteLineAsync($"  }},");
                await writer.WriteLineAsync($"  \"totalMatches\": {results.Matches.Count},");
                await writer.WriteLineAsync($"  \"searchDuration\": \"{results.Duration}\",");
                await writer.WriteLineAsync("  \"matches\": [");

                for (int i = 0; i < results.Matches.Count; i++)
                {
                    SearchMatch match = results.Matches[i];
                    string comma = i < results.Matches.Count - 1 ? "," : "";

                    await writer.WriteLineAsync("    {");
                    await writer.WriteLineAsync($"      \"from\": \"{match.Message.FromName}\",");
                    await writer.WriteLineAsync($"      \"to\": \"{match.Message.ToName}\",");
                    await writer.WriteLineAsync($"      \"direction\": \"{match.Message.Direction}\",");
                    await writer.WriteLineAsync($"      \"dateTime\": \"{match.Message.DateTime:yyyy-MM-ddTHH:mm:ss}\",");
                    await writer.WriteLineAsync($"      \"matchedKeywords\": [{string.Join(", ", match.MatchedKeywords.Select(k => $"\"{k}\""))}],");
                    await writer.WriteLineAsync($"      \"context\": \"{EscapeJson(match.Context)}\"");
                    await writer.WriteLineAsync($"    }}{comma}");
                }

                await writer.WriteLineAsync("  ]");
                await writer.WriteLineAsync("}");
            }

            AppLogger.Information("Search results export complete");
        }

        private bool MatchesQuery(SmsMessage msg, SearchQuery query)
        {
            if (!ApplyFilters(msg, query))
            {
                return false;
            }

            List<bool> keywordMatches = new List<bool>();

            foreach (string keyword in query.Keywords)
            {
                bool matches = query.CaseSensitive
                    ? msg.MessageText.Contains(keyword)
                    : msg.MessageText.Contains(keyword, StringComparison.OrdinalIgnoreCase);

                keywordMatches.Add(matches);
            }

            return query.MatchAll ? keywordMatches.All(m => m) : keywordMatches.Any(m => m);
        }

        private bool ApplyFilters(SmsMessage msg, SearchQuery query)
        {
            if (query.DateFrom.HasValue && msg.DateTime < query.DateFrom.Value)
            {
                return false;
            }

            if (query.DateTo.HasValue && msg.DateTime > query.DateTo.Value)
            {
                return false;
            }

            return query.Direction != null && msg.Direction != query.Direction
                ? false
                : query.ContactFilter == null || query.ContactFilter.Any(c =>
                msg.FromPhone == c || msg.ToPhone == c);
        }

        private List<string> GetMatchedKeywords(SmsMessage msg, List<string> keywords)
        {
            return keywords.Where(k => msg.MessageText.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private string GetContext(string text, List<string> keywords, int contextLength)
        {
            foreach (string keyword in keywords)
            {
                int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    int start = Math.Max(0, index - contextLength);
                    int length = Math.Min(text.Length - start, keyword.Length + 2 * contextLength);
                    string context = text.Substring(start, length);

                    if (start > 0)
                    {
                        context = "..." + context;
                    }

                    if (start + length < text.Length)
                    {
                        context += "...";
                    }

                    return context;
                }
            }

            return text.Length > contextLength * 2 ? text.Substring(0, contextLength * 2) + "..." : text;
        }

        private string GetRegexContext(string text, MatchCollection matches, int contextLength)
        {
            if (matches.Count == 0)
            {
                return text;
            }

            Match firstMatch = matches[0];
            int start = Math.Max(0, firstMatch.Index - contextLength);
            int length = Math.Min(text.Length - start, firstMatch.Length + 2 * contextLength);
            string context = text.Substring(start, length);

            if (start > 0)
            {
                context = "..." + context;
            }

            if (start + length < text.Length)
            {
                context += "...";
            }

            return context;
        }

        private int CountOccurrences(string text, string keyword, bool caseSensitive)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(keyword, index, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += keyword.Length;
            }

            return count;
        }

        private string EscapeJson(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    /// <summary>
    /// Search query parameters
    /// </summary>
    public class SearchQuery
    {
        public List<string> Keywords { get; set; } = new List<string>();
        public bool CaseSensitive { get; set; } = false;
        public bool MatchAll { get; set; } = false;  // AND vs OR
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Direction { get; set; }  // "Sent" or "Received"
        public List<string>? ContactFilter { get; set; }
        public int ContextLength { get; set; } = 50;  // Characters before/after match
    }

    /// <summary>
    /// Search results container
    /// </summary>
    public class SearchResults
    {
        public SearchQuery Query { get; set; } = new SearchQuery();
        public List<SearchMatch> Matches { get; set; } = new List<SearchMatch>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalMatches => Matches.Count;
    }

    /// <summary>
    /// Individual search match
    /// </summary>
    public class SearchMatch
    {
        public SmsMessage Message { get; set; } = new SmsMessage();
        public List<string> MatchedKeywords { get; set; } = new List<string>();
        public string Context { get; set; } = string.Empty;
    }
}
