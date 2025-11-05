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
