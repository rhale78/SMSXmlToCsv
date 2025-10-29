using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Analysis
{
    /// <summary>
    /// Analyzes message patterns to detect conversation threads
    /// </summary>
    public class ConversationThreadAnalyzer
    {
        private readonly TimeSpan _threadTimeoutMinutes;
        private readonly int _minimumThreadLength;

        public ConversationThreadAnalyzer(int threadTimeoutMinutes = 60, int minimumThreadLength = 2)
        {
            _threadTimeoutMinutes = TimeSpan.FromMinutes(threadTimeoutMinutes);
            _minimumThreadLength = minimumThreadLength;
        }

        /// <summary>
        /// Detect conversation threads in messages
        /// </summary>
        public List<ConversationThread> DetectThreads(List<SmsMessage> messages)
        {
            AppLogger.Information($"Starting thread detection for {messages.Count} messages");

            List<ConversationThread> threads = new List<ConversationThread>();
            ConversationThread? currentThread = null;
            DateTime lastMessageTime = DateTime.MinValue;
            string? lastContactKey = null;

            foreach (SmsMessage msg in messages.OrderBy(m => m.UnixTimestamp))
            {
                string contactKey = GetContactKey(msg);
                TimeSpan timeSinceLastMessage = msg.DateTime - lastMessageTime;

                // Start new thread if contact changed or timeout exceeded
                if (contactKey != lastContactKey || timeSinceLastMessage > _threadTimeoutMinutes)
                {
                    // Save previous thread if it meets minimum length
                    if (currentThread != null && currentThread.Messages.Count >= _minimumThreadLength)
                    {
                        threads.Add(currentThread);
                    }

                    // Start new thread
                    currentThread = new ConversationThread
                    {
                        ThreadId = Guid.NewGuid(),
                        ContactKey = contactKey,
                        StartTime = msg.DateTime,
                        Messages = new List<SmsMessage>()
                    };
                }

                // Add message to current thread
                currentThread?.Messages.Add(msg);
                currentThread!.EndTime = msg.DateTime;

                lastMessageTime = msg.DateTime;
                lastContactKey = contactKey;
            }

            // Add final thread
            if (currentThread != null && currentThread.Messages.Count >= _minimumThreadLength)
            {
                threads.Add(currentThread);
            }

            AppLogger.Information($"Detected {threads.Count} conversation threads");
            return threads;
        }

        /// <summary>
        /// Calculate thread statistics
        /// </summary>
        public ThreadStatistics CalculateStatistics(List<ConversationThread> threads)
        {
            return threads.Count == 0
                ? new ThreadStatistics()
                : new ThreadStatistics
                {
                    TotalThreads = threads.Count,
                    AverageThreadLength = threads.Average(t => t.Messages.Count),
                    LongestThread = threads.Max(t => t.Messages.Count),
                    ShortestThread = threads.Min(t => t.Messages.Count),
                    AverageThreadDuration = TimeSpan.FromSeconds(threads.Average(t => t.Duration.TotalSeconds)),
                    TotalMessages = threads.Sum(t => t.Messages.Count),
                    ThreadsPerContact = threads.GroupBy(t => t.ContactKey).ToDictionary(g => g.Key, g => g.Count())
                };
        }

        /// <summary>
        /// Export threads to JSON format
        /// </summary>
        public async Task ExportThreadsAsync(List<ConversationThread> threads, string outputPath)
        {
            AppLogger.Information($"Exporting {threads.Count} threads to {outputPath}");

            using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync("{");
                await writer.WriteLineAsync($"  \"totalThreads\": {threads.Count},");
                await writer.WriteLineAsync("  \"threads\": [");

                for (int i = 0; i < threads.Count; i++)
                {
                    ConversationThread thread = threads[i];
                    string comma = i < threads.Count - 1 ? "," : "";

                    await writer.WriteLineAsync("    {");
                    await writer.WriteLineAsync($"      \"threadId\": \"{thread.ThreadId}\",");
                    await writer.WriteLineAsync($"      \"contactKey\": \"{thread.ContactKey}\",");
                    await writer.WriteLineAsync($"      \"startTime\": \"{thread.StartTime:yyyy-MM-ddTHH:mm:ss}\",");
                    await writer.WriteLineAsync($"      \"endTime\": \"{thread.EndTime:yyyy-MM-ddTHH:mm:ss}\",");
                    await writer.WriteLineAsync($"      \"duration\": \"{thread.Duration}\",");
                    await writer.WriteLineAsync($"      \"messageCount\": {thread.Messages.Count},");
                    await writer.WriteLineAsync($"      \"averageResponseTime\": \"{thread.AverageResponseTime}\"");
                    await writer.WriteLineAsync($"    }}{comma}");
                }

                await writer.WriteLineAsync("  ]");
                await writer.WriteLineAsync("}");
            }

            AppLogger.Information("Thread export complete");
        }

        private string GetContactKey(SmsMessage msg)
        {
            string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
            string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;
            return $"{contactName}|{contactPhone}";
        }
    }

    /// <summary>
    /// Represents a conversation thread
    /// </summary>
    public class ConversationThread
    {
        public Guid ThreadId { get; set; }
        public string ContactKey { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<SmsMessage> Messages { get; set; } = new List<SmsMessage>();

        public TimeSpan Duration => EndTime - StartTime;
        public int MessageCount => Messages.Count;

        public TimeSpan AverageResponseTime
        {
            get
            {
                if (Messages.Count < 2)
                {
                    return TimeSpan.Zero;
                }

                List<TimeSpan> responseTimes = new List<TimeSpan>();
                for (int i = 1; i < Messages.Count; i++)
                {
                    responseTimes.Add(Messages[i].DateTime - Messages[i - 1].DateTime);
                }

                return TimeSpan.FromSeconds(responseTimes.Average(t => t.TotalSeconds));
            }
        }
    }

    /// <summary>
    /// Thread statistics
    /// </summary>
    public class ThreadStatistics
    {
        public int TotalThreads { get; set; }
        public double AverageThreadLength { get; set; }
        public int LongestThread { get; set; }
        public int ShortestThread { get; set; }
        public TimeSpan AverageThreadDuration { get; set; }
        public int TotalMessages { get; set; }
        public Dictionary<string, int> ThreadsPerContact { get; set; } = new Dictionary<string, int>();
    }
}
