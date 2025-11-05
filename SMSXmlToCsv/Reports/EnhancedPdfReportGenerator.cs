using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using SMSXmlToCsv.Analysis;
using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Reports
{
    /// <summary>
    /// Enhanced PDF report generator with AI insights, topic analysis, and visualizations
    /// </summary>
    public class EnhancedPdfReportGenerator
    {
        static EnhancedPdfReportGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Generate comprehensive overview PDF report (all contacts combined)
        /// </summary>
        public async Task GenerateComprehensiveReportAsync(
            List<SmsMessage> messages,
            ComprehensiveStats stats,
            string outputPath,
            string userPhone,
            SentimentAnalysisResults? sentimentResults = null,
            Dictionary<string, ResponseTimeStats>? responseStats = null,
            Dictionary<string, int>? topicFrequency = null)
        {
            AppLogger.Information($"Generating comprehensive PDF report: {outputPath}");

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                        page.Header().Element(c => ComposeHeader(c, "Comprehensive SMS Report"));
                        page.Content().Element(content => ComposeComprehensiveContent(
                            content, messages, stats, userPhone, sentimentResults, responseStats, topicFrequency));
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                }).GeneratePdf(outputPath);
            });

            AppLogger.Information("Comprehensive PDF report complete");
        }

        /// <summary>
        /// Generate per-contact PDF report (when split by contact is enabled)
        /// </summary>
        public async Task GenerateContactReportAsync(
            string contactName,
            string contactPhone,
            List<SmsMessage> contactMessages,
            string outputPath,
            string userPhone,
            List<string>? contactTopics = null,
            SentimentAnalysisResults? contactSentiment = null,
            ResponseTimeStats? contactResponseStats = null)
        {
            AppLogger.Information($"Generating contact PDF report for {contactName}: {outputPath}");

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                        page.Header().Element(c => ComposeHeader(c, $"Contact Report: {contactName}"));
                        page.Content().Element(content => ComposeContactContent(
                            content, contactName, contactPhone, contactMessages, userPhone,
                            contactTopics, contactSentiment, contactResponseStats));
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                }).GeneratePdf(outputPath);
            });

            AppLogger.Information($"Contact PDF report complete for {contactName}");
        }

        private void ComposeHeader(IContainer container, string title)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(title).FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private void ComposeComprehensiveContent(
            IContainer container,
            List<SmsMessage> messages,
            ComprehensiveStats stats,
            string userPhone,
            SentimentAnalysisResults? sentimentResults,
            Dictionary<string, ResponseTimeStats>? responseStats,
            Dictionary<string, int>? topicFrequency)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Executive Summary
                column.Item().Element(c => ComposeExecutiveSummary(c, stats, sentimentResults));

                // Message Statistics
                column.Item().Element(c => ComposeOverview(c, stats));

                // Sentiment Analysis (if available)
                if (sentimentResults != null)
                {
                    column.Item().PageBreak();
                    column.Item().Element(c => ComposeSentimentAnalysis(c, sentimentResults));
                }

                // Topic Analysis (if available)
                if (topicFrequency != null && topicFrequency.Count > 0)
                {
                    column.Item().PageBreak();
                    column.Item().Element(c => ComposeTopicAnalysis(c, topicFrequency));
                }

                // Activity Patterns
                column.Item().PageBreak();
                column.Item().Element(c => ComposeActivityPatterns(c, stats, messages));

                // Response Time Analysis (if available)
                if (responseStats != null && responseStats.Count > 0)
                {
                    column.Item().PageBreak();
                    column.Item().Element(c => ComposeResponseTimeAnalysis(c, responseStats));
                }

                // Top Contacts
                column.Item().PageBreak();
                column.Item().Element(c => ComposeTopContacts(c, stats));

                // Message Frequency Over Time
                column.Item().PageBreak();
                column.Item().Element(c => ComposeMessageFrequency(c, messages));
            });
        }

        private void ComposeContactContent(
            IContainer container,
            string contactName,
            string contactPhone,
            List<SmsMessage> messages,
            string userPhone,
            List<string>? topics,
            SentimentAnalysisResults? sentiment,
            ResponseTimeStats? responseStats)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Contact Overview
                column.Item().Element(c => ComposeContactOverview(c, contactName, contactPhone, messages, userPhone));

                // Topics Discussed (if available)
                if (topics != null && topics.Count > 0)
                {
                    column.Item().Element(c => ComposeContactTopics(c, topics));
                }

                // Sentiment (if available)
                if (sentiment != null)
                {
                    column.Item().Element(c => ComposeSentimentAnalysis(c, sentiment));
                }

                // Response Times (if available)
                if (responseStats != null)
                {
                    column.Item().Element(c => ComposeContactResponseTimes(c, responseStats));
                }

                // Message Activity
                column.Item().PageBreak();
                column.Item().Element(c => ComposeContactActivity(c, messages));

                // Recent Messages Sample
                column.Item().PageBreak();
                column.Item().Element(c => ComposeMessageSample(c, messages.OrderByDescending(m => m.DateTime).Take(30).ToList(), contactName));
            });
        }

        private void ComposeExecutiveSummary(IContainer container, ComprehensiveStats stats, SentimentAnalysisResults? sentiment)
        {
            container.Column(column =>
            {
                column.Item().Text("Executive Summary").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10).Text(text =>
                {
                    text.Span("This report analyzes ").FontSize(10);
                    text.Span($"{stats.TotalMessages:N0} messages").Bold().FontColor(Colors.Blue.Medium);
                    text.Span($" exchanged with ").FontSize(10);
                    text.Span($"{stats.UniqueContacts} contacts").Bold().FontColor(Colors.Blue.Medium);
                    text.Span($" over ").FontSize(10);
                    text.Span($"{stats.DateRange.TotalDays:F0} days").Bold().FontColor(Colors.Blue.Medium);
                    text.Span($" ({stats.FirstMessage:yyyy-MM-dd} to {stats.LastMessage:yyyy-MM-dd}).").FontSize(10);
                });

                if (sentiment != null)
                {
                    double positivePercentage = sentiment.TotalAnalyzed > 0 ? sentiment.PositiveCount * 100.0 / sentiment.TotalAnalyzed : 0;
                    double neutralPercentage = sentiment.TotalAnalyzed > 0 ? sentiment.NeutralCount * 100.0 / sentiment.TotalAnalyzed : 0;
                    double negativePercentage = sentiment.TotalAnalyzed > 0 ? sentiment.NegativeCount * 100.0 / sentiment.TotalAnalyzed : 0;

                    column.Item().PaddingTop(10).Text(text =>
                    {
                        text.Span("Sentiment: ").FontSize(10).Bold();
                        text.Span($"{positivePercentage:F1}% Positive, ").FontColor(Colors.Green.Medium);
                        text.Span($"{neutralPercentage:F1}% Neutral, ").FontColor(Colors.Grey.Medium);
                        text.Span($"{negativePercentage:F1}% Negative").FontColor(Colors.Red.Medium);
                    });
                }
            });
        }

        private void ComposeOverview(IContainer container, ComprehensiveStats stats)
        {
            container.Column(column =>
            {
                column.Item().Text("Message Statistics").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(200);
                        columns.RelativeColumn();
                    });

                    AddStatRow(table, "Total Messages:", $"{stats.TotalMessages:N0}");
                    AddStatRow(table, "Sent:", $"{stats.SentMessages:N0} ({stats.SentMessages * 100.0 / stats.TotalMessages:F1}%)");
                    AddStatRow(table, "Received:", $"{stats.ReceivedMessages:N0} ({stats.ReceivedMessages * 100.0 / stats.TotalMessages:F1}%)");
                    AddStatRow(table, "Unique Contacts:", $"{stats.UniqueContacts:N0}");
                    AddStatRow(table, "Date Range:", $"{stats.FirstMessage:yyyy-MM-dd} to {stats.LastMessage:yyyy-MM-dd}");
                    AddStatRow(table, "Total Days:", $"{stats.DateRange.TotalDays:F0} days");
                    AddStatRow(table, "Avg per Day:", $"{stats.TotalMessages / Math.Max(1, stats.DateRange.TotalDays):F1} messages");
                    AddStatRow(table, "Average Length:", $"{stats.AverageMessageLength:F0} characters");
                    AddStatRow(table, "Busiest Day:", $"{stats.BusiestDay:yyyy-MM-dd}");
                    AddStatRow(table, "Most Active Hour:", $"{stats.BusiestHour}:00");
                });
            });
        }

        private void ComposeSentimentAnalysis(IContainer container, SentimentAnalysisResults sentiment)
        {
            Dictionary<string, (int Count, double Percentage)> nonZeroSentiments = sentiment.GetNonZeroSentiments();

            if (nonZeroSentiments.Count == 0)
            {
                return; // Skip if no sentiment data
            }

            container.Column(column =>
            {
                column.Item().Text("Sentiment Analysis").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10).Text($"Based on analysis of {sentiment.TotalAnalyzed:N0} messages:").FontSize(10);

                // Display in grid format with color coding
                column.Item().PaddingTop(10).Row(row =>
                {
                    int itemsPerRow = 3;
                    int itemIndex = 0;

                    foreach (KeyValuePair<string, (int Count, double Percentage)> kvp in nonZeroSentiments.OrderByDescending(s => s.Value.Count))
                    {
                        (int count, double percentage) = kvp.Value;
                        Color color = kvp.Key.ToLower() switch
                        {
                            "positive" => Colors.Green.Medium,
                            "negative" => Colors.Red.Medium,
                            "neutral" => Colors.Grey.Medium,
                            "professional" => Colors.Blue.Medium,
                            "friendly" => Colors.Teal.Medium,
                            "combative" => Colors.Orange.Medium,
                            "argumentative" => Colors.Red.Lighten1,
                            "casual" => Colors.Purple.Lighten2,
                            "formal" => Colors.Blue.Darken2,
                            _ => Colors.Grey.Medium
                        };

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignCenter().Text(kvp.Key).Bold().FontColor(color);
                            col.Item().AlignCenter().PaddingTop(5).Text($"{count:N0}").FontSize(20).Bold().FontColor(color);
                            col.Item().AlignCenter().Text($"{percentage:F1}%").FontSize(11).FontColor(color);
                        });

                        itemIndex++;
                        if (itemIndex % itemsPerRow == 0 && itemIndex < nonZeroSentiments.Count)
                        {
                            // This would need a new row in a more complex layout
                            // For simplicity, we'll just continue in same row with wrapping
                        }
                    }
                });

                // Add legend for extended sentiments if present
                if (nonZeroSentiments.Any(s => s.Key != "Positive" && s.Key != "Negative" && s.Key != "Neutral"))
                {
                    column.Item().PaddingTop(10).Text("Extended sentiment analysis includes tone, formality, and interaction style.").FontSize(9).FontColor(Colors.Grey.Medium);
                }

                // Add temporal sentiment patterns if available
                if (sentiment.SentimentByPeriod.Count > 0)
                {
                    column.Item().PaddingTop(15).Text("Sentiment Over Time").FontSize(13).Bold().FontColor(Colors.Blue.Medium);

                    var grouping = sentiment.SentimentByPeriod.First().Value.Grouping;
                    column.Item().PaddingTop(5).Text($"Sentiment trends grouped by {grouping}:").FontSize(10);

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("Period").Bold();
                        table.Cell().Element(CellStyle).Text("Messages").Bold();
                        table.Cell().Element(CellStyle).Text("Positive").Bold();
                        table.Cell().Element(CellStyle).Text("Neutral").Bold();
                        table.Cell().Element(CellStyle).Text("Negative").Bold();
                        table.Cell().Element(CellStyle).Text("Sentiment Bar").Bold();

                        // Show temporal data (limit to last 12 periods for readability)
                        List<KeyValuePair<string, TemporalSentiment>> periodsToShow = sentiment.SentimentByPeriod
                            .OrderBy(p => p.Key)
                            .TakeLast(12)
                            .ToList();

                        foreach (KeyValuePair<string, TemporalSentiment> period in periodsToShow)
                        {
                            table.Cell().Element(CellStyle).Text(period.Key);
                            table.Cell().Element(CellStyle).Text($"{period.Value.TotalMessages:N0}");
                            table.Cell().Element(CellStyle).Text($"{period.Value.PositivePercentage:F0}%").FontColor(Colors.Green.Medium);
                            table.Cell().Element(CellStyle).Text($"{period.Value.NeutralPercentage:F0}%").FontColor(Colors.Grey.Medium);
                            table.Cell().Element(CellStyle).Text($"{period.Value.NegativePercentage:F0}%").FontColor(Colors.Red.Medium);

                            // Visual sentiment bar
                            table.Cell().Element(CellStyle).Row(row =>
                            {
                                int totalWidth = 100;
                                int positiveWidth = (int)(period.Value.PositivePercentage * totalWidth / 100);
                                int neutralWidth = (int)(period.Value.NeutralPercentage * totalWidth / 100);
                                int negativeWidth = totalWidth - positiveWidth - neutralWidth;

                                if (positiveWidth > 0)
                                {
                                    row.ConstantItem(positiveWidth).Height(10).Background(Colors.Green.Medium);
                                }

                                if (neutralWidth > 0)
                                {
                                    row.ConstantItem(neutralWidth).Height(10).Background(Colors.Grey.Lighten1);
                                }

                                if (negativeWidth > 0)
                                {
                                    row.ConstantItem(negativeWidth).Height(10).Background(Colors.Red.Medium);
                                }

                                row.RelativeItem();
                            });
                        }
                    });
                }
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeTopicAnalysis(IContainer container, Dictionary<string, int> topicFrequency)
        {
            container.Column(column =>
            {
                column.Item().Text("AI-Detected Topics").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10).Text($"Top {Math.Min(15, topicFrequency.Count)} topics discussed across all conversations:").FontSize(10);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                        columns.ConstantColumn(100);
                    });

                    // Header
                    table.Cell().Element(CellStyle).Text("Rank").Bold();
                    table.Cell().Element(CellStyle).Text("Topic").Bold();
                    table.Cell().Element(CellStyle).Text("Messages").Bold();
                    table.Cell().Element(CellStyle).Text("Percentage").Bold();

                    int rank = 1;
                    int total = topicFrequency.Values.Sum();

                    // FIX: Ensure no duplicate topics by grouping and filtering zero-count topics
                    List<KeyValuePair<string, int>> uniqueTopics = topicFrequency
                        .Where(t => t.Value > 0) // Filter out zero-count topics
                        .OrderByDescending(t => t.Value)
                        .Take(15)
                        .ToList();

                    foreach (KeyValuePair<string, int> topic in uniqueTopics)
                    {
                        double percentage = total > 0 ? topic.Value * 100.0 / total : 0;
                        table.Cell().Element(CellStyle).Text(rank.ToString());
                        table.Cell().Element(CellStyle).Text(topic.Key);
                        table.Cell().Element(CellStyle).Text($"{topic.Value:N0}");
                        table.Cell().Element(CellStyle).Text($"{percentage:F1}%");
                        rank++;
                    }

                    // If no valid topics, show message
                    if (uniqueTopics.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).Element(CellStyle).Text("No topic data available").FontColor(Colors.Grey.Medium);
                    }
                });
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeActivityPatterns(IContainer container, ComprehensiveStats stats, List<SmsMessage> messages)
        {
            container.Column(column =>
            {
                column.Item().Text("Activity Patterns").FontSize(16).Bold().FontColor(Colors.Blue.Medium);

                // Day of Week
                column.Item().PaddingTop(10).Text("Messages by Day of Week").FontSize(13).Bold();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(100);
                        columns.ConstantColumn(100);
                        columns.RelativeColumn(3);
                    });

                    table.Cell().Element(CellStyle).Text("Day").Bold();
                    table.Cell().Element(CellStyle).Text("Messages").Bold();
                    table.Cell().Element(CellStyle).Text("Percentage").Bold();
                    table.Cell().Element(CellStyle).Text("Visual").Bold();

                    var maxMessages = stats.MessagesByDayOfWeek.Values.Max();
                    foreach (KeyValuePair<string, int> kvp in stats.MessagesByDayOfWeek.OrderByDescending(k => k.Value))
                    {
                        double percentage = kvp.Value * 100.0 / stats.TotalMessages;
                        int barWidth = (int)(kvp.Value * 100.0 / maxMessages);
                        int actualBarWidth = Math.Min(barWidth, 75) * 2; // Cap at 150 points total

                        table.Cell().Element(CellStyle).Text(kvp.Key);
                        table.Cell().Element(CellStyle).Text($"{kvp.Value:N0}");
                        table.Cell().Element(CellStyle).Text($"{percentage:F1}%");
                        table.Cell().Element(CellStyle).Row(row =>
                        {
                            row.ConstantItem(actualBarWidth).Height(15).Background(Colors.Blue.Medium);
                            row.RelativeItem();
                        });
                    }
                });

                // Hour of Day
                column.Item().PaddingTop(15).Text("Messages by Hour of Day").FontSize(13).Bold();
                column.Item().PaddingTop(5).Text("Most active hours:").FontSize(10);

                var hourlyActivity = messages
                    .GroupBy(m => m.DateTime.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .OrderByDescending(h => h.Count)
                    .Take(5)
                    .ToList();

                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(100);
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(CellStyle).Text("Hour").Bold();
                    table.Cell().Element(CellStyle).Text("Messages").Bold();
                    table.Cell().Element(CellStyle).Text("Visual").Bold();

                    var maxHourly = hourlyActivity.Max(h => h.Count);
                    foreach (var hour in hourlyActivity)
                    {
                        int barWidth = (int)(hour.Count * 100.0 / maxHourly);
                        int actualBarWidth = Math.Min(barWidth, 50) * 3; // Cap at 150 points total
                        string timeRange = $"{hour.Hour:D2}:00-{hour.Hour:D2}:59";

                        table.Cell().Element(CellStyle).Text(timeRange);
                        table.Cell().Element(CellStyle).Text($"{hour.Count:N0}");
                        table.Cell().Element(CellStyle).Row(row =>
                        {
                            row.ConstantItem(actualBarWidth).Height(15).Background(Colors.Blue.Medium);
                            row.RelativeItem();
                        });
                    }
                });
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeResponseTimeAnalysis(IContainer container, Dictionary<string, ResponseTimeStats> responseStats)
        {
            container.Column(column =>
            {
                column.Item().Text("Response Time Analysis").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10).Text($"Response patterns for top {Math.Min(10, responseStats.Count)} contacts:").FontSize(10);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(90);
                    });
                    table.Cell().Element(CellStyle).Text("Contact").Bold();
                    table.Cell().Element(CellStyle).Text("Your Avg").Bold();
                    table.Cell().Element(CellStyle).Text("Their Avg").Bold();

                    foreach (KeyValuePair<string, ResponseTimeStats> kvp in responseStats.OrderByDescending(s => s.Value.TotalExchanges).Take(10))
                    {
                        string contactName = kvp.Value.ContactKey.Split('|')[0];
                        table.Cell().Element(CellStyle).Text(contactName);
                        table.Cell().Element(CellStyle).Text(FormatTimeSpan(kvp.Value.OurAverageResponseTime));
                        table.Cell().Element(CellStyle).Text(FormatTimeSpan(kvp.Value.TheirAverageResponseTime));
                    }
                });
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeTopContacts(IContainer container, ComprehensiveStats stats)
        {
            container.Column(column =>
            {
                column.Item().Text("Top 10 Contacts").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Cell().Element(CellStyle).Text("#").Bold();
                    table.Cell().Element(CellStyle).Text("Contact").Bold();
                    table.Cell().Element(CellStyle).Text("Total").Bold();
                    table.Cell().Element(CellStyle).Text("Sent").Bold();
                    table.Cell().Element(CellStyle).Text("Received").Bold();

                    int rank = 1;
                    foreach (ContactSummary? contact in stats.TopContacts.Take(10))
                    {
                        table.Cell().Element(CellStyle).Text(rank.ToString());
                        table.Cell().Element(CellStyle).Text(contact.ContactKey.Split('|')[0]);
                        table.Cell().Element(CellStyle).Text($"{contact.TotalMessages:N0}");
                        table.Cell().Element(CellStyle).Text($"{contact.SentToThem:N0}");
                        table.Cell().Element(CellStyle).Text($"{contact.ReceivedFromThem:N0}");
                        rank++;
                    }
                });
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeMessageFrequency(IContainer container, List<SmsMessage> messages)
        {
            container.Column(column =>
            {
                column.Item().Text("Message Frequency Over Time").FontSize(16).Bold().FontColor(Colors.Blue.Medium);

                // Group by month
                var monthlyActivity = messages
                    .GroupBy(m => new { Year = m.DateTime.Year, Month = m.DateTime.Month })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        Sent = g.Count(m => m.Direction == "Sent"),
                        Received = g.Count(m => m.Direction == "Received")
                    })
                    .OrderBy(m => m.Date)
                    .ToList();

                if (monthlyActivity.Count > 0)
                {
                    // FIX: Better date range display
                    DateTime firstDate = monthlyActivity.First().Date;
                    DateTime lastDate = monthlyActivity.Last().Date;
                    DateTime earliestMessage = messages.Min(m => m.DateTime);
                    DateTime latestMessage = messages.Max(m => m.DateTime);

                    // Check if there's a gap between earliest message and first activity shown
                    if (earliestMessage.Year < firstDate.Year ||
                        (earliestMessage.Year == firstDate.Year && earliestMessage.Month < firstDate.Month))
                    {
                        column.Item().PaddingTop(10).Text($"Showing activity from {firstDate:MMM yyyy} to {lastDate:MMM yyyy}").FontSize(10);
                        column.Item().Text($"(Earliest message: {earliestMessage:MMM yyyy}, Latest: {latestMessage:MMM yyyy})").FontSize(9).FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        column.Item().PaddingTop(10).Text($"Monthly activity from {firstDate:MMM yyyy} to {lastDate:MMM yyyy}").FontSize(10);
                    }

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("Month").Bold();
                        table.Cell().Element(CellStyle).Text("Total").Bold();
                        table.Cell().Element(CellStyle).Text("Sent").Bold();
                        table.Cell().Element(CellStyle).Text("Received").Bold();
                        table.Cell().Element(CellStyle).Text("Visual").Bold();

                        var maxMonthly = monthlyActivity.Max(m => m.Count);

                        // FIX: Show last 24 months if there are more than 24, otherwise show all
                        var monthsToShow = monthlyActivity.Count <= 24 ? monthlyActivity : monthlyActivity.TakeLast(24);

                        foreach (var month in monthsToShow)
                        {
                            int barWidth = maxMonthly > 0 ? (int)(month.Count * 100.0 / maxMonthly) : 0;
                            int actualBarWidth = Math.Min(barWidth, 75) * 2; // Cap at 150 points total

                            table.Cell().Element(CellStyle).Text(month.Date.ToString("MMM yyyy"));
                            table.Cell().Element(CellStyle).Text($"{month.Count:N0}");
                            table.Cell().Element(CellStyle).Text($"{month.Sent:N0}");
                            table.Cell().Element(CellStyle).Text($"{month.Received:N0}");
                            table.Cell().Element(CellStyle).Row(row =>
                            {
                                if (actualBarWidth > 0)
                                {
                                    row.ConstantItem(actualBarWidth).Height(15).Background(Colors.Blue.Medium);
                                }
                                row.RelativeItem();
                            });
                        }
                    });
                }
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeContactOverview(IContainer container, string contactName, string contactPhone, List<SmsMessage> messages, string userPhone)
        {
            var sent = messages.Count(m => m.Direction == "Sent");
            var received = messages.Count - sent;
            DateTime firstMessage = messages.Min(m => m.DateTime);
            DateTime lastMessage = messages.Max(m => m.DateTime);
            var avgLength = messages.Average(m => m.MessageText.Length);

            container.Column(column =>
            {
                column.Item().Text("Contact Overview").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(200);
                        columns.RelativeColumn();
                    });

                    AddStatRow(table, "Contact Name:", contactName);
                    AddStatRow(table, "Phone Number:", contactPhone);
                    AddStatRow(table, "Total Messages:", $"{messages.Count:N0}");
                    AddStatRow(table, "Sent:", $"{sent:N0} ({sent * 100.0 / messages.Count:F1}%)");
                    AddStatRow(table, "Received:", $"{received:N0} ({received * 100.0 / messages.Count:F1}%)");
                    AddStatRow(table, "First Message:", firstMessage.ToString("yyyy-MM-dd HH:mm"));
                    AddStatRow(table, "Last Message:", lastMessage.ToString("yyyy-MM-dd HH:mm"));
                    AddStatRow(table, "Conversation Span:", $"{(lastMessage - firstMessage).TotalDays:F0} days");
                    AddStatRow(table, "Average Length:", $"{avgLength:F0} characters");
                });
            });
        }

        private void ComposeContactTopics(IContainer container, List<string> topics)
        {
            container.Column(column =>
            {
                column.Item().Text("Topics Discussed").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10).Text($"Top {topics.Count} topics in this conversation (AI-detected):").FontSize(10);

                column.Item().PaddingTop(10).Column(col =>
                {
                    foreach (var topic in topics.Take(10))
                    {
                        col.Item().PaddingVertical(3).Row(row =>
                        {
                            row.ConstantItem(15).Height(15).Background(Colors.Blue.Lighten2);
                            row.ConstantItem(10);
                            row.RelativeItem().Text(topic).FontSize(11);
                        });
                    }
                });
            });
        }

        private void ComposeContactResponseTimes(IContainer container, ResponseTimeStats responseStats)
        {
            container.Column(column =>
            {
                column.Item().Text("Response Time Analysis").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(200);
                        columns.RelativeColumn();
                    });

                    AddStatRow(table, "Total Exchanges:", $"{responseStats.TotalExchanges:N0}");
                    AddStatRow(table, "Your Avg Response:", FormatTimeSpan(responseStats.OurAverageResponseTime));
                    AddStatRow(table, "Their Avg Response:", FormatTimeSpan(responseStats.TheirAverageResponseTime));
                    AddStatRow(table, "Your Median Response:", FormatTimeSpan(responseStats.OurMedianResponseTime));
                    AddStatRow(table, "Their Median Response:", FormatTimeSpan(responseStats.TheirMedianResponseTime));
                    AddStatRow(table, "Your Fastest:", FormatTimeSpan(responseStats.OurFastestResponse));
                    AddStatRow(table, "Their Fastest:", FormatTimeSpan(responseStats.TheirFastestResponse));
                });
            });
        }

        private void ComposeContactActivity(IContainer container, List<SmsMessage> messages)
        {
            container.Column(column =>
            {
                column.Item().Text("Message Activity").FontSize(16).Bold().FontColor(Colors.Blue.Medium);

                // By day of week
                var dayActivity = messages
                    .GroupBy(m => m.DateTime.DayOfWeek)
                    .Select(g => new { Day = g.Key.ToString(), Count = g.Count() })
                    .OrderByDescending(d => d.Count)
                    .ToList();

                column.Item().PaddingTop(10).Text("Most active days:").FontSize(11).Bold();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(100);
                        columns.ConstantColumn(80);
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(CellStyle).Text("Day").Bold();
                    table.Cell().Element(CellStyle).Text("Messages").Bold();
                    table.Cell().Element(CellStyle).Text("Visual").Bold();

                    var maxDay = dayActivity.Max(d => d.Count);
                    foreach (var day in dayActivity.Take(3))
                    {
                        int barWidth = (int)(day.Count * 100.0 / maxDay);
                        int actualBarWidth = Math.Min(barWidth, 75) * 2; // Cap at 150 points total

                        table.Cell().Element(CellStyle).Text(day.Day);
                        table.Cell().Element(CellStyle).Text($"{day.Count:N0}");
                        table.Cell().Element(CellStyle).Row(row =>
                        {
                            row.ConstantItem(actualBarWidth).Height(15).Background(Colors.Blue.Medium);
                            row.RelativeItem();
                        });
                    }
                });
            });

            static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private void ComposeMessageSample(IContainer container, List<SmsMessage> messages, string contactName)
        {
            container.Column(column =>
            {
                column.Item().Text($"Recent Messages (Last {Math.Min(30, messages.Count)})").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5);

                foreach (SmsMessage? msg in messages.Take(30))
                {
                    column.Item().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Column(msgColumn =>
                    {
                        msgColumn.Item().Row(row =>
                        {
                            row.AutoItem().Text($"{msg.DateTime:yyyy-MM-dd HH:mm} ").FontSize(9).FontColor(Colors.Grey.Medium);
                            row.AutoItem().Text($"{msg.Direction} ").Bold().FontColor(msg.Direction == "Sent" ? Colors.Blue.Medium : Colors.Red.Medium);
                        });
                        msgColumn.Item().PaddingTop(2).Text(msg.MessageText.Length > 200 ? msg.MessageText.Substring(0, 200) + "..." : msg.MessageText)
                            .FontSize(9);
                    });
                }
            });
        }

        private void AddStatRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Text(label).Bold();
            table.Cell().Text(value);
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
            {
                return $"{ts.TotalDays:F1} days";
            }

            return ts.TotalHours >= 1
                ? $"{ts.TotalHours:F1} hours"
                : ts.TotalMinutes >= 1 ? $"{ts.TotalMinutes:F0} min" : $"{ts.TotalSeconds:F0} sec";
        }
    }
}
