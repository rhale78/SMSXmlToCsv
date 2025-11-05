using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Analysis;
using SMSXmlToCsv.Services.ML;
using Serilog;

namespace SMSXmlToCsv.Services.Reports;

/// <summary>
/// Generates PDF reports with message statistics and analysis
/// </summary>
public class PdfReportGenerator
{
    static PdfReportGenerator()
    {
        // QuestPDF License - Community Edition
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generate a comprehensive PDF report
    /// </summary>
    public void GenerateReport(
        IEnumerable<Message> messages,
        string outputPath,
        MessageStatistics? statistics = null,
        ThreadStatistics? threadStats = null,
        ResponseTimeReport? responseTimeReport = null,
        Dictionary<ExtendedSentiment, int>? sentimentCounts = null)
    {
        Log.Information("Generating PDF report: {OutputPath}", outputPath);

        List<Message> messageList = messages.ToList();

        // Calculate statistics if not provided
        if (statistics == null)
        {
            StatisticsAnalyzer analyzer = new StatisticsAnalyzer();
            statistics = analyzer.AnalyzeMessages(messageList);
        }

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header()
                    .AlignCenter()
                    .Text("Message Analysis Report")
                    .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);

                        // Overview Section
                        column.Item().Element(container => RenderOverview(container, statistics));

                        // Message Statistics
                        column.Item().Element(container => RenderMessageStatistics(container, statistics));

                        // Top Contacts
                        column.Item().Element(container => RenderTopContacts(container, statistics));

                        // Thread Statistics (if available)
                        if (threadStats != null)
                        {
                            column.Item().Element(container => RenderThreadStatistics(container, threadStats));
                        }

                        // Response Time Statistics (if available)
                        if (responseTimeReport != null)
                        {
                            column.Item().Element(container => RenderResponseTimeStatistics(container, responseTimeReport));
                        }

                        // Sentiment Analysis (if available)
                        if (sentimentCounts != null && sentimentCounts.Count > 0)
                        {
                            column.Item().Element(container => RenderSentimentAnalysis(container, sentimentCounts));
                        }

                        // Activity Patterns
                        column.Item().Element(container => RenderActivityPatterns(container, statistics));
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Generated: ");
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).SemiBold();
                        text.Span(" | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        })
        .GeneratePdf(outputPath);

        Log.Information("PDF report generated successfully");
    }

    private void RenderOverview(IContainer container, MessageStatistics stats)
    {
        container.Column(column =>
        {
            column.Item().Text("Overview").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Total Messages").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(stats.TotalMessages.ToString("N0")).FontSize(20).SemiBold();
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Sent Messages").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(stats.SentMessages.ToString("N0")).FontSize(20).SemiBold().FontColor(Colors.Green.Darken1);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Received Messages").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(stats.ReceivedMessages.ToString("N0")).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken1);
                });
            });

            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Text($"Date Range: {stats.FirstMessageDate:yyyy-MM-dd} to {stats.LastMessageDate:yyyy-MM-dd}");
                row.RelativeItem().Text($"Duration: {stats.DateRange.Days} days").AlignRight();
            });
        });
    }

    private void RenderMessageStatistics(IContainer container, MessageStatistics stats)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Item().Text("Message Statistics").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Metric").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Value").SemiBold();

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Average Words per Message");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{stats.AverageWordsPerMessage:F1}");

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Words");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(stats.TotalWords.ToString("N0"));

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Average Message Length");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{stats.AverageMessageLength:F0} characters");

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Messages with Attachments");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(stats.MessagesWithAttachments.ToString("N0"));
            });
        });
    }

    private void RenderTopContacts(IContainer container, MessageStatistics stats)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Item().Text("Top 10 Contacts").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Contact").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Total").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Sent").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Received").SemiBold();

                // Data
                foreach (KeyValuePair<string, ContactStats> kvp in stats.ContactStatistics.OrderByDescending(x => x.Value.TotalMessages).Take(10))
                {
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Key);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Value.TotalMessages.ToString("N0"));
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Value.SentMessages.ToString("N0"));
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Value.ReceivedMessages.ToString("N0"));
                }
            });
        });
    }

    private void RenderThreadStatistics(IContainer container, ThreadStatistics stats)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Item().Text("Thread Analysis").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Metric").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Value").SemiBold();

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Threads");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(stats.TotalThreads.ToString("N0"));

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Average Thread Length");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{stats.AverageThreadLength:F1} messages");

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Longest Thread");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{stats.LongestThread} messages");

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Average Duration");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(FormatTimeSpan(stats.AverageThreadDuration));
            });
        });
    }

    private void RenderResponseTimeStatistics(IContainer container, ResponseTimeReport report)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Item().Text("Response Time Analysis").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Metric").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Value").SemiBold();

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Total Responses");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(report.TotalResponses.ToString("N0"));

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Average Response Time");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(FormatTimeSpan(report.AverageResponseTime));

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Median Response Time");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(FormatTimeSpan(report.MedianResponseTime));

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Fastest Response");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(FormatTimeSpan(report.MinResponseTime));

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Slowest Response");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(FormatTimeSpan(report.MaxResponseTime));
            });
        });
    }

    private void RenderSentimentAnalysis(IContainer container, Dictionary<ExtendedSentiment, int> sentimentCounts)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Item().Text("Sentiment Analysis").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            int total = sentimentCounts.Values.Sum();

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Sentiment").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Count").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Percentage").SemiBold();

                foreach (KeyValuePair<ExtendedSentiment, int> kvp in sentimentCounts.OrderByDescending(x => x.Value))
                {
                    double percentage = (double)kvp.Value / total * 100;

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Key.ToString());
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Value.ToString("N0"));
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{percentage:F1}%");
                }
            });
        });
    }

    private void RenderActivityPatterns(IContainer container, MessageStatistics stats)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Item().Text("Activity Patterns").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(10).Text("Messages by Hour of Day").FontSize(14).SemiBold();

            // Render as table instead of chart
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                });

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Hour").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Message Count").SemiBold();

                for (int hour = 0; hour < 24; hour++)
                {
                    int count = stats.MessagesByHour.ContainsKey(hour) ? stats.MessagesByHour[hour] : 0;

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{hour:D2}:00");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(count.ToString("N0"));
                }
            });

            column.Item().PageBreak();

            column.Item().Text("Messages by Day of Week").FontSize(14).SemiBold();

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                });

                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Day").SemiBold();
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3).Text("Message Count").SemiBold();

                foreach (KeyValuePair<DayOfWeek, int> kvp in stats.MessagesByDayOfWeek.OrderBy(x => (int)x.Key))
                {
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Key.ToString());
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kvp.Value.ToString("N0"));
                }
            });
        });
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalMinutes < 1)
        {
            return $"{timeSpan.TotalSeconds:F0} seconds";
        }
        else if (timeSpan.TotalHours < 1)
        {
            return $"{timeSpan.TotalMinutes:F0} minutes";
        }
        else if (timeSpan.TotalDays < 1)
        {
            return $"{timeSpan.TotalHours:F1} hours";
        }
        else
        {
            return $"{timeSpan.TotalDays:F1} days";
        }
    }
}
