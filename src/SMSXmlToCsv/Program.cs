using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Spectre.Console;
using SMSXmlToCsv.Configuration;
using SMSXmlToCsv.Services;

namespace SMSXmlToCsv;

public class Program
{
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

            // Check if this is a backup-only run (called from build)
            if (args.Length > 0 && args[0] == "--backup-only")
            {
                PerformBackup(configuration);
                return;
            }

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

    private static void PerformBackup(IConfiguration configuration)
    {
        try
        {
            BackupSettings backupSettings = new BackupSettings();
            configuration.GetSection("BackupSettings").Bind(backupSettings);

            if (!backupSettings.Enabled)
            {
                return;
            }

            PathBuilder pathBuilder = new PathBuilder();
            SolutionBackupOrchestrator orchestrator = new SolutionBackupOrchestrator(
                backupSettings, 
                pathBuilder, 
                Log.Logger);

            // Backup from solution root (3 levels up from bin/Debug/net9.0)
            string solutionDirectory = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), 
                "../../../.."));

            orchestrator.BackupSolution(solutionDirectory);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during backup operation");
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

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Import Messages",
                        "Export Messages",
                        "Backup Project Now",
                        "Settings",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Import Messages":
                    AnsiConsole.MarkupLine("[yellow]Import feature coming soon...[/]");
                    AnsiConsole.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;

                case "Export Messages":
                    AnsiConsole.MarkupLine("[yellow]Export feature coming soon...[/]");
                    AnsiConsole.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;

                case "Backup Project Now":
                    AnsiConsole.Status()
                        .Start("Creating backup...", ctx =>
                        {
                            PerformBackup(configuration);
                        });
                    AnsiConsole.MarkupLine("[green]Backup completed![/]");
                    AnsiConsole.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;

                case "Settings":
                    AnsiConsole.MarkupLine("[yellow]Settings feature coming soon...[/]");
                    AnsiConsole.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;

                case "Exit":
                    exit = true;
                    break;
            }
        }
    }
}

