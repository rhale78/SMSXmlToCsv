using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Serilog;
using Spectre.Console;
using SMSXmlToCsv.Exporters;
using SMSXmlToCsv.Importers;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services;

namespace SMSXmlToCsv;

public class Program
{
    private static List<Message> _importedMessages = new List<Message>();

    public static void Main(string[] args)
    {
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

            // Ask user if they want to import all or select specific ones
            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Import All",
                        "Select Specific Sources",
                        "Cancel"
                    }));

            if (choice == "Cancel")
            {
                return;
            }

            List<(IDataImporter, string)> toImport = new List<(IDataImporter, string)>();

            if (choice == "Import All")
            {
                toImport.AddRange(detectedImporters);
            }
            else
            {
                // Let user select specific importers
                List<string> options = detectedImporters
                    .Select(d => $"{d.Importer.SourceName} - {Path.GetFileName(d.SourcePath)}")
                    .ToList();

                List<string> selected = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Select sources to import:")
                        .Required()
                        .AddChoices(options));

                toImport = detectedImporters
                    .Where(d => selected.Contains($"{d.Importer.SourceName} - {Path.GetFileName(d.SourcePath)}"))
                    .ToList();
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

        string formatChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select export format:")
                .AddChoices(new[]
                {
                    "CSV (Spreadsheet)",
                    "JSONL (JSON Lines)",
                    "HTML (Chat Interface)",
                    "Parquet (Analytics)",
                    "Cancel"
                }));

        if (formatChoice == "Cancel")
        {
            return;
        }

        string outputDirectory = AnsiConsole.Ask<string>("Enter output directory:", "./exports");
        string baseFileName = AnsiConsole.Ask<string>("Enter base filename:", "messages");

        IDataExporter? exporter = formatChoice switch
        {
            "CSV (Spreadsheet)" => new CsvExporter(),
            "JSONL (JSON Lines)" => new JsonlExporter(),
            "HTML (Chat Interface)" => new HtmlExporter(),
            "Parquet (Analytics)" => new ParquetExporter(),
            _ => null
        };

        if (exporter == null)
        {
            return;
        }

        try
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            AnsiConsole.Status()
                .Start("Exporting messages...", ctx =>
                {
                    ExportPathBuilder pathBuilder = new ExportPathBuilder();
                    ExportOrchestrator orchestrator = new ExportOrchestrator(pathBuilder, Log.Logger);
                    orchestrator.ExportAllInOneAsync(_importedMessages, exporter, outputDirectory, baseFileName)
                        .GetAwaiter().GetResult();
                });

            string outputFile = Path.Combine(outputDirectory, $"{baseFileName}.{exporter.FileExtension}");
            AnsiConsole.MarkupLine($"[green]✓ Successfully exported to {outputFile}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Log.Error(ex, "Error exporting messages");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
