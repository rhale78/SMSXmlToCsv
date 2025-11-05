using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Analysis
{
    /// <summary>
    /// Analyzes response times and patterns
    /// </summary>
    public class ResponseTimeAnalyzer
    {
        /// <summary>
        /// Analyze response times for all contacts
        /// </summary>
        public Dictionary<string, ResponseTimeStats> AnalyzeResponseTimes(List<SmsMessage> messages, string userPhone)
        {
            AppLogger.Information("Starting response time analysis");

            Dictionary<string, ResponseTimeStats> contactStats = new Dictionary<string, ResponseTimeStats>();
            Dictionary<string, SmsMessage?> lastMessageByContact = new Dictionary<string, SmsMessage?>();

            foreach (SmsMessage msg in messages.OrderBy(m => m.UnixTimestamp))
            {
                string contactKey = GetContactKey(msg, userPhone);
                if (contactKey == "SELF")
                {
                    continue;
                }

                if (!contactStats.ContainsKey(contactKey))
                {
                    contactStats[contactKey] = new ResponseTimeStats { ContactKey = contactKey };
                }

                ResponseTimeStats stats = contactStats[contactKey];
                SmsMessage? lastMessage = lastMessageByContact.GetValueOrDefault(contactKey);

                if (lastMessage != null)
                {
                    TimeSpan responseTime = msg.DateTime - lastMessage.DateTime;

                    // Only count as response if direction changed
                    if (lastMessage.Direction != msg.Direction)
                    {
                        if (msg.Direction == "Received")
                        {
                            // They responded to us
                            stats.TheirResponseTimes.Add(responseTime);
                        }
                        else
                        {
                            // We responded to them
                            stats.OurResponseTimes.Add(responseTime);
                        }
                    }
                }

                lastMessageByContact[contactKey] = msg;
            }

            // Calculate statistics
            foreach (ResponseTimeStats stats in contactStats.Values)
            {
                stats.Calculate();
            }

            AppLogger.Information($"Response time analysis complete for {contactStats.Count} contacts");
            return contactStats;
        }

        /// <summary>
        /// Analyze response patterns by time of day
        /// </summary>
        public TimeOfDayStats AnalyzeTimeOfDayPatterns(List<SmsMessage> messages)
        {
            TimeOfDayStats stats = new TimeOfDayStats();

            foreach (SmsMessage msg in messages)
            {
                int hour = msg.DateTime.Hour;

                if (hour >= 0 && hour < 6)
                {
                    stats.Night.Add(msg);
                }
                else if (hour >= 6 && hour < 12)
                {
                    stats.Morning.Add(msg);
                }
                else if (hour >= 12 && hour < 18)
                {
                    stats.Afternoon.Add(msg);
                }
                else
                {
                    stats.Evening.Add(msg);
                }

                stats.ByHour[hour]++;
            }

            AppLogger.Information("Time of day analysis complete");
            return stats;
        }

        /// <summary>
        /// Export response time analysis to JSON
        /// </summary>
        public async Task ExportAnalysisAsync(Dictionary<string, ResponseTimeStats> stats, string outputPath)
        {
            AppLogger.Information($"Exporting response time analysis to {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync($"  \"totalContacts\": {stats.Count},");
                await writer.WriteLineAsync("  \"contacts\": [");

                int index = 0;
                foreach (KeyValuePair<string, ResponseTimeStats> kvp in stats.OrderByDescending(s => s.Value.TotalExchanges))
                {
                    string comma = index < stats.Count - 1 ? "," : "";
                    ResponseTimeStats contactStats = kvp.Value;

                    await writer.WriteLineAsync("    {");
                    await writer.WriteLineAsync($"      \"contactKey\": \"{contactStats.ContactKey}\",");
                    await writer.WriteLineAsync($"      \"totalExchanges\": {contactStats.TotalExchanges},");
                    await writer.WriteLineAsync($"      \"ourAverageResponseTime\": \"{contactStats.OurAverageResponseTime}\",");
                    await writer.WriteLineAsync($"      \"theirAverageResponseTime\": \"{contactStats.TheirAverageResponseTime}\",");
                    await writer.WriteLineAsync($"      \"ourMedianResponseTime\": \"{contactStats.OurMedianResponseTime}\",");
                    await writer.WriteLineAsync($"      \"theirMedianResponseTime\": \"{contactStats.TheirMedianResponseTime}\",");
                    await writer.WriteLineAsync($"      \"ourFastestResponse\": \"{contactStats.OurFastestResponse}\",");
                    await writer.WriteLineAsync($"      \"theirFastestResponse\": \"{contactStats.TheirFastestResponse}\"");
                    await writer.WriteLineAsync($"    }}{comma}");

                    index++;
                }

                await writer.WriteLineAsync("  ]");
                await writer.WriteLineAsync("}");
            }

            AppLogger.Information("Response time export complete");
        }

        private string GetContactKey(SmsMessage msg, string userPhone)
        {
            string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;
            if (contactPhone == userPhone)
            {
                return "SELF";
            }

            string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
            return $"{contactName}|{contactPhone}";
        }
    }

    /// <summary>
    /// Response time statistics for a contact
    /// </summary>
    public class ResponseTimeStats
    {
        public string ContactKey { get; set; } = string.Empty;
        public List<TimeSpan> OurResponseTimes { get; set; } = new List<TimeSpan>();
        public List<TimeSpan> TheirResponseTimes { get; set; } = new List<TimeSpan>();

        public TimeSpan OurAverageResponseTime { get; set; }
        public TimeSpan TheirAverageResponseTime { get; set; }
        public TimeSpan OurMedianResponseTime { get; set; }
        public TimeSpan TheirMedianResponseTime { get; set; }
        public TimeSpan OurFastestResponse { get; set; }
        public TimeSpan TheirFastestResponse { get; set; }
        public int TotalExchanges => OurResponseTimes.Count + TheirResponseTimes.Count;

        public void Calculate()
        {
            if (OurResponseTimes.Count > 0)
            {
                OurAverageResponseTime = TimeSpan.FromSeconds(OurResponseTimes.Average(t => t.TotalSeconds));
                OurMedianResponseTime = CalculateMedian(OurResponseTimes);
                OurFastestResponse = OurResponseTimes.Min();
            }

            if (TheirResponseTimes.Count > 0)
            {
                TheirAverageResponseTime = TimeSpan.FromSeconds(TheirResponseTimes.Average(t => t.TotalSeconds));
                TheirMedianResponseTime = CalculateMedian(TheirResponseTimes);
                TheirFastestResponse = TheirResponseTimes.Min();
            }
        }

        private TimeSpan CalculateMedian(List<TimeSpan> times)
        {
            List<TimeSpan> sorted = times.OrderBy(t => t).ToList();
            int mid = sorted.Count / 2;

            return sorted.Count % 2 == 0 ? TimeSpan.FromSeconds((sorted[mid - 1].TotalSeconds + sorted[mid].TotalSeconds) / 2) : sorted[mid];
        }
    }

    /// <summary>
    /// Time of day messaging patterns
    /// </summary>
    public class TimeOfDayStats
    {
        public List<SmsMessage> Morning { get; set; } = new List<SmsMessage>();      // 6am-12pm
        public List<SmsMessage> Afternoon { get; set; } = new List<SmsMessage>();    // 12pm-6pm
        public List<SmsMessage> Evening { get; set; } = new List<SmsMessage>();      // 6pm-12am
        public List<SmsMessage> Night { get; set; } = new List<SmsMessage>();        // 12am-6am
        public Dictionary<int, int> ByHour { get; set; } = Enumerable.Range(0, 24).ToDictionary(h => h, h => 0);

        public int TotalMessages => Morning.Count + Afternoon.Count + Evening.Count + Night.Count;
        public int MostActiveHour => ByHour.OrderByDescending(kvp => kvp.Value).First().Key;
        public int LeastActiveHour => ByHour.OrderBy(kvp => kvp.Value).First().Key;
    }
}
