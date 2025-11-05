using SMSXmlToCsv.Logging;
using SMSXmlToCsv.ML;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Analysis
{
    /// <summary>
    /// ML-based conversation clustering
    /// </summary>
    public class ConversationClusterAnalyzer
    {
        private readonly OllamaIntegration? _ollama;

        public ConversationClusterAnalyzer(OllamaIntegration? ollama = null)
        {
            _ollama = ollama;
        }

        /// <summary>
        /// Cluster conversations by similarity (simple keyword-based)
        /// </summary>
        public async Task<List<MessageCluster>> ClusterConversationsAsync(List<SmsMessage> messages, int numClusters = 5)
        {
            AppLogger.Information($"Starting conversation clustering ({numClusters} clusters)");

            // Simple keyword extraction and clustering
            Dictionary<string, List<SmsMessage>> keywordGroups = new Dictionary<string, List<SmsMessage>>();

            // Common topic keywords
            string[] topicKeywords = new[]
            {
                "work", "meeting", "office", "project", "job",
                "home", "house", "family", "kids", "parent",
                "food", "dinner", "lunch", "restaurant", "eat",
                "weekend", "party", "fun", "vacation", "trip",
                "love", "miss", "care", "sorry", "thanks",
                "school", "class", "study", "exam", "homework",
                "health", "doctor", "hospital", "sick", "medicine",
                "money", "pay", "buy", "shop", "price"
            };

            foreach (SmsMessage msg in messages)
            {
                string text = msg.MessageText.ToLower();
                bool clustered = false;

                foreach (string keyword in topicKeywords)
                {
                    if (text.Contains(keyword))
                    {
                        if (!keywordGroups.ContainsKey(keyword))
                        {
                            keywordGroups[keyword] = new List<SmsMessage>();
                        }
                        keywordGroups[keyword].Add(msg);
                        clustered = true;
                        break;
                    }
                }

                if (!clustered)
                {
                    if (!keywordGroups.ContainsKey("general"))
                    {
                        keywordGroups["general"] = new List<SmsMessage>();
                    }
                    keywordGroups["general"].Add(msg);
                }
            }

            // Convert to clusters and get labels from Ollama if available
            List<MessageCluster> clusters = new List<MessageCluster>();
            int clusterId = 1;

            foreach (KeyValuePair<string, List<SmsMessage>> group in keywordGroups.OrderByDescending(g => g.Value.Count).Take(numClusters))
            {
                string label = group.Key;

                // Use Ollama for better labels if available
                if (_ollama != null && await _ollama.IsAvailableAsync())
                {
                    List<string> samples = group.Value.Take(5).Select(m => m.MessageText).ToList();
                    label = await _ollama.GenerateClusterLabelAsync(samples);
                }

                clusters.Add(new MessageCluster
                {
                    ClusterId = clusterId++,
                    Label = label,
                    Messages = group.Value,
                    Size = group.Value.Count
                });
            }

            AppLogger.Information($"Created {clusters.Count} conversation clusters");
            return clusters;
        }

        /// <summary>
        /// Export clusters to JSON
        /// </summary>
        public async Task ExportClustersAsync(List<MessageCluster> clusters, string outputPath)
        {
            AppLogger.Information($"Exporting clusters to {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync($"  \"totalClusters\": {clusters.Count},");
                await writer.WriteLineAsync("  \"clusters\": [");

                for (int i = 0; i < clusters.Count; i++)
                {
                    MessageCluster cluster = clusters[i];
                    string comma = i < clusters.Count - 1 ? "," : "";

                    await writer.WriteLineAsync("    {");
                    await writer.WriteLineAsync($"      \"clusterId\": {cluster.ClusterId},");
                    await writer.WriteLineAsync($"      \"label\": \"{cluster.Label}\",");
                    await writer.WriteLineAsync($"      \"size\": {cluster.Size},");
                    await writer.WriteLineAsync($"      \"percentage\": {cluster.Size * 100.0 / clusters.Sum(c => c.Size):F2}");
                    await writer.WriteLineAsync($"    }}{comma}");
                }

                await writer.WriteLineAsync("  ]");
                await writer.WriteLineAsync("}");
            }

            AppLogger.Information("Cluster export complete");
        }
    }

    /// <summary>
    /// Message cluster
    /// </summary>
    public class MessageCluster
    {
        public int ClusterId { get; set; }
        public string Label { get; set; } = string.Empty;
        public List<SmsMessage> Messages { get; set; } = new List<SmsMessage>();
        public int Size { get; set; }
    }

    /// <summary>
    /// Sentiment analyzer for messages
    /// </summary>
    public class SentimentAnalyzer
    {
        private readonly OllamaIntegration? _ollama;

        public SentimentAnalyzer(OllamaIntegration? ollama = null)
        {
            _ollama = ollama;
        }

        /// <summary>
        /// Analyze sentiment of a single message
        /// </summary>
        public async Task<SentimentResult> AnalyzeSingleMessageAsync(string messageText)
        {
            if (_ollama == null || !await _ollama.IsAvailableAsync())
            {
                // Fallback for single message
                string[] positiveWords = { "love", "happy", "great", "good", "thanks", "awesome", "excellent", "wonderful", "nice", "perfect", "amazing" };
                string[] negativeWords = { "hate", "sad", "bad", "sorry", "terrible", "awful", "angry", "upset", "disappointed", "annoyed", "frustrated" };

                string text = messageText.ToLower();
                int positiveScore = positiveWords.Count(w => text.Contains(w));
                int negativeScore = negativeWords.Count(w => text.Contains(w));

                string sentiment = "neutral";
                if (positiveScore > negativeScore && positiveScore > 0)
                {
                    sentiment = "positive";
                }
                else if (negativeScore > positiveScore && negativeScore > 0)
                {
                    sentiment = "negative";
                }

                return new SentimentResult
                {
                    Text = messageText,
                    Sentiment = sentiment,
                    Confidence = (positiveScore + negativeScore) > 0 ? 0.7 : 0.5
                };
            }

            return await _ollama.AnalyzeSentimentAsync(messageText);
        }

        /// <summary>
        /// Analyze sentiment of messages with extended categories
        /// </summary>
        public async Task<SentimentAnalysisResults> AnalyzeMessagesAsync(List<SmsMessage> messages, int maxMessages = 1000)
        {
            AppLogger.Information($"Starting sentiment analysis (max {maxMessages} messages)");

            if (_ollama == null || !await _ollama.IsAvailableAsync())
            {
                AppLogger.Warning("Ollama not available, using fallback sentiment analysis");
                return FallbackSentimentAnalysis(messages);
            }

            SentimentAnalysisResults results = new SentimentAnalysisResults
            {
                TotalAnalyzed = Math.Min(messages.Count, maxMessages)
            };

            int analyzed = 0;
            foreach (SmsMessage msg in messages.Take(maxMessages))
            {
                SentimentResult result = await _ollama.AnalyzeSentimentAsync(msg.MessageText);
                results.Results.Add(result);

                // Count basic sentiment
                if (result.Sentiment == "positive")
                {
                    results.PositiveCount++;
                }
                else if (result.Sentiment == "negative")
                {
                    results.NegativeCount++;
                }
                else
                {
                    results.NeutralCount++;
                }

                // Extract extended sentiments from AI response if available
                string sentimentLower = result.Sentiment.ToLower();
                if (sentimentLower.Contains("professional"))
                {
                    results.ProfessionalCount++;
                }

                if (sentimentLower.Contains("friendly"))
                {
                    results.FriendlyCount++;
                }

                if (sentimentLower.Contains("combative") || sentimentLower.Contains("hostile"))
                {
                    results.CombativeCount++;
                }

                if (sentimentLower.Contains("argumentative"))
                {
                    results.ArgumentativeCount++;
                }

                if (sentimentLower.Contains("casual") || sentimentLower.Contains("informal"))
                {
                    results.CasualCount++;
                }

                if (sentimentLower.Contains("formal"))
                {
                    results.FormalCount++;
                }

                analyzed++;
                if (analyzed % 50 == 0)
                {
                    AppLogger.Information($"Sentiment analysis progress: {analyzed}/{results.TotalAnalyzed}");
                }
            }

            AppLogger.Information($"Sentiment complete: {results.PositiveCount} positive, {results.NegativeCount} negative, {results.NeutralCount} neutral");
            return results;
        }

        private SentimentAnalysisResults FallbackSentimentAnalysis(List<SmsMessage> messages)
        {
            // Enhanced keyword-based sentiment with multiple categories
            string[] positiveWords = { "love", "happy", "great", "good", "thanks", "awesome", "excellent", "wonderful", "nice", "perfect", "amazing" };
            string[] negativeWords = { "hate", "sad", "bad", "sorry", "terrible", "awful", "angry", "upset", "disappointed", "annoyed", "frustrated" };
            string[] professionalWords = { "meeting", "schedule", "confirm", "regarding", "attached", "please", "thank you", "regards" };
            string[] friendlyWords = { "lol", "haha", "btw", "hey", "thanks", "cool", "nice", "fun" };
            string[] combativeWords = { "wrong", "stupid", "ridiculous", "unacceptable", "seriously", "whatever" };
            string[] casualWords = { "gonna", "wanna", "yeah", "nah", "sup", "cool", "dude" };
            string[] formalWords = { "sincerely", "respectfully", "kindly", "regarding", "pursuant", "therefore" };

            SentimentAnalysisResults results = new SentimentAnalysisResults { TotalAnalyzed = messages.Count };

            foreach (SmsMessage msg in messages)
            {
                string text = msg.MessageText.ToLower();

                // Count keyword matches
                int positiveScore = positiveWords.Count(w => text.Contains(w));
                int negativeScore = negativeWords.Count(w => text.Contains(w));
                int professionalScore = professionalWords.Count(w => text.Contains(w));
                int friendlyScore = friendlyWords.Count(w => text.Contains(w));
                int combativeScore = combativeWords.Count(w => text.Contains(w));
                int casualScore = casualWords.Count(w => text.Contains(w));
                int formalScore = formalWords.Count(w => text.Contains(w));

                // Determine primary sentiment
                string sentiment = "neutral";
                if (positiveScore > negativeScore && positiveScore > 0)
                {
                    sentiment = "positive";
                }
                else if (negativeScore > positiveScore && negativeScore > 0)
                {
                    sentiment = "negative";
                }

                // Increment counters
                if (sentiment == "positive")
                {
                    results.PositiveCount++;
                }
                else if (sentiment == "negative")
                {
                    results.NegativeCount++;
                }
                else
                {
                    results.NeutralCount++;
                }

                if (professionalScore > 0)
                {
                    results.ProfessionalCount++;
                }

                if (friendlyScore > 0)
                {
                    results.FriendlyCount++;
                }

                if (combativeScore > 0)
                {
                    results.CombativeCount++;
                }

                if (casualScore > 0)
                {
                    results.CasualCount++;
                }

                if (formalScore > 0)
                {
                    results.FormalCount++;
                }

                // Argumentative is combination of negative and combative
                if (negativeScore > 0 && combativeScore > 0)
                {
                    results.ArgumentativeCount++;
                }

                results.Results.Add(new SentimentResult
                {
                    Text = msg.MessageText,
                    Sentiment = sentiment,
                    Confidence = (positiveScore + negativeScore) > 0 ? 0.7 : 0.5
                });
            }

            AppLogger.Information($"Fallback sentiment: {results.PositiveCount} positive, {results.NeutralCount} neutral, {results.NegativeCount} negative, {results.ProfessionalCount} professional, {results.FriendlyCount} friendly");
            return results;
        }

        /// <summary>
        /// Export sentiment analysis results
        /// </summary>
        public async Task ExportResultsAsync(SentimentAnalysisResults results, string outputPath)
        {
            AppLogger.Information($"Exporting sentiment results to {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync($"  \"totalAnalyzed\": {results.TotalAnalyzed},");
                await writer.WriteLineAsync($"  \"positiveCount\": {results.PositiveCount},");
                await writer.WriteLineAsync($"  \"negativeCount\": {results.NegativeCount},");
                await writer.WriteLineAsync($"  \"neutralCount\": {results.NeutralCount},");
                await writer.WriteLineAsync($"  \"positivePercentage\": {results.PositiveCount * 100.0 / results.TotalAnalyzed:F2},");
                await writer.WriteLineAsync($"  \"negativePercentage\": {results.NegativeCount * 100.0 / results.TotalAnalyzed:F2},");
                await writer.WriteLineAsync($"  \"neutralPercentage\": {results.NeutralCount * 100.0 / results.TotalAnalyzed:F2}");
                await writer.WriteLineAsync("}");
            }

            AppLogger.Information("Sentiment export complete");
        }
    }

    /// <summary>
    /// Sentiment analysis results with extended sentiment types
    /// </summary>
    public class SentimentAnalysisResults
    {
        public int TotalAnalyzed { get; set; }
        public int PositiveCount { get; set; }
        public int NegativeCount { get; set; }
        public int NeutralCount { get; set; }

        // Extended sentiment types
        public int ProfessionalCount { get; set; }
        public int FriendlyCount { get; set; }
        public int CombativeCount { get; set; }
        public int ArgumentativeCount { get; set; }
        public int CasualCount { get; set; }
        public int FormalCount { get; set; }

        public List<SentimentResult> Results { get; set; } = new List<SentimentResult>();

        // Temporal sentiment grouping
        public Dictionary<string, TemporalSentiment> SentimentByPeriod { get; set; } = new Dictionary<string, TemporalSentiment>();

        /// <summary>
        /// Get non-zero sentiment categories with their counts and percentages
        /// </summary>
        public Dictionary<string, (int Count, double Percentage)> GetNonZeroSentiments()
        {
            Dictionary<string, (int, double)> result = new Dictionary<string, (int, double)>();

            if (PositiveCount > 0)
            {
                result["Positive"] = (PositiveCount, PositiveCount * 100.0 / TotalAnalyzed);
            }

            if (NegativeCount > 0)
            {
                result["Negative"] = (NegativeCount, NegativeCount * 100.0 / TotalAnalyzed);
            }

            if (NeutralCount > 0)
            {
                result["Neutral"] = (NeutralCount, NeutralCount * 100.0 / TotalAnalyzed);
            }

            if (ProfessionalCount > 0)
            {
                result["Professional"] = (ProfessionalCount, ProfessionalCount * 100.0 / TotalAnalyzed);
            }

            if (FriendlyCount > 0)
            {
                result["Friendly"] = (FriendlyCount, FriendlyCount * 100.0 / TotalAnalyzed);
            }

            if (CombativeCount > 0)
            {
                result["Combative"] = (CombativeCount, CombativeCount * 100.0 / TotalAnalyzed);
            }

            if (ArgumentativeCount > 0)
            {
                result["Argumentative"] = (ArgumentativeCount, ArgumentativeCount * 100.0 / TotalAnalyzed);
            }

            if (CasualCount > 0)
            {
                result["Casual"] = (CasualCount, CasualCount * 100.0 / TotalAnalyzed);
            }

            if (FormalCount > 0)
            {
                result["Formal"] = (FormalCount, FormalCount * 100.0 / TotalAnalyzed);
            }

            return result;
        }

        /// <summary>
        /// Calculate temporal sentiment patterns
        /// </summary>
        public void CalculateTemporalPatterns(List<SmsMessage> messages)
        {
            if (Results.Count != messages.Count)
            {
                return;
            }

            // Determine grouping strategy based on date range
            List<DateTime> dates = messages.Select(m => m.DateTime).OrderBy(d => d).ToList();
            if (dates.Count == 0)
            {
                return;
            }

            TimeSpan span = dates.Last() - dates.First();
            string grouping = span.TotalDays > 365 ? "month" : span.TotalDays > 30 ? "week" : "day";

            for (int i = 0; i < messages.Count && i < Results.Count; i++)
            {
                SmsMessage msg = messages[i];
                SentimentResult result = Results[i];

                string periodKey = grouping switch
                {
                    "month" => msg.DateTime.ToString("yyyy-MM"),
                    "week" => $"{msg.DateTime.Year}-W{GetIso8601WeekOfYear(msg.DateTime):D2}",
                    "day" => msg.DateTime.ToString("yyyy-MM-dd"),
                    _ => msg.DateTime.ToString("yyyy-MM-dd")
                };

                if (!SentimentByPeriod.ContainsKey(periodKey))
                {
                    SentimentByPeriod[periodKey] = new TemporalSentiment
                    {
                        Period = periodKey,
                        Grouping = grouping
                    };
                }

                TemporalSentiment period = SentimentByPeriod[periodKey];
                period.TotalMessages++;

                if (result.Sentiment == "positive")
                {
                    period.PositiveCount++;
                }
                else if (result.Sentiment == "negative")
                {
                    period.NegativeCount++;
                }
                else
                {
                    period.NeutralCount++;
                }
            }
        }

        private int GetIso8601WeekOfYear(DateTime date)
        {
            DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= System.DayOfWeek.Monday && day <= System.DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
        }
    }

    /// <summary>
    /// Temporal sentiment data for a time period
    /// </summary>
    public class TemporalSentiment
    {
        public string Period { get; set; } = string.Empty;
        public string Grouping { get; set; } = string.Empty; // "day", "week", "month"
        public int TotalMessages { get; set; }
        public int PositiveCount { get; set; }
        public int NegativeCount { get; set; }
        public int NeutralCount { get; set; }

        public double PositivePercentage => TotalMessages > 0 ? PositiveCount * 100.0 / TotalMessages : 0;
        public double NegativePercentage => TotalMessages > 0 ? NegativeCount * 100.0 / TotalMessages : 0;
        public double NeutralPercentage => TotalMessages > 0 ? NeutralCount * 100.0 / TotalMessages : 0;
    }
}
