using System.Text;

using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Analysis
{
    /// <summary>
    /// Generates comprehensive statistics reports
    /// </summary>
    public class AdvancedStatisticsExporter
    {
        /// <summary>
        /// Generate comprehensive statistics
        /// </summary>
        public ComprehensiveStats GenerateStatistics(List<SmsMessage> messages, string userPhone)
        {
            AppLogger.Information("Generating comprehensive statistics");

            ComprehensiveStats stats = new ComprehensiveStats
            {
                GeneratedAt = DateTime.Now,
                UserPhone = userPhone
            };

            // Basic counts
            stats.TotalMessages = messages.Count;
            stats.SentMessages = messages.Count(m => m.Direction == "Sent");
            stats.ReceivedMessages = messages.Count(m => m.Direction == "Received");

            // Date range
            if (messages.Count > 0)
            {
                stats.FirstMessage = messages.Min(m => m.DateTime);
                stats.LastMessage = messages.Max(m => m.DateTime);
                stats.DateRange = stats.LastMessage - stats.FirstMessage;
            }

            // Contact statistics
            stats.UniqueContacts = messages
                .Select(m => m.Direction == "Sent" ? m.ToPhone : m.FromPhone)
                .Where(p => p != userPhone)
                .Distinct()
                .Count();

            // Message length statistics
            List<int> lengths = messages.Select(m => m.MessageText.Length).ToList();
            if (lengths.Count > 0)
            {
                stats.AverageMessageLength = lengths.Average();
                stats.LongestMessage = lengths.Max();
                stats.ShortestMessage = lengths.Min();
            }

            // Time-based statistics
            stats.MessagesByDayOfWeek = messages
                .GroupBy(m => m.DateTime.DayOfWeek)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            stats.MessagesByMonth = messages
                .GroupBy(m => $"{m.DateTime.Year}-{m.DateTime.Month:D2}")
                .ToDictionary(g => g.Key, g => g.Count());

            stats.MessagesByYear = messages
                .GroupBy(m => m.DateTime.Year)
                .ToDictionary(g => g.Key, g => g.Count());

            // Peak activity
            stats.BusiestDay = messages
                .GroupBy(m => m.DateTime.Date)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? DateTime.MinValue;

            stats.BusiestHour = messages
                .GroupBy(m => m.DateTime.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? 0;

            // Top contacts
            stats.TopContacts = messages
                .Where(m => (m.Direction == "Sent" ? m.ToPhone : m.FromPhone) != userPhone)
                .GroupBy(m =>
                {
                    string phone = m.Direction == "Sent" ? m.ToPhone : m.FromPhone;
                    string name = m.Direction == "Sent" ? m.ToName : m.FromName;
                    return $"{name}|{phone}";
                })
                .Select(g => new ContactSummary
                {
                    ContactKey = g.Key,
                    TotalMessages = g.Count(),
                    SentToThem = g.Count(m => m.Direction == "Sent"),
                    ReceivedFromThem = g.Count(m => m.Direction == "Received")
                })
                .OrderByDescending(c => c.TotalMessages)
                .Take(20)
                .ToList();

            AppLogger.Information("Statistics generation complete");
            return stats;
        }

        /// <summary>
        /// Export statistics to JSON
        /// </summary>
        public async Task ExportJsonAsync(ComprehensiveStats stats, string outputPath)
        {
            AppLogger.Information($"Exporting statistics to JSON: {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync($"  \"generatedAt\": \"{stats.GeneratedAt:yyyy-MM-ddTHH:mm:ss}\",");
                await writer.WriteLineAsync($"  \"userPhone\": \"{MaskPhone(stats.UserPhone)}\",");
                await writer.WriteLineAsync($"  \"totalMessages\": {stats.TotalMessages},");
                await writer.WriteLineAsync($"  \"sentMessages\": {stats.SentMessages},");
                await writer.WriteLineAsync($"  \"receivedMessages\": {stats.ReceivedMessages},");
                await writer.WriteLineAsync($"  \"uniqueContacts\": {stats.UniqueContacts},");
                await writer.WriteLineAsync($"  \"firstMessage\": \"{stats.FirstMessage:yyyy-MM-ddTHH:mm:ss}\",");
                await writer.WriteLineAsync($"  \"lastMessage\": \"{stats.LastMessage:yyyy-MM-ddTHH:mm:ss}\",");
                await writer.WriteLineAsync($"  \"dateRangeDays\": {stats.DateRange.TotalDays:F0},");
                await writer.WriteLineAsync($"  \"averageMessageLength\": {stats.AverageMessageLength:F1},");
                await writer.WriteLineAsync($"  \"longestMessage\": {stats.LongestMessage},");
                await writer.WriteLineAsync($"  \"shortestMessage\": {stats.ShortestMessage},");
                await writer.WriteLineAsync($"  \"busiestDay\": \"{stats.BusiestDay:yyyy-MM-dd}\",");
                await writer.WriteLineAsync($"  \"busiestHour\": {stats.BusiestHour},");

                // Day of week breakdown
                await writer.WriteLineAsync("  \"messagesByDayOfWeek\": {");
                int dayIndex = 0;
                foreach (KeyValuePair<string, int> kvp in stats.MessagesByDayOfWeek)
                {
                    string comma = dayIndex < stats.MessagesByDayOfWeek.Count - 1 ? "," : "";
                    await writer.WriteLineAsync($"    \"{kvp.Key}\": {kvp.Value}{comma}");
                    dayIndex++;
                }
                await writer.WriteLineAsync("  },");

                // Top contacts
                await writer.WriteLineAsync("  \"topContacts\": [");
                for (int i = 0; i < stats.TopContacts.Count; i++)
                {
                    ContactSummary contact = stats.TopContacts[i];
                    string comma = i < stats.TopContacts.Count - 1 ? "," : "";

                    await writer.WriteLineAsync("    {");
                    await writer.WriteLineAsync($"      \"contactKey\": \"{contact.ContactKey}\",");
                    await writer.WriteLineAsync($"      \"totalMessages\": {contact.TotalMessages},");
                    await writer.WriteLineAsync($"      \"sentToThem\": {contact.SentToThem},");
                    await writer.WriteLineAsync($"      \"receivedFromThem\": {contact.ReceivedFromThem}");
                    await writer.WriteLineAsync($"    }}{comma}");
                }
                await writer.WriteLineAsync("  ]");

                await writer.WriteLineAsync("}");
            }

            AppLogger.Information("Statistics export complete");
        }

        /// <summary>
        /// Export statistics to Markdown report
        /// </summary>
        public async Task ExportMarkdownAsync(ComprehensiveStats stats, string outputPath)
        {
            AppLogger.Information($"Exporting statistics to Markdown: {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                await writer.WriteLineAsync("# SMS Conversation Statistics");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"**Generated**: {stats.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("## Overview");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"- **Total Messages**: {stats.TotalMessages:N0}");
                await writer.WriteLineAsync($"- **Sent**: {stats.SentMessages:N0} ({stats.SentMessages * 100.0 / stats.TotalMessages:F1}%)");
                await writer.WriteLineAsync($"- **Received**: {stats.ReceivedMessages:N0} ({stats.ReceivedMessages * 100.0 / stats.TotalMessages:F1}%)");
                await writer.WriteLineAsync($"- **Unique Contacts**: {stats.UniqueContacts:N0}");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("## Time Range");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"- **First Message**: {stats.FirstMessage:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync($"- **Last Message**: {stats.LastMessage:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync($"- **Total Days**: {stats.DateRange.TotalDays:F0}");
                await writer.WriteLineAsync($"- **Average per Day**: {stats.TotalMessages / Math.Max(1, stats.DateRange.TotalDays):F1}");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("## Message Characteristics");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"- **Average Length**: {stats.AverageMessageLength:F0} characters");
                await writer.WriteLineAsync($"- **Longest Message**: {stats.LongestMessage} characters");
                await writer.WriteLineAsync($"- **Shortest Message**: {stats.ShortestMessage} characters");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("## Activity Patterns");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"- **Busiest Day**: {stats.BusiestDay:yyyy-MM-dd}");
                await writer.WriteLineAsync($"- **Most Active Hour**: {stats.BusiestHour}:00");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("## Messages by Day of Week");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("| Day | Messages | Percentage |");
                await writer.WriteLineAsync("|-----|----------|------------|");
                foreach (KeyValuePair<string, int> kvp in stats.MessagesByDayOfWeek.OrderByDescending(k => k.Value))
                {
                    double percentage = kvp.Value * 100.0 / stats.TotalMessages;
                    await writer.WriteLineAsync($"| {kvp.Key} | {kvp.Value:N0} | {percentage:F1}% |");
                }
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("## Top 20 Contacts");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("| Rank | Contact | Total | Sent | Received |");
                await writer.WriteLineAsync("|------|---------|-------|------|----------|");
                for (int i = 0; i < Math.Min(20, stats.TopContacts.Count); i++)
                {
                    ContactSummary contact = stats.TopContacts[i];
                    await writer.WriteLineAsync($"| {i + 1} | {contact.ContactKey} | {contact.TotalMessages:N0} | {contact.SentToThem:N0} | {contact.ReceivedFromThem:N0} |");
                }
                await writer.WriteLineAsync();
            }

            AppLogger.Information("Markdown export complete");
        }

        private string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            {
                return "+****";
            }

            string digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 ? $"+****{digits.Substring(digits.Length - 4)}" : "+****";
        }
    }

    /// <summary>
    /// Comprehensive statistics data
    /// </summary>
    public class ComprehensiveStats
    {
        public DateTime GeneratedAt { get; set; }
        public string UserPhone { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int SentMessages { get; set; }
        public int ReceivedMessages { get; set; }
        public int UniqueContacts { get; set; }
        public DateTime FirstMessage { get; set; }
        public DateTime LastMessage { get; set; }
        public TimeSpan DateRange { get; set; }
        public double AverageMessageLength { get; set; }
        public int LongestMessage { get; set; }
        public int ShortestMessage { get; set; }
        public DateTime BusiestDay { get; set; }
        public int BusiestHour { get; set; }
        public Dictionary<string, int> MessagesByDayOfWeek { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MessagesByMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> MessagesByYear { get; set; } = new Dictionary<int, int>();
        public List<ContactSummary> TopContacts { get; set; } = new List<ContactSummary>();
    }

    /// <summary>
    /// Contact summary for statistics
    /// </summary>
    public class ContactSummary
    {
        public string ContactKey { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int SentToThem { get; set; }
        public int ReceivedFromThem { get; set; }
    }
}
