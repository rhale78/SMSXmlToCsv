using System.Globalization;
using System.Text;
using System.Text.Json;

using Parquet;
using Parquet.Data;
using Parquet.Schema;

using SMSXmlToCsv.Analysis;
using SMSXmlToCsv.Logging;
using SMSXmlToCsv.ML;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Reports;

namespace SMSXmlToCsv
{
    /// <summary>
    /// Splits messages into per-contact folders and files
    /// </summary>
    public class ContactSplitter
    {
        private readonly string _basePath;
        private readonly string _contactsFolderName;
        private readonly string _userPhone;
        private readonly HashSet<string> _selectedColumns;

        public ContactSplitter(string basePath, string contactsFolderName, string userPhone, HashSet<string>? selectedColumns = null)
        {
            _basePath = basePath;
            _contactsFolderName = contactsFolderName;
            _userPhone = userPhone;
            _selectedColumns = selectedColumns ?? FeatureConfiguration.GetDefaultColumns();
        }

        /// <summary>
        /// Split messages by contact and export to separate folders
        /// </summary>
        public async Task SplitMessagesByContactAsync(
            List<SmsMessage> allMessages,
            HashSet<OutputFormat> formats,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null,
            HashSet<string>? selectedContacts = null,
            bool generatePdfReports = false,
            string userPhone = "",
            SMSXmlToCsv.ML.OllamaIntegration? ollama = null,
            bool enableSentimentAnalysis = false)
        {
            SMSXmlToCsv.Utils.ConsoleHelper.WriteLine("\n📁 Splitting messages by contact...", ConsoleColor.Cyan);

            // Group messages by contact
            Dictionary<string, List<SmsMessage>> contactMessages = GroupMessagesByContact(allMessages, selectedContacts);

            SMSXmlToCsv.Utils.ConsoleHelper.WriteLine($"  Processing {contactMessages.Count} contacts...", ConsoleColor.Gray);

            int processedCount = 0;
            int totalContacts = contactMessages.Count;

            foreach (KeyValuePair<string, List<SmsMessage>> kvp in contactMessages)
            {
                string contactKey = kvp.Key;
                List<SmsMessage> messages = kvp.Value;

                // Parse contact info
                string[] parts = contactKey.Split('|');
                string contactName = parts[0];
                string contactPhone = parts[1];

                // Create sanitized folder name
                string folderName = CreateContactFolderName(contactName, contactPhone);
                string contactFolder = Path.Combine(_basePath, _contactsFolderName, folderName);
                Directory.CreateDirectory(contactFolder);

                // Export messages directly in contact folder
                string basePath = Path.Combine(contactFolder, folderName);

                // Show progress
                processedCount++;
                double percentage = (processedCount * 100.0) / totalContacts;
                SMSXmlToCsv.Utils.ConsoleHelper.ClearLine();
                SMSXmlToCsv.Utils.ConsoleHelper.Write($"  📁 Processing {processedCount}/{totalContacts} ({percentage:F1}%) - ", ConsoleColor.Cyan);
                SMSXmlToCsv.Utils.ConsoleHelper.Write(contactName, ConsoleColor.White);
                SMSXmlToCsv.Utils.ConsoleHelper.Write("...          ", ConsoleColor.Gray);

                // Export standard formats
                foreach (OutputFormat format in formats)
                {
                    await ExportContactMessagesAsync(messages, basePath, format, mmsAttachments);
                }

                // Generate per-contact PDF report if enabled OR run sentiment analysis
                if ((generatePdfReports || enableSentimentAnalysis) && messages.Count > 0 && ollama != null)
                {
                    try
                    {
                        string? pdfPath = generatePdfReports ? Path.Combine(contactFolder, $"{folderName}_report.pdf") : null;

                        List<string>? topics = null;
                        SMSXmlToCsv.Analysis.SentimentAnalysisResults? sentiment = null;
                        SMSXmlToCsv.Analysis.ResponseTimeStats? responseStats = null;

                        // Sentiment analysis
                        SentimentAnalyzer sentimentAnalyzer = new SMSXmlToCsv.Analysis.SentimentAnalyzer(ollama);
                        sentiment = await AnalyzeContactSentimentWithProgressAsync(
                            sentimentAnalyzer,
                      messages,
                            contactName,
                               (current, total, etaAndSentiments) =>
                       {
                           Console.Write($"\r  📊 {contactName}: {current}/{total} ({current * 100.0 / total:F1}%) - ETA: {etaAndSentiments}          ");
                       });

                        Console.Write($"\r  ✓ Sentiment complete for {contactName}   \r");

                        // Response time analysis
                        ResponseTimeAnalyzer responseAnalyzer = new SMSXmlToCsv.Analysis.ResponseTimeAnalyzer();
                        Dictionary<string, ResponseTimeStats> responseResults = responseAnalyzer.AnalyzeResponseTimes(messages, userPhone);
                        if (responseResults.ContainsKey(contactKey))
                        {
                            responseStats = responseResults[contactKey];
                        }

                        // Generate PDF only if requested
                        if (generatePdfReports && pdfPath != null)
                        {
                            EnhancedPdfReportGenerator pdfGen = new SMSXmlToCsv.Reports.EnhancedPdfReportGenerator();
                            await pdfGen.GenerateContactReportAsync(
                         contactName,
                                       contactPhone,
                                   messages,
                                  pdfPath,
                          userPhone,
                                       topics,
                              sentiment,
                                  responseStats);

                            AppLogger.Information($"Generated PDF report for {contactName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed analysis for {contactName}: {ex.Message}");
                    }
                }
            }

            SMSXmlToCsv.Utils.ConsoleHelper.WriteLine($"\r✓ Split messages for {contactMessages.Count} contacts        ", ConsoleColor.Green);

            if (generatePdfReports || enableSentimentAnalysis)
            {
                SMSXmlToCsv.Utils.ConsoleHelper.WriteLine($"✓ Completed AI analysis for {contactMessages.Count} contacts", ConsoleColor.Green);
            }
        }

        /// <summary>
        /// Analyze contact sentiment with progress tracking and ETA
        /// </summary>
        private async Task<SMSXmlToCsv.Analysis.SentimentAnalysisResults> AnalyzeContactSentimentWithProgressAsync(
            SMSXmlToCsv.Analysis.SentimentAnalyzer analyzer,
            List<SmsMessage> messages,
            string contactName,
            Action<int, int, string> progressCallback)
        {
            SentimentAnalysisResults results = new SMSXmlToCsv.Analysis.SentimentAnalysisResults
            {
                TotalAnalyzed = messages.Count
            };

            DateTime startTime = DateTime.Now;
            int batchSize = 50;
            DateTime? etaCalculated = null;

            for (int i = 0; i < messages.Count; i++)
            {
                SmsMessage msg = messages[i];
                SentimentResult result = await analyzer.AnalyzeSingleMessageAsync(msg.MessageText);
                results.Results.Add(result);

                // Count sentiments
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

                // Extract extended sentiments
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

                // Calculate ETA after first batch
                if (i == batchSize && !etaCalculated.HasValue)
                {
                    TimeSpan elapsed = DateTime.Now - startTime;
                    double msPerMessage = elapsed.TotalMilliseconds / batchSize;
                    int remaining = messages.Count - batchSize;
                    etaCalculated = DateTime.Now.AddMilliseconds(msPerMessage * remaining);
                }

                // Update progress every 10 messages or on first batch completion
                if ((i + 1) % 10 == 0 || i == batchSize - 1 || i == messages.Count - 1)
                {
                    string eta = "Calculating...";
                    if (etaCalculated.HasValue)
                    {
                        TimeSpan remainingTime = etaCalculated.Value - DateTime.Now;
                        if (remainingTime.TotalSeconds < 60)
                        {
                            eta = $"{remainingTime.TotalSeconds:F0}s";
                        }
                        else if (remainingTime.TotalMinutes < 60)
                        {
                            eta = $"{remainingTime.TotalMinutes:F1}m";
                        }
                        else
                        {
                            eta = $"{remainingTime.TotalHours:F1}h";
                        }
                    }

                    // Get top sentiments for display
                    string topSentiments = GetTopSentimentsDisplay(results, i + 1);
                    progressCallback(i + 1, messages.Count, eta + topSentiments);
                }
            }

            // Calculate temporal sentiment patterns
            results.CalculateTemporalPatterns(messages);

            return results;
        }

        /// <summary>
        /// Get top 3-5 sentiments with percentages for progress display
        /// </summary>
        private string GetTopSentimentsDisplay(SMSXmlToCsv.Analysis.SentimentAnalysisResults results, int currentCount)
        {
            if (currentCount == 0)
            {
                return "";
            }

            List<(string Name, int Count, double Percentage)> sentiments = new List<(string Name, int Count, double Percentage)>();

            // Add ALL sentiment types
            if (results.PositiveCount > 0)
            {
                sentiments.Add(("Pos", results.PositiveCount, results.PositiveCount * 100.0 / currentCount));
            }

            if (results.NegativeCount > 0)
            {
                sentiments.Add(("Neg", results.NegativeCount, results.NegativeCount * 100.0 / currentCount));
            }

            if (results.NeutralCount > 0)
            {
                sentiments.Add(("Neu", results.NeutralCount, results.NeutralCount * 100.0 / currentCount));
            }

            if (results.FriendlyCount > 0)
            {
                sentiments.Add(("Friendly", results.FriendlyCount, results.FriendlyCount * 100.0 / currentCount));
            }

            if (results.ProfessionalCount > 0)
            {
                sentiments.Add(("Prof", results.ProfessionalCount, results.ProfessionalCount * 100.0 / currentCount));
            }

            if (results.CasualCount > 0)
            {
                sentiments.Add(("Casual", results.CasualCount, results.CasualCount * 100.0 / currentCount));
            }

            if (results.FormalCount > 0)
            {
                sentiments.Add(("Formal", results.FormalCount, results.FormalCount * 100.0 / currentCount));
            }

            if (results.CombativeCount > 0)
            {
                sentiments.Add(("Combative", results.CombativeCount, results.CombativeCount * 100.0 / currentCount));
            }

            if (results.ArgumentativeCount > 0)
            {
                sentiments.Add(("Argumentative", results.ArgumentativeCount, results.ArgumentativeCount * 100.0 / currentCount));
            }

            // Show top 4 sentiments (increased from 3)
            List<(string Name, int Count, double Percentage)> top = sentiments.OrderByDescending(s => s.Count).Take(4).ToList();

            if (top.Count == 0)
            {
                return "";
            }

            IEnumerable<string> parts = top.Select(s => $"{s.Name}:{s.Percentage:F0}%");
            return $" | {string.Join(", ", parts)}";
        }

        /// <summary>
        /// Group messages by contact (excluding self)
        /// </summary>
        private Dictionary<string, List<SmsMessage>> GroupMessagesByContact(
            List<SmsMessage> allMessages,
            HashSet<string>? selectedContacts)
        {
            Dictionary<string, List<SmsMessage>> grouped = new Dictionary<string, List<SmsMessage>>();

            foreach (SmsMessage msg in allMessages)
            {
                // Determine the contact (not self)
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;

                // Skip self-messages
                if (contactPhone == _userPhone)
                {
                    continue;
                }

                string key = $"{contactName}|{contactPhone}";

                // Apply filter if specified
                if (selectedContacts != null && selectedContacts.Count > 0)
                {
                    if (!selectedContacts.Contains(key))
                    {
                        continue;
                    }
                }

                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = new List<SmsMessage>();
                }

                grouped[key].Add(msg);
            }

            return grouped;
        }

        /// <summary>
        /// Create a sanitized folder name for a contact
        /// </summary>
        private string CreateContactFolderName(string name, string phone)
        {
            // Sanitize name only (no phone number masking)
            string sanitizedName = SanitizeName(name);

            // Return just the sanitized name
            return string.IsNullOrEmpty(sanitizedName) ? "Unknown" : sanitizedName;
        }

        /// <summary>
        /// Sanitize contact name for folder naming
        /// </summary>
        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "(Unknown)")
            {
                return "Unknown";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

            // Remove leading/trailing spaces and dots (Windows doesn't allow these)
            sanitized = sanitized.Trim(' ', '.');

            // Replace spaces with underscores
            sanitized = sanitized.Replace(' ', '_');

            // Remove any multiple consecutive underscores
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');

            // Limit length
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50).TrimEnd('_', '.', ' ');
            }

            return string.IsNullOrEmpty(sanitized) ? "Unknown" : sanitized;
        }

        /// <summary>
        /// Mask phone number for privacy (show last 4 digits)
        /// </summary>
        private string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            {
                return "****";
            }

            // Remove non-digits
            string digits = new string(phone.Where(char.IsDigit).ToArray());

            return digits.Length >= 4 ? $"+****{digits.Substring(digits.Length - 4)}" : "****";
        }

        /// <summary>
        /// Export contact messages in specified format
        /// </summary>
        private async Task ExportContactMessagesAsync(
            List<SmsMessage> messages,
            string basePath,
            OutputFormat format,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            string outputFile = format switch
            {
                OutputFormat.Csv => $"{basePath}.csv",
                OutputFormat.JsonLines => $"{basePath}.jsonl",
                OutputFormat.Parquet => $"{basePath}.parquet",
                _ => throw new ArgumentException("Invalid format")
            };

            switch (format)
            {
                case OutputFormat.Csv:
                    await WriteCsvAsync(messages, outputFile, mmsAttachments);
                    break;
                case OutputFormat.JsonLines:
                    await WriteJsonLinesAsync(messages, outputFile, mmsAttachments);
                    break;
                case OutputFormat.Parquet:
                    await WriteParquetAsync(messages, outputFile, mmsAttachments);
                    break;
            }
        }

        /// <summary>
        /// Write messages to CSV format
        /// </summary>
        private async Task WriteCsvAsync(
            List<SmsMessage> messages,
            string outputFile,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                // Build header from selected columns
                List<string> headerColumns = new List<string>();
                if (_selectedColumns.Contains("FromName"))
                {
                    headerColumns.Add("From_Name");
                }

                if (_selectedColumns.Contains("FromPhone"))
                {
                    headerColumns.Add("From_Phone");
                }

                if (_selectedColumns.Contains("ToName"))
                {
                    headerColumns.Add("To_Name");
                }

                if (_selectedColumns.Contains("ToPhone"))
                {
                    headerColumns.Add("To_Phone");
                }

                if (_selectedColumns.Contains("Direction"))
                {
                    headerColumns.Add("Direction");
                }

                if (_selectedColumns.Contains("DateTime"))
                {
                    headerColumns.Add("DateTime");
                }

                if (_selectedColumns.Contains("UnixTimestamp"))
                {
                    headerColumns.Add("UnixTimestamp");
                }

                if (_selectedColumns.Contains("MessageText"))
                {
                    headerColumns.Add("MessageText");
                }

                if (_selectedColumns.Contains("HasMMS"))
                {
                    headerColumns.Add("HasMMS");
                }

                if (_selectedColumns.Contains("MMS_Files"))
                {
                    headerColumns.Add("MMS_Files");
                }

                await writer.WriteLineAsync(string.Join(",", headerColumns));

                // Write data
                foreach (SmsMessage msg in messages)
                {
                    bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp);
                    string mmsFiles = string.Empty;

                    if (hasMms)
                    {
                        List<MmsAttachment> attachments = mmsAttachments[msg.UnixTimestamp];
                        mmsFiles = string.Join("; ", attachments.Select(a => a.FilePath));
                    }

                    List<string> rowValues = new List<string>();
                    if (_selectedColumns.Contains("FromName"))
                    {
                        rowValues.Add(EscapeCsv(msg.FromName));
                    }

                    if (_selectedColumns.Contains("FromPhone"))
                    {
                        rowValues.Add(EscapeCsv(msg.FromPhone));
                    }

                    if (_selectedColumns.Contains("ToName"))
                    {
                        rowValues.Add(EscapeCsv(msg.ToName));
                    }

                    if (_selectedColumns.Contains("ToPhone"))
                    {
                        rowValues.Add(EscapeCsv(msg.ToPhone));
                    }

                    if (_selectedColumns.Contains("Direction"))
                    {
                        rowValues.Add(msg.Direction);
                    }

                    if (_selectedColumns.Contains("DateTime"))
                    {
                        rowValues.Add(msg.DateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }

                    if (_selectedColumns.Contains("UnixTimestamp"))
                    {
                        rowValues.Add(msg.UnixTimestamp.ToString());
                    }

                    if (_selectedColumns.Contains("MessageText"))
                    {
                        rowValues.Add(EscapeCsv(msg.MessageText));
                    }

                    if (_selectedColumns.Contains("HasMMS"))
                    {
                        rowValues.Add(hasMms.ToString());
                    }

                    if (_selectedColumns.Contains("MMS_Files"))
                    {
                        rowValues.Add(EscapeCsv(mmsFiles));
                    }

                    await writer.WriteLineAsync(string.Join(",", rowValues));
                }
            }
        }

        private string EscapeCsv(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        /// <summary>
        /// Write messages to JSON Lines format
        /// </summary>
        private async Task WriteJsonLinesAsync(
            List<SmsMessage> messages,
            string outputFile,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                foreach (SmsMessage msg in messages)
                {
                    bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(msg.UnixTimestamp);
                    List<string>? mmsFiles = null;

                    if (hasMms)
                    {
                        mmsFiles = mmsAttachments[msg.UnixTimestamp].Select(a => a.FilePath).ToList();
                    }

                    // Build dynamic object with only selected columns
                    Dictionary<string, object?> jsonMsg = new Dictionary<string, object?>();

                    if (_selectedColumns.Contains("FromName"))
                    {
                        jsonMsg["fromName"] = msg.FromName;
                    }

                    if (_selectedColumns.Contains("FromPhone"))
                    {
                        jsonMsg["fromPhone"] = msg.FromPhone;
                    }

                    if (_selectedColumns.Contains("ToName"))
                    {
                        jsonMsg["toName"] = msg.ToName;
                    }

                    if (_selectedColumns.Contains("ToPhone"))
                    {
                        jsonMsg["toPhone"] = msg.ToPhone;
                    }

                    if (_selectedColumns.Contains("Direction"))
                    {
                        jsonMsg["direction"] = msg.Direction;
                    }

                    if (_selectedColumns.Contains("DateTime"))
                    {
                        jsonMsg["dateTime"] = msg.DateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                    }

                    if (_selectedColumns.Contains("UnixTimestamp"))
                    {
                        jsonMsg["unixTimestamp"] = msg.UnixTimestamp;
                    }

                    if (_selectedColumns.Contains("MessageText"))
                    {
                        jsonMsg["messageText"] = msg.MessageText;
                    }

                    if (_selectedColumns.Contains("HasMMS"))
                    {
                        jsonMsg["hasMMS"] = hasMms;
                    }

                    if (_selectedColumns.Contains("MMS_Files"))
                    {
                        jsonMsg["mmsFiles"] = mmsFiles;
                    }

                    string json = JsonSerializer.Serialize(jsonMsg, options);
                    await writer.WriteLineAsync(json);
                }
            }
        }

        /// <summary>
        /// Write messages to Parquet format
        /// </summary>
        private async Task WriteParquetAsync(
            List<SmsMessage> messages,
            string outputFile,
            Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            // Build schema dynamically based on selected columns
            List<DataField> fields = new List<DataField>();

            if (_selectedColumns.Contains("FromName"))
            {
                fields.Add(new DataField<string>("from_name"));
            }

            if (_selectedColumns.Contains("FromPhone"))
            {
                fields.Add(new DataField<string>("from_phone"));
            }

            if (_selectedColumns.Contains("ToName"))
            {
                fields.Add(new DataField<string>("to_name"));
            }

            if (_selectedColumns.Contains("ToPhone"))
            {
                fields.Add(new DataField<string>("to_phone"));
            }

            if (_selectedColumns.Contains("Direction"))
            {
                fields.Add(new DataField<string>("direction"));
            }

            if (_selectedColumns.Contains("DateTime"))
            {
                fields.Add(new DataField<DateTime>("date_time"));
            }

            if (_selectedColumns.Contains("UnixTimestamp"))
            {
                fields.Add(new DataField<long>("unix_timestamp"));
            }

            if (_selectedColumns.Contains("MessageText"))
            {
                fields.Add(new DataField<string>("message_text"));
            }

            if (_selectedColumns.Contains("HasMMS"))
            {
                fields.Add(new DataField<bool>("has_mms"));
            }

            if (_selectedColumns.Contains("MMS_Files"))
            {
                fields.Add(new DataField<string>("mms_files"));
            }

            ParquetSchema schema = new ParquetSchema(fields.ToArray());

            // Prepare data arrays for selected columns only
            Dictionary<string, Array> columnData = new Dictionary<string, Array>();

            if (_selectedColumns.Contains("FromName"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].FromName;
                }

                columnData["from_name"] = data;
            }
            if (_selectedColumns.Contains("FromPhone"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].FromPhone;
                }

                columnData["from_phone"] = data;
            }
            if (_selectedColumns.Contains("ToName"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].ToName;
                }

                columnData["to_name"] = data;
            }
            if (_selectedColumns.Contains("ToPhone"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].ToPhone;
                }

                columnData["to_phone"] = data;
            }
            if (_selectedColumns.Contains("Direction"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].Direction;
                }

                columnData["direction"] = data;
            }
            if (_selectedColumns.Contains("DateTime"))
            {
                DateTime[] data = new DateTime[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].DateTime;
                }

                columnData["date_time"] = data;
            }
            if (_selectedColumns.Contains("UnixTimestamp"))
            {
                long[] data = new long[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].UnixTimestamp;
                }

                columnData["unix_timestamp"] = data;
            }
            if (_selectedColumns.Contains("MessageText"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = messages[i].MessageText;
                }

                columnData["message_text"] = data;
            }
            if (_selectedColumns.Contains("HasMMS"))
            {
                bool[] data = new bool[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    data[i] = mmsAttachments != null && mmsAttachments.ContainsKey(messages[i].UnixTimestamp);
                }
                columnData["has_mms"] = data;
            }
            if (_selectedColumns.Contains("MMS_Files"))
            {
                string[] data = new string[messages.Count];
                for (int i = 0; i < messages.Count; i++)
                {
                    bool hasMms = mmsAttachments != null && mmsAttachments.ContainsKey(messages[i].UnixTimestamp);
                    data[i] = hasMms
                        ? string.Join("; ", mmsAttachments[messages[i].UnixTimestamp].Select(a => a.FilePath))
                        : string.Empty;
                }
                columnData["mms_files"] = data;
            }

            using (Stream fileStream = File.Create(outputFile))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(schema, fileStream))
                {
                    writer.CompressionMethod = CompressionMethod.Snappy;

                    using (ParquetRowGroupWriter groupWriter = writer.CreateRowGroup())
                    {
                        // Write columns in schema order
                        for (int fieldIdx = 0; fieldIdx < fields.Count; fieldIdx++)
                        {
                            DataField field = fields[fieldIdx];
                            Array data = columnData[field.Name];
                            await groupWriter.WriteColumnAsync(new DataColumn(field, data));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extended JSON message with MMS support
    /// </summary>
    public class JsonSmsMessageExtended
    {
        public string FromName { get; set; } = string.Empty;
        public string FromPhone { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string ToPhone { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
        public long UnixTimestamp { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public bool HasMMS { get; set; }
        public List<string>? MmsFiles { get; set; }
    }
}
