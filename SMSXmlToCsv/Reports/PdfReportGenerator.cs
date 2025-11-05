using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using SMSXmlToCsv.Analysis;
using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Reports
{
    /// <summary>
    /// Generates basic PDF reports from SMS data (original implementation)
    /// </summary>
    public class PdfReportGenerator
    {
        static PdfReportGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Generate comprehensive PDF report (original basic version)
        /// </summary>
        public async Task GenerateComprehensiveReportAsync(
            List<SmsMessage> messages,
            ComprehensiveStats stats,
            string outputPath,
            string userPhone)
        {
            AppLogger.Information($"Generating PDF report: {outputPath}");

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

                        page.Header().Element(ComposeHeader);
                        page.Content().Element(content => ComposeContent(content, messages, stats, userPhone));
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

            AppLogger.Information("PDF report generation complete");
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("SMS Conversation Report").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private void ComposeContent(IContainer container, List<SmsMessage> messages, ComprehensiveStats stats, string userPhone)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // Overview section
                column.Item().Element(c => ComposeOverview(c, stats));

                // Statistics section
                column.Item().Element(c => ComposeStatistics(c, stats));

                // Top contacts section
                column.Item().Element(c => ComposeTopContacts(c, stats));

                // Activity patterns
                column.Item().Element(c => ComposeActivityPatterns(c, stats));

                // Recent messages sample
                column.Item().Element(c => ComposeMessageSample(c, messages.Take(20).ToList()));
            });
        }

        private void ComposeOverview(IContainer container, ComprehensiveStats stats)
        {
            container.Column(column =>
            {
                column.Item().Text("Overview").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(200);
                        columns.RelativeColumn();
                    });

                    table.Cell().Text("Total Messages:").Bold();
                    table.Cell().Text($"{stats.TotalMessages:N0}");

                    table.Cell().Text("Sent:").Bold();
                    table.Cell().Text($"{stats.SentMessages:N0} ({stats.SentMessages * 100.0 / stats.TotalMessages:F1}%)");

                    table.Cell().Text("Received:").Bold();
                    table.Cell().Text($"{stats.ReceivedMessages:N0} ({stats.ReceivedMessages * 100.0 / stats.TotalMessages:F1}%)");

                    table.Cell().Text("Unique Contacts:").Bold();
                    table.Cell().Text($"{stats.UniqueContacts:N0}");

                    table.Cell().Text("Date Range:").Bold();
                    table.Cell().Text($"{stats.FirstMessage:yyyy-MM-dd} to {stats.LastMessage:yyyy-MM-dd}");

                    table.Cell().Text("Total Days:").Bold();
                    table.Cell().Text($"{stats.DateRange.TotalDays:F0} days");

                    table.Cell().Text("Avg per Day:").Bold();
                    table.Cell().Text($"{stats.TotalMessages / Math.Max(1, stats.DateRange.TotalDays):F1} messages");
                });
            });
        }

        private void ComposeStatistics(IContainer container, ComprehensiveStats stats)
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

                    table.Cell().Text("Average Length:").Bold();
                    table.Cell().Text($"{stats.AverageMessageLength:F0} characters");

                    table.Cell().Text("Longest Message:").Bold();
                    table.Cell().Text($"{stats.LongestMessage} characters");

                    table.Cell().Text("Shortest Message:").Bold();
                    table.Cell().Text($"{stats.ShortestMessage} characters");

                    table.Cell().Text("Busiest Day:").Bold();
                    table.Cell().Text($"{stats.BusiestDay:yyyy-MM-dd}");

                    table.Cell().Text("Most Active Hour:").Bold();
                    table.Cell().Text($"{stats.BusiestHour}:00");
                });
            });
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

                    // Header
                    table.Cell().Element(CellStyle).Text("#").Bold();
                    table.Cell().Element(CellStyle).Text("Contact").Bold();
                    table.Cell().Element(CellStyle).Text("Total").Bold();
                    table.Cell().Element(CellStyle).Text("Sent").Bold();
                    table.Cell().Element(CellStyle).Text("Received").Bold();

                    int rank = 1;
                    foreach (ContactSummary contact in stats.TopContacts.Take(10))
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

            static IContainer CellStyle(IContainer container)
            {
                return container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
            }
        }

        private void ComposeActivityPatterns(IContainer container, ComprehensiveStats stats)
        {
            container.Column(column =>
            {
                column.Item().Text("Activity by Day of Week").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                        columns.ConstantColumn(100);
                    });

                    // Header
                    table.Cell().Element(CellStyle).Text("Day").Bold();
                    table.Cell().Element(CellStyle).Text("Messages").Bold();
                    table.Cell().Element(CellStyle).Text("Percentage").Bold();

                    foreach (KeyValuePair<string, int> kvp in stats.MessagesByDayOfWeek.OrderByDescending(k => k.Value))
                    {
                        double percentage = kvp.Value * 100.0 / stats.TotalMessages;
                        table.Cell().Element(CellStyle).Text(kvp.Key);
                        table.Cell().Element(CellStyle).Text($"{kvp.Value:N0}");
                        table.Cell().Element(CellStyle).Text($"{percentage:F1}%");
                    }
                });
            });

            static IContainer CellStyle(IContainer container)
            {
                return container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
            }
        }

        private void ComposeMessageSample(IContainer container, List<SmsMessage> messages)
        {
            container.Column(column =>
            {
                column.Item().Text("Recent Messages Sample (First 20)").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5);

                foreach (SmsMessage msg in messages)
                {
                    column.Item().PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Column(msgColumn =>
                    {
                        msgColumn.Item().Row(row =>
                        {
                            row.AutoItem().Text($"{msg.DateTime:yyyy-MM-dd HH:mm} ").FontSize(9).FontColor(Colors.Grey.Medium);
                            row.AutoItem().Text($"{msg.Direction} ").Bold().FontColor(msg.Direction == "Sent" ? Colors.Blue.Medium : Colors.Red.Medium);
                            row.RelativeItem().Text($"{msg.FromName} ? {msg.ToName}").FontSize(9);
                        });
                        msgColumn.Item().PaddingTop(2).Text(msg.MessageText.Length > 200 ? msg.MessageText.Substring(0, 200) + "..." : msg.MessageText)
                            .FontSize(9);
                    });
                }
            });
        }
    }
}
