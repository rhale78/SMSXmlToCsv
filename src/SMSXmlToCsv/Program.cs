using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Spectre.Console;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Importers;
using SMSXmlToCsv.Services;
using SMSXmlToCsv.Exporters;

namespace SMSXmlToCsv;

public class Program
{
    private static List<Message> _importedMessages = new List<Message>();

    public static void Main(string[] args)
    {
        // Register encoding provider for legacy code pages (required by MimeKit for international emails)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Load configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("SMSXmlToCsv Application Starting");

            // Show main menu
            ShowMainMenu(configuration);

            Log.Information("Application completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ShowMainMenu(IConfiguration configuration)
    {
        bool exit = false;

        while (!exit)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("SMS XML to CSV")
                    .LeftJustified()
                    .Color(Color.Blue));

            AnsiConsole.WriteLine();
            
            if (_importedMessages.Any())
            {
                AnsiConsole.MarkupLine($"[green]✓ {_importedMessages.Count} messages loaded[/]");
                AnsiConsole.WriteLine();
            }

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Import Messages",
                        "Export Messages",
                        "Analysis & Reports",
                        "Visualization",
                        "Advanced Tools",
                        "Clear Imported Messages",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Import Messages":
                    ImportMessages();
                    break;

                case "Export Messages":
                    ExportMessages();
                    break;

                case "Analysis & Reports":
                    ShowAnalysisMenu();
                    break;

                case "Visualization":
                    ShowVisualizationMenu();
                    break;

                case "Advanced Tools":
                    ShowAdvancedToolsMenu();
                    break;

                case "Clear Imported Messages":
                    _importedMessages.Clear();
                    AnsiConsole.MarkupLine("[yellow]Messages cleared[/]");
                    AnsiConsole.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;

                case "Exit":
                    exit = true;
                    break;
            }
        }
    }

    private static void ImportMessages()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]Import Messages[/]");
        AnsiConsole.WriteLine();

        string importerChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select import source:")
                .AddChoices(new[]
                {
                    "Auto-Detect (Scan directory)",
                    "Android SMS Backup (XML)",
                    "Facebook Messenger (JSON)",
                    "Instagram Messages (JSON)",
                    "Google Takeout (Hangouts/Voice)",
                    "Gmail (.mbox)",
                    "Cancel"
                }));

        if (importerChoice == "Cancel")
        {
            return;
        }

        // Handle auto-detect separately
        if (importerChoice == "Auto-Detect (Scan directory)")
        {
            ImportMessagesAutoDetect();
            return;
        }

        string sourcePath = AnsiConsole.Ask<string>("Enter the path to the data source:");

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            AnsiConsole.MarkupLine("[red]Error: Path not found[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        IDataImporter? importer = importerChoice switch
        {
            "Android SMS Backup (XML)" => new SmsXmlImporter(),
            "Facebook Messenger (JSON)" => new FacebookMessageImporter(),
            "Instagram Messages (JSON)" => new InstagramMessageImporter(),
            "Google Takeout (Hangouts/Voice)" => new GoogleTakeoutImporter(),
            "Gmail (.mbox)" => new GoogleMailImporter(),
            _ => null
        };

        if (importer == null)
        {
            return;
        }

        try
        {
            AnsiConsole.Status()
                .Start("Importing messages...", ctx =>
                {
                    IEnumerable<Message> messages = importer.ImportAsync(sourcePath).GetAwaiter().GetResult();
                    _importedMessages.AddRange(messages);
                });

            AnsiConsole.MarkupLine($"[green]✓ Successfully imported {_importedMessages.Count} total messages[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error importing messages");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void ImportMessagesAutoDetect()
    {
        string directoryPath = AnsiConsole.Ask<string>("Enter the directory path to scan:");

        if (!Directory.Exists(directoryPath))
        {
            AnsiConsole.MarkupLine("[red]Error: Directory not found[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        ImporterDetectionService detectionService = new ImporterDetectionService();
        
        try
        {
            IEnumerable<(IDataImporter Importer, string SourcePath)> detectedImporters = 
                detectionService.DetectImporters(directoryPath).ToList();

            if (!detectedImporters.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No compatible data sources detected in this directory.[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Display detected importers
            AnsiConsole.MarkupLine($"[green]✓ Found {detectedImporters.Count()} compatible data source(s):[/]");
            AnsiConsole.WriteLine();

            Table table = new Table();
            table.AddColumn("Importer");
            table.AddColumn("Source Path");

            foreach ((IDataImporter Importer, string SourcePath) item in detectedImporters)
            {
                table.AddRow(item.Importer.SourceName, Path.GetFileName(item.SourcePath));
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            List<(IDataImporter, string)> toImport;

            // If only one source, import it directly
            if (detectedImporters.Count() == 1)
            {
                bool confirm = AnsiConsole.Confirm($"Import from [cyan]{detectedImporters.First().Importer.SourceName}[/]?", defaultValue: true);
                
                if (!confirm)
                {
                    return;
                }
                
                toImport = new List<(IDataImporter, string)> { detectedImporters.First() };
            }
            else
            {
                // Multiple sources - use multi-select with ALL pre-selected by default
                List<string> options = detectedImporters
                    .Select(d => $"{d.Importer.SourceName} - {Path.GetFileName(d.SourcePath)}")
                    .ToList();

                MultiSelectionPrompt<string> prompt = new MultiSelectionPrompt<string>()
                    .Title("Select sources to import [dim](use [blue]Space[/] to toggle, [green]Enter[/] to confirm)[/]:")
                    .Required()
                    .InstructionsText("[grey](Press [blue]Space[/] to toggle, [green]Enter[/] to accept)[/]")
                    .AddChoices(options);

                // Pre-select ALL sources by default
                foreach (string option in options)
                {
                    prompt = prompt.Select(option);
                }

                List<string> selected = AnsiConsole.Prompt(prompt);

                toImport = detectedImporters
                    .Where(d => selected.Contains($"{d.Importer.SourceName} - {Path.GetFileName(d.SourcePath)}"))
                    .ToList();
            }

            if (toImport.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No sources selected. Cancelled.[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Import selected sources
            int startCount = _importedMessages.Count;
            
            AnsiConsole.Status()
                .Start("Importing messages...", ctx =>
                {
                    foreach ((IDataImporter importer, string sourcePath) in toImport)
                    {
                        ctx.Status($"Importing from {importer.SourceName}...");
                        
                        try
                        {
                            IEnumerable<Message> messages = importer.ImportAsync(sourcePath).GetAwaiter().GetResult();
                            _importedMessages.AddRange(messages);
                            Log.Information($"Imported {messages.Count()} messages from {importer.SourceName}");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[yellow]Warning: Failed to import from {importer.SourceName}: {ex.Message}[/]");
                            Log.Warning(ex, $"Failed to import from {importer.SourceName}");
                        }
                    }
                });

            int importedCount = _importedMessages.Count - startCount;
            AnsiConsole.MarkupLine($"[green]✓ Successfully imported {importedCount} messages from {toImport.Count} source(s)[/]");
            AnsiConsole.MarkupLine($"[green]✓ Total messages: {_importedMessages.Count}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error during auto-detection");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void ExportMessages()
    {
        if (!_importedMessages.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No messages to export. Please import messages first.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]Export Messages[/]");
        AnsiConsole.WriteLine();

        List<string> formatChoices = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select export format(s) (use [blue]Space[/] to select, [blue]Enter[/] to confirm):")
                .Required()
                .InstructionsText("[grey](Press [blue]Space[/] to toggle, [green]Enter[/] to accept)[/]")
                .AddChoices(new[]
                {
                    "CSV (Spreadsheet)",
                    "JSONL (JSON Lines)",
                    "HTML (Chat Interface)",
                    "Parquet (Analytics)",
                    "SQLite (Database)"
                }));

        // Note: formatChoices will always have at least one item due to .Required() on the prompt
        // This check is kept as a defensive programming practice

        string outputDirectory = AnsiConsole.Ask<string>("Enter output directory:", "./exports");
        string baseFileName = AnsiConsole.Ask<string>("Enter base filename:", "messages");

        List<IDataExporter> exporters = new List<IDataExporter>();
        foreach (string formatChoice in formatChoices)
        {
            IDataExporter? exporter = formatChoice switch
            {
                "CSV (Spreadsheet)" => new CsvExporter(),
                "JSONL (JSON Lines)" => new JsonlExporter(),
                "HTML (Chat Interface)" => new HtmlExporter(),
                "Parquet (Analytics)" => new ParquetExporter(),
                "SQLite (Database)" => new SqliteExporter(),
                _ => null
            };

            if (exporter != null)
            {
                exporters.Add(exporter);
            }
        }

        if (!exporters.Any())
        {
            return;
        }

        try
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            ExportPathBuilder pathBuilder = new ExportPathBuilder();
            ExportOrchestrator orchestrator = new ExportOrchestrator(pathBuilder, Log.Logger);

            int successCount = 0;
            int failCount = 0;
            List<string> exportedFiles = new List<string>();

            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    ProgressTask task = ctx.AddTask("[green]Exporting messages[/]", maxValue: exporters.Count);

                    foreach (IDataExporter exporter in exporters)
                    {
                        string formatName = exporter.FileExtension.ToUpperInvariant();
                        task.Description = $"[green]Exporting to {formatName}...[/]";

                        try
                        {
                            orchestrator.ExportAllInOneAsync(_importedMessages, exporter, outputDirectory, baseFileName)
                                .GetAwaiter().GetResult();
                            
                            string outputFile = Path.Combine(outputDirectory, $"{baseFileName}.{exporter.FileExtension}");
                            exportedFiles.Add(outputFile);
                            successCount++;
                            
                            AnsiConsole.MarkupLine($"[green]✓ {formatName} export completed[/]");
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            AnsiConsole.MarkupLine($"[red]✗ {formatName} export failed: {ex.Message}[/]");
                            Log.Error(ex, $"Error exporting to {formatName}");
                        }

                        task.Increment(1);
                    }
                });

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Export Summary:[/]");
            AnsiConsole.MarkupLine($"[green]✓ Successful: {successCount}[/]");
            if (failCount > 0)
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed: {failCount}[/]");
            }

            if (exportedFiles.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Exported files:[/]");
                foreach (string file in exportedFiles)
                {
                    AnsiConsole.MarkupLine($"  • {file}");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error exporting messages");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void ShowAnalysisMenu()
    {
        if (!_importedMessages.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No messages loaded. Please import messages first.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]Analysis & Reports[/]");
        AnsiConsole.WriteLine();

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select analysis type:")
                .AddChoices(new[]
                {
                    "Thread Analysis",
                    "Response Time Analysis",
                    "Advanced Statistics",
                    "Message Search",
                    "Sentiment Analysis (Requires Ollama)",
                    "Generate PDF Report",
                    "Back to Main Menu"
                }));

        try
        {
            switch (choice)
            {
                case "Thread Analysis":
                    PerformThreadAnalysis();
                    break;

                case "Response Time Analysis":
                    PerformResponseTimeAnalysis();
                    break;

                case "Advanced Statistics":
                    ShowAdvancedStatistics();
                    break;

                case "Message Search":
                    PerformMessageSearch();
                    break;

                case "Sentiment Analysis (Requires Ollama)":
                    PerformSentimentAnalysis();
                    break;

                case "Generate PDF Report":
                    GeneratePdfReport();
                    break;

                case "Back to Main Menu":
                    return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error during analysis");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    private static void ShowVisualizationMenu()
    {
        if (!_importedMessages.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No messages loaded. Please import messages first.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]Visualization[/]");
        AnsiConsole.WriteLine();

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select visualization type:")
                .AddChoices(new[]
                {
                    "Network Graph (All Contacts)",
                    "Network Graph (Per Contact)",
                    "Back to Main Menu"
                }));

        try
        {
            switch (choice)
            {
                case "Network Graph (All Contacts)":
                    GenerateNetworkGraph(perContact: false).Wait();
                    break;

                case "Network Graph (Per Contact)":
                    GenerateNetworkGraph(perContact: true).Wait();
                    break;

                case "Back to Main Menu":
                    return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error during visualization");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    private static void ShowAdvancedToolsMenu()
    {
        if (!_importedMessages.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No messages loaded. Please import messages first.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]Advanced Tools[/]");
        AnsiConsole.WriteLine();

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select tool:")
                .AddChoices(new[]
                {
                    "Contact Merge",
                    "Contact Filter",
                    "Date Range Filter",
                    "Back to Main Menu"
                }));

        try
        {
            switch (choice)
            {
                case "Contact Merge":
                    PerformContactMerge();
                    break;

                case "Contact Filter":
                    ApplyContactFilter();
                    break;

                case "Date Range Filter":
                    ApplyDateRangeFilter();
                    break;

                case "Back to Main Menu":
                    return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error in advanced tools");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    private static void PerformThreadAnalysis()
    {
        int timeout = AnsiConsole.Ask("Thread timeout in minutes:", 30);

        Services.Analysis.ThreadAnalyzer analyzer = new Services.Analysis.ThreadAnalyzer(timeout);
        List<Services.Analysis.ConversationThread> threads = analyzer.DetectThreads(_importedMessages);
        Services.Analysis.ThreadStatistics stats = analyzer.CalculateStatistics(threads);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Thread Analysis Results[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Total Threads: [cyan]{stats.TotalThreads}[/]");
        AnsiConsole.MarkupLine($"Average Thread Length: [cyan]{stats.AverageThreadLength:F1}[/] messages");
        AnsiConsole.MarkupLine($"Longest Thread: [cyan]{stats.LongestThread}[/] messages");
        AnsiConsole.MarkupLine($"Average Duration: [cyan]{stats.AverageThreadDuration.TotalMinutes:F1}[/] minutes");

        if (AnsiConsole.Confirm("Export thread analysis to JSON?"))
        {
            string outputPath = AnsiConsole.Ask("Output file path:", "./output/threads.json");
            analyzer.ExportThreadsAsync(threads, outputPath).Wait();
            AnsiConsole.MarkupLine($"[green]✓ Exported to {outputPath}[/]");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void PerformResponseTimeAnalysis()
    {
        Services.Analysis.ResponseTimeAnalyzer analyzer = new Services.Analysis.ResponseTimeAnalyzer();
        Services.Analysis.ResponseTimeReport report = analyzer.AnalyzeResponseTimes(_importedMessages);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Response Time Analysis Results[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Total Responses: [cyan]{report.TotalResponses}[/]");
        AnsiConsole.MarkupLine($"Average Response Time: [cyan]{FormatTimeSpan(report.AverageResponseTime)}[/]");
        AnsiConsole.MarkupLine($"Median Response Time: [cyan]{FormatTimeSpan(report.MedianResponseTime)}[/]");
        AnsiConsole.MarkupLine($"Fastest: [cyan]{FormatTimeSpan(report.MinResponseTime)}[/]");
        AnsiConsole.MarkupLine($"Slowest: [cyan]{FormatTimeSpan(report.MaxResponseTime)}[/]");

        if (AnsiConsole.Confirm("Export response time analysis to JSON?"))
        {
            string outputPath = AnsiConsole.Ask("Output file path:", "./output/response-times.json");
            analyzer.ExportReportAsync(report, outputPath).Wait();
            AnsiConsole.MarkupLine($"[green]✓ Exported to {outputPath}[/]");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void ShowAdvancedStatistics()
    {
        Services.Analysis.StatisticsAnalyzer analyzer = new Services.Analysis.StatisticsAnalyzer();
        Services.Analysis.MessageStatistics stats = analyzer.AnalyzeMessages(_importedMessages);

        analyzer.DisplayStatistics(stats);

        AnsiConsole.WriteLine();
        string exportChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Export options:")
                .AddChoices(new[] { "Export to JSON", "Export to Markdown", "Don't Export", "Back" }));

        if (exportChoice == "Export to JSON")
        {
            string outputPath = AnsiConsole.Ask("Output file path:", "./output/statistics.json");
            analyzer.ExportToJsonAsync(stats, outputPath).Wait();
            AnsiConsole.MarkupLine($"[green]✓ Exported to {outputPath}[/]");
        }
        else if (exportChoice == "Export to Markdown")
        {
            string outputPath = AnsiConsole.Ask("Output file path:", "./output/statistics.md");
            analyzer.ExportToMarkdownAsync(stats, outputPath).Wait();
            AnsiConsole.MarkupLine($"[green]✓ Exported to {outputPath}[/]");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void PerformMessageSearch()
    {
        Services.Search.MessageSearchService searchService = new Services.Search.MessageSearchService();
        searchService.LoadMessages(_importedMessages);
        searchService.InteractiveSearch();
    }

    private static void PerformSentimentAnalysis()
    {
        string? model = SelectOllamaModelAsync("sentiment analysis").Result;
        
        if (model == null)
        {
            return; // User cancelled or Ollama not available
        }

        Services.ML.OllamaSentimentAnalyzer analyzer = new Services.ML.OllamaSentimentAnalyzer(model);

        int maxMessages = AnsiConsole.Ask("How many messages to analyze? (warning: can be slow)", 10);
        Dictionary<Services.ML.ExtendedSentiment, int> sentimentCounts = new Dictionary<Services.ML.ExtendedSentiment, int>();

        AnsiConsole.Progress()
            .Start(ctx =>
            {
                ProgressTask task = ctx.AddTask("[green]Analyzing sentiment[/]", maxValue: Math.Min(maxMessages, _importedMessages.Count));

                foreach (Message message in _importedMessages.Take(maxMessages))
                {
                    Services.ML.SentimentResult result = analyzer.AnalyzeSentimentAsync(message.Body).Result;

                    if (!sentimentCounts.ContainsKey(result.PrimarySentiment))
                    {
                        sentimentCounts[result.PrimarySentiment] = 0;
                    }
                    sentimentCounts[result.PrimarySentiment]++;

                    task.Increment(1);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Sentiment Analysis Results[/]");
        AnsiConsole.WriteLine();

        foreach (KeyValuePair<Services.ML.ExtendedSentiment, int> kvp in sentimentCounts.OrderByDescending(x => x.Value))
        {
            double percentage = (double)kvp.Value / maxMessages * 100;
            AnsiConsole.MarkupLine($"{kvp.Key}: [cyan]{kvp.Value}[/] ({percentage:F1}%)");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void GeneratePdfReport()
    {
        string outputPath = AnsiConsole.Ask("Output PDF file path:", "./output/report.pdf");

        Services.Analysis.StatisticsAnalyzer statsAnalyzer = new Services.Analysis.StatisticsAnalyzer();
        Services.Analysis.MessageStatistics stats = statsAnalyzer.AnalyzeMessages(_importedMessages);

        Services.Reports.PdfReportGenerator pdfGenerator = new Services.Reports.PdfReportGenerator();

        AnsiConsole.Status()
            .Start("Generating PDF report...", ctx =>
            {
                pdfGenerator.GenerateReport(_importedMessages, outputPath, stats);
            });

        AnsiConsole.MarkupLine($"[green]✓ PDF report generated: {outputPath}[/]");
        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task GenerateNetworkGraph(bool perContact)
    {
        // Smart model selection
        string? model = await SelectOllamaModelAsync("AI topic extraction");
        
        if (model == null)
        {
            return; // User cancelled or Ollama not available
        }

        Services.ML.OllamaSentimentAnalyzer analyzer = new Services.ML.OllamaSentimentAnalyzer(model);

        AnsiConsole.MarkupLine($"[cyan]✓ Using AI ({model}) for topic extraction[/]");
        AnsiConsole.MarkupLine($"[dim]Topics will be extracted using AI analysis of conversation content.[/]");
        AnsiConsole.WriteLine();

        // Ask for network graph options
        bool includeBothSides = AnsiConsole.Confirm("Include topics from YOUR sent messages? (may create busier graph)", defaultValue: false);
        bool includeUserLinks = AnsiConsole.Confirm("Show links from YOU node to topics?", defaultValue: !includeBothSides);
        bool extractEntities = AnsiConsole.Confirm("Extract named entities (people, dates, events)?", defaultValue: true);
        bool skipUnknownContacts = AnsiConsole.Confirm("Skip contacts with 'Unknown' name?", defaultValue: true);
        
        // Create generator options
        var options = new Services.Visualization.NetworkGraphOptions
        {
            IncludeBothSides = includeBothSides,
            IncludeUserLinks = includeUserLinks,
            ExtractNamedEntities = extractEntities,
            ImprovedSpacing = true,
            SkipUnknownContacts = skipUnknownContacts
        };

        // Create generator with analyzer and options
        Services.Visualization.NetworkGraphGenerator generator = 
            new Services.Visualization.NetworkGraphGenerator(analyzer, options);

        try
        {
            if (perContact)
            {
                string outputDirectory = AnsiConsole.Ask("Output directory for per-contact graphs:", "./output/per-contact");
                
                string userName = AnsiConsole.Ask("Your name:", "You");

                // Generate per-contact graphs
                await generator.GeneratePerContactGraphsAsync(_importedMessages, outputDirectory, userName);
            }
            else
            {
                string outputPath = AnsiConsole.Ask("Output file path:", "./output/network-graph.html");
                
                string userName = AnsiConsole.Ask("Your name:", "You");

                // FIXED: Use await instead of .Wait() to avoid deadlock with AnsiConsole.Status()
                await generator.GenerateGraphAsync(_importedMessages, outputPath, userName);
            }

            AnsiConsole.MarkupLine($"[green]✓ Network graph generated successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error generating network graph");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void PerformContactMerge()
    {
        // Get unique contacts from messages
        HashSet<Contact> uniqueContacts = new HashSet<Contact>();
        foreach (Message message in _importedMessages)
        {
            uniqueContacts.Add(message.From);
            uniqueContacts.Add(message.To);
        }

        Services.ContactMergeService mergeService = new Services.ContactMergeService();
        List<Services.ContactMergeCandidate> candidates = mergeService.FindDuplicates(uniqueContacts);

        if (candidates.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No duplicate contacts found![/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Dictionary<string, string> mergeDecisions = mergeService.InteractiveMerge(candidates);

        if (mergeDecisions.Count > 0)
        {
            _importedMessages = mergeService.ApplyMergeDecisions(_importedMessages, mergeDecisions).ToList();
            AnsiConsole.MarkupLine($"[green]✓ Contact merge completed![/]");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void ApplyContactFilter()
    {
        Services.ContactFilterService filterService = new Services.ContactFilterService();
        _importedMessages = filterService.InteractiveFilter(_importedMessages).ToList();

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static void ApplyDateRangeFilter()
    {
        bool useStartDate = AnsiConsole.Confirm("Filter by start date?");
        DateTime? startDate = null;
        if (useStartDate)
        {
            string startDateStr = AnsiConsole.Ask<string>("Enter start date (yyyy-MM-dd):");
            startDate = Services.Filtering.DateRangeFilter.ParseDate(startDateStr);
        }

        bool useEndDate = AnsiConsole.Confirm("Filter by end date?");
        DateTime? endDate = null;
        if (useEndDate)
        {
            string endDateStr = AnsiConsole.Ask<string>("Enter end date (yyyy-MM-dd):");
            endDate = Services.Filtering.DateRangeFilter.ParseDate(endDateStr);
        }

        if (!startDate.HasValue && !endDate.HasValue)
        {
            AnsiConsole.MarkupLine("[yellow]No date filter applied.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Services.Filtering.DateRangeFilter filter = new Services.Filtering.DateRangeFilter(true, startDate, endDate);
        int originalCount = _importedMessages.Count;
        _importedMessages = filter.Filter(_importedMessages).ToList();

        AnsiConsole.MarkupLine($"[green]✓ Filtered from {originalCount} to {_importedMessages.Count} messages[/]");
        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
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

    /// <summary>
    /// Smart model selection for Ollama - checks available models and offers to download if needed
    /// </summary>
    private static async Task<string?> SelectOllamaModelAsync(string purpose = "AI processing")
    {
        // Create a temporary analyzer to check availability
        Services.ML.OllamaSentimentAnalyzer tempAnalyzer = new Services.ML.OllamaSentimentAnalyzer();

        // Check if Ollama is running
        if (!await tempAnalyzer.IsAvailableAsync())
        {
            AnsiConsole.MarkupLine("[red]✗ Ollama is not running or not available.[/]");
            AnsiConsole.MarkupLine("[yellow]Please ensure Ollama is installed and running.[/]");
            AnsiConsole.MarkupLine("[yellow]Install from: https://ollama.ai[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return null;
        }

        // Get available models
        List<string> availableModels = await tempAnalyzer.GetAvailableModelsAsync();

        if (availableModels.Count > 0)
        {
            // Filter to show recommended models that are available, plus any others
            List<string> recommendedAvailable = Services.ML.OllamaSentimentAnalyzer.RecommendedModels
                .Where(rec => availableModels.Any(avail => 
                    avail.Equals(rec, StringComparison.OrdinalIgnoreCase) || 
                    avail.StartsWith(rec.Split(':')[0], StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Add other available models that aren't in the recommended list
            List<string> otherModels = availableModels
                .Where(avail => !recommendedAvailable.Any(rec => 
                    avail.StartsWith(rec.Split(':')[0], StringComparison.OrdinalIgnoreCase)))
                .Take(5) // Limit to avoid overwhelming the user
                .ToList();

            List<string> modelChoices = new List<string>();
            modelChoices.AddRange(recommendedAvailable);
            modelChoices.AddRange(otherModels);
            modelChoices.Add("Download a new model...");

            AnsiConsole.MarkupLine($"[green]✓ Found {availableModels.Count} downloaded model(s)[/]");
            string selectedModel = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select Ollama model for {purpose}:")
                    .AddChoices(modelChoices));

            if (selectedModel == "Download a new model...")
            {
                return await DownloadNewModelAsync();
            }

            return selectedModel;
        }
        else
        {
            // No models available, offer to download one
            AnsiConsole.MarkupLine("[yellow]! No Ollama models are currently downloaded.[/]");
            
            if (AnsiConsole.Confirm("Would you like to download a recommended model?"))
            {
                return await DownloadNewModelAsync();
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Cannot proceed without a model.[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return null;
            }
        }
    }

    /// <summary>
    /// Download a new Ollama model
    /// </summary>
    private static async Task<string?> DownloadNewModelAsync()
    {
        string selectedModel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a model to download:")
                .AddChoices(Services.ML.OllamaSentimentAnalyzer.RecommendedModels)
                .AddChoices(new[] { "Cancel" }));

        if (selectedModel == "Cancel")
        {
            return null;
        }

        AnsiConsole.MarkupLine($"[cyan]Downloading model: {selectedModel}[/]");
        AnsiConsole.MarkupLine("[dim]This may take several minutes depending on the model size and your internet connection...[/]");

        Services.ML.OllamaSentimentAnalyzer analyzer = new Services.ML.OllamaSentimentAnalyzer(selectedModel);
        
        bool success = false;
        await AnsiConsole.Status()
            .StartAsync($"Downloading {selectedModel}...", async ctx =>
            {
                success = await analyzer.PullModelAsync(selectedModel);
            });

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓ Successfully downloaded {selectedModel}[/]");
            return selectedModel;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Failed to download {selectedModel}[/]");
            AnsiConsole.MarkupLine("[yellow]Please check your internet connection and try again.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return null;
        }
    }
}
