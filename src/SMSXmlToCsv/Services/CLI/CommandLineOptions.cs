using System;
using System.Collections.Generic;
using System.Linq;

namespace SMSXmlToCsv.Services.CLI;

/// <summary>
/// Command-line options for the application
/// </summary>
public class CommandLineOptions
{
    public string? InputFile { get; set; }
    public string? OutputDirectory { get; set; }
    public List<string> ExportFormats { get; set; } = new List<string>();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> SelectedContacts { get; set; } = new List<string>();
    public bool ContinueOnError { get; set; }
    public bool SaveErrorReport { get; set; }
    public bool EnableThreadAnalysis { get; set; }
    public bool EnableResponseTimeAnalysis { get; set; }
    public bool EnableStatistics { get; set; }
    public bool Interactive { get; set; } = true;
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }

    /// <summary>
    /// Parse command-line arguments
    /// </summary>
    public static CommandLineOptions Parse(string[] args)
    {
        CommandLineOptions options = new CommandLineOptions();

        if (args.Length == 0)
        {
            options.Interactive = true;
            return options;
        }

        options.Interactive = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg.ToLowerInvariant())
            {
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    return options;

                case "--version":
                case "-v":
                    options.ShowVersion = true;
                    return options;

                case "--input":
                case "-i":
                    if (i + 1 < args.Length)
                    {
                        options.InputFile = args[++i];
                    }
                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputDirectory = args[++i];
                    }
                    break;

                case "--formats":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        string formatsArg = args[++i];
                        options.ExportFormats = formatsArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(f => f.Trim().ToLowerInvariant())
                            .ToList();
                    }
                    break;

                case "--start-date":
                    if (i + 1 < args.Length)
                    {
                        if (DateTime.TryParse(args[++i], out DateTime startDate))
                        {
                            options.StartDate = startDate;
                        }
                    }
                    break;

                case "--end-date":
                    if (i + 1 < args.Length)
                    {
                        if (DateTime.TryParse(args[++i], out DateTime endDate))
                        {
                            options.EndDate = endDate;
                        }
                    }
                    break;

                case "--contacts":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        string contactsArg = args[++i];
                        options.SelectedContacts = contactsArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(c => c.Trim())
                            .ToList();
                    }
                    break;

                case "--continue-on-error":
                    options.ContinueOnError = true;
                    break;

                case "--save-error-report":
                    options.SaveErrorReport = true;
                    break;

                case "--thread-analysis":
                    options.EnableThreadAnalysis = true;
                    break;

                case "--response-time":
                    options.EnableResponseTimeAnalysis = true;
                    break;

                case "--statistics":
                case "--stats":
                    options.EnableStatistics = true;
                    break;

                case "--interactive":
                    options.Interactive = true;
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Display help text
    /// </summary>
    public static void DisplayHelp()
    {
        Console.WriteLine("SMSXmlToCsv - Message Import and Export Tool");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  SMSXmlToCsv [OPTIONS]");
        Console.WriteLine("  SMSXmlToCsv (no arguments for interactive mode)");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -h, --help                    Show this help message");
        Console.WriteLine("  -v, --version                 Show version information");
        Console.WriteLine("  -i, --input <file>            Input file path");
        Console.WriteLine("  -o, --output <directory>      Output directory path");
        Console.WriteLine("  -f, --formats <formats>       Export formats (comma-separated)");
        Console.WriteLine("                                Values: csv, json, html, parquet, sqlite");
        Console.WriteLine("  --start-date <date>           Filter messages from this date (yyyy-MM-dd)");
        Console.WriteLine("  --end-date <date>             Filter messages to this date (yyyy-MM-dd)");
        Console.WriteLine("  -c, --contacts <contacts>     Filter by specific contacts (comma-separated)");
        Console.WriteLine("  --continue-on-error           Continue processing on errors");
        Console.WriteLine("  --save-error-report           Save error report to file");
        Console.WriteLine("  --thread-analysis             Enable conversation thread analysis");
        Console.WriteLine("  --response-time               Enable response time analysis");
        Console.WriteLine("  --statistics, --stats         Generate advanced statistics");
        Console.WriteLine("  --interactive                 Force interactive mode");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Export to CSV");
        Console.WriteLine("  SMSXmlToCsv --input backup.xml --output ./exports --formats csv");
        Console.WriteLine();
        Console.WriteLine("  # Export to multiple formats");
        Console.WriteLine("  SMSXmlToCsv --input backup.xml --output ./exports --formats csv,html,parquet");
        Console.WriteLine();
        Console.WriteLine("  # Filter by date range");
        Console.WriteLine("  SMSXmlToCsv --input backup.xml --start-date 2024-01-01 --end-date 2024-12-31");
        Console.WriteLine();
        Console.WriteLine("  # Export specific contacts with analysis");
        Console.WriteLine("  SMSXmlToCsv --input backup.xml --contacts \"John,Jane\" --thread-analysis --stats");
        Console.WriteLine();
        Console.WriteLine("  # Interactive mode (default)");
        Console.WriteLine("  SMSXmlToCsv");
        Console.WriteLine();
    }

    /// <summary>
    /// Display version information
    /// </summary>
    public static void DisplayVersion()
    {
        Console.WriteLine("SMSXmlToCsv Version 2.0.0");
        Console.WriteLine("Copyright (c) 2025");
        Console.WriteLine();
        Console.WriteLine("A comprehensive message import and export tool for SMS, MMS,");
        Console.WriteLine("Facebook Messenger, Instagram, Google Takeout, and more.");
    }

    /// <summary>
    /// Validate options
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (!Interactive)
        {
            if (string.IsNullOrWhiteSpace(InputFile))
            {
                errorMessage = "Input file is required in non-interactive mode. Use --input <file>";
                return false;
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                errorMessage = "Output directory is required in non-interactive mode. Use --output <directory>";
                return false;
            }

            if (ExportFormats.Count == 0)
            {
                errorMessage = "At least one export format is required. Use --formats <formats>";
                return false;
            }

            List<string> validFormats = new List<string> { "csv", "json", "jsonl", "html", "parquet", "sqlite" };
            List<string> invalidFormats = ExportFormats.Where(f => !validFormats.Contains(f)).ToList();

            if (invalidFormats.Count > 0)
            {
                errorMessage = $"Invalid export format(s): {string.Join(", ", invalidFormats)}. " +
                              $"Valid formats: {string.Join(", ", validFormats)}";
                return false;
            }
        }

        return true;
    }
}
