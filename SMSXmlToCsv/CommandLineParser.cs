using SMSXmlToCsv.Models;

namespace SMSXmlToCsv
{
    /// <summary>
    /// Handles command-line argument parsing for the SMS Converter
    /// </summary>
    public class CommandLineParser
    {
        public string? InputFile { get; set; }
        public HashSet<OutputFormat> Formats { get; set; } = new();
        public string? SourceDir { get; set; }
        public string? OutputDir { get; set; }
        public string? ContactsDir { get; set; }
        public string? UserName { get; set; }
        public string? UserPhone { get; set; }
        public bool SaveConfig { get; set; }
        public bool ShowHelpRequested { get; set; }

        // Feature overrides (null = not specified on command line)
        public FeatureMode? MmsExtraction { get; set; }
        public FeatureMode? SplitByContact { get; set; }
        public FeatureMode? EnableFiltering { get; set; }
        public FeatureMode? ExportToSQLite { get; set; }
        public FeatureMode? ExportToHTML { get; set; }

        // NEW v1.5 options
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<string> SelectedContacts { get; set; } = new();
        public HashSet<string> SelectedColumns { get; set; } = new();
        public bool? ContinueOnError { get; set; }
        public bool? LogToConsole { get; set; }
        public bool? ValidateMime { get; set; }

        // NEW v1.7: ML and advanced features
        public bool EnableSentimentAnalysis { get; set; } = false;
        public bool EnableClustering { get; set; } = false;
        public bool GenerateNetworkGraph { get; set; } = false;
        public bool GeneratePdfReport { get; set; } = false;
        public bool InteractiveSearchMode { get; set; } = false;
        public bool UseOllama { get; set; } = true;
        public string OllamaModel { get; set; } = "llama3.2";
        public bool BatchMode { get; set; } = false;
        public string? BatchPath { get; set; }
        public string? TemplateName { get; set; }

        /// <summary>
        /// Parse command-line arguments
        /// </summary>
        public static CommandLineParser Parse(string[] args)
        {
            CommandLineParser parser = new CommandLineParser();

            if (args.Length == 0)
            {
                return parser;
            }

            // Check for help first
            if (args.Any(a => a == "--help" || a == "-h" || a == "/?"))
            {
                parser.ShowHelpRequested = true;
                return parser;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                switch (arg)
                {
                    // Format options
                    case "--formats":
                    case "-f":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ParseFormats(args[i]);
                        }
                        break;

                    // Directory options
                    case "--source-dir":
                    case "--source":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.SourceDir = args[i];
                        }
                        break;

                    case "--output-dir":
                    case "--output":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.OutputDir = args[i];
                        }
                        break;

                    case "--contacts-dir":
                    case "--contacts":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ContactsDir = args[i];
                        }
                        break;

                    // User identification
                    case "--user-name":
                    case "--username":
                    case "--name":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.UserName = args[i];
                        }
                        break;

                    case "--user-phone":
                    case "--userphone":
                    case "--phone":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.UserPhone = args[i];
                        }
                        break;

                    // Individual feature flags
                    case "--mms":
                    case "--mms-extraction":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.MmsExtraction = ParseFeatureMode(args[i]);
                        }
                        break;

                    case "--split":
                    case "--split-contact":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.SplitByContact = ParseFeatureMode(args[i]);
                        }
                        break;

                    case "--filter":
                    case "--filter-contacts":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.EnableFiltering = ParseFeatureMode(args[i]);
                        }
                        break;

                    case "--sqlite":
                    case "--sqlite-export":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ExportToSQLite = ParseFeatureMode(args[i]);
                        }
                        break;

                    case "--html":
                    case "--html-export":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ExportToHTML = ParseFeatureMode(args[i]);
                        }
                        break;

                    // Combined features flag
                    case "--features":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ParseFeatures(args[i]);
                        }
                        break;

                    // NEW: Date range filtering
                    case "--date-from":
                    case "--from-date":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            if (DateTime.TryParse(args[i], out DateTime dateFrom))
                            {
                                parser.DateFrom = dateFrom;
                            }
                        }
                        break;

                    case "--date-to":
                    case "--to-date":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            if (DateTime.TryParse(args[i], out DateTime dateTo))
                            {
                                parser.DateTo = dateTo;
                            }
                        }
                        break;

                    // NEW: Contact selection
                    case "--select-contacts":
                    case "--contacts-list":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ParseContactsList(args[i]);
                        }
                        break;

                    // NEW: Column selection
                    case "--columns":
                    case "--fields":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.ParseColumnsList(args[i]);
                        }
                        break;

                    // NEW: Error handling
                    case "--continue-on-error":
                    case "--ignore-errors":
                        parser.ContinueOnError = true;
                        break;

                    case "--stop-on-error":
                    case "--strict":
                        parser.ContinueOnError = false;
                        break;

                    // NEW: Logging
                    case "--log-console":
                    case "--console-log":
                        parser.LogToConsole = true;
                        break;

                    case "--no-console-log":
                    case "--no-log-console":
                        parser.LogToConsole = false;
                        break;

                    // NEW v1.7: ML and advanced features
                    case "--sentiment":
                    case "--sentiment-analysis":
                        parser.EnableSentimentAnalysis = true;
                        break;

                    case "--clustering":
                    case "--cluster":
                        parser.EnableClustering = true;
                        break;

                    case "--network-graph":
                    case "--graph":
                        parser.GenerateNetworkGraph = true;
                        break;

                    case "--pdf-report":
                    case "--pdf":
                        parser.GeneratePdfReport = true;
                        break;

                    case "--search-mode":
                    case "--interactive-search":
                        parser.InteractiveSearchMode = true;
                        break;

                    case "--no-ollama":
                        parser.UseOllama = false;
                        break;

                    case "--ollama-model":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.OllamaModel = args[i];
                        }
                        break;

                    case "--batch":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.BatchMode = true;
                            parser.BatchPath = args[i];
                        }
                        break;

                    case "--template":
                        if (i + 1 < args.Length)
                        {
                            i++;
                            parser.TemplateName = args[i];
                        }
                        break;

                    // Configuration
                    case "--save-config":
                    case "--save":
                        parser.SaveConfig = true;
                        break;

                    // Input file (no flag)
                    default:
                        if (!arg.StartsWith("-") && File.Exists(args[i]))
                        {
                            parser.InputFile = args[i];
                        }
                        break;
                }
            }

            // Default to Parquet if no formats specified
            if (parser.Formats.Count == 0)
            {
                parser.Formats.Add(OutputFormat.Parquet);
            }

            return parser;
        }

        private void ParseFormats(string formatsString)
        {
            string[] formatNames = formatsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string formatName in formatNames)
            {
                OutputFormat format = formatName.ToLowerInvariant() switch
                {
                    "csv" => OutputFormat.Csv,
                    "json" or "jsonl" => OutputFormat.JsonLines,
                    "parquet" or "pq" => OutputFormat.Parquet,
                    "sqlite" or "db" => OutputFormat.SQLite,
                    "html" => OutputFormat.Html,
                    "postgresql" or "postgres" or "pgsql" => OutputFormat.PostgreSQL,
                    "mysql" => OutputFormat.MySQL,
                    "markdown" or "md" => OutputFormat.Markdown,
                    _ => OutputFormat.None
                };

                if (format != OutputFormat.None)
                {
                    Formats.Add(format);
                }
            }
        }

        private void ParseFeatures(string featuresString)
        {
            string[] features = featuresString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string feature in features)
            {
                string[] parts = feature.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                string featureName = parts[0].ToLowerInvariant();
                FeatureMode? mode = ParseFeatureMode(parts[1]);

                if (mode == null)
                {
                    continue;
                }

                switch (featureName)
                {
                    case "mms":
                    case "mms-extraction":
                        MmsExtraction = mode;
                        break;
                    case "split":
                    case "split-contact":
                        SplitByContact = mode;
                        break;
                    case "filter":
                    case "filter-contacts":
                        EnableFiltering = mode;
                        break;
                    case "sqlite":
                    case "sqlite-export":
                        ExportToSQLite = mode;
                        break;
                    case "html":
                    case "html-export":
                        ExportToHTML = mode;
                        break;
                }
            }
        }

        private void ParseContactsList(string contactsString)
        {
            // Format: "Name1|Phone1,Name2|Phone2" or "Name1|Phone1;Name2|Phone2"
            string[] contacts = contactsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string contact in contacts)
            {
                if (contact.Contains('|'))
                {
                    SelectedContacts.Add(contact.Trim());
                }
            }
        }

        private void ParseColumnsList(string columnsString)
        {
            // Format: "FromName,ToName,MessageText" or "FromName;ToName;MessageText"
            string[] columns = columnsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string column in columns)
            {
                SelectedColumns.Add(column.Trim());
            }
        }

        private static FeatureMode? ParseFeatureMode(string modeString)
        {
            return modeString.ToLowerInvariant() switch
            {
                "enable" or "enabled" or "on" or "true" or "yes" or "y" => FeatureMode.Enable,
                "disable" or "disabled" or "off" or "false" or "no" or "n" => FeatureMode.Disable,
                "ask" or "prompt" => FeatureMode.Ask,
                _ => null
            };
        }

        /// <summary>
        /// Apply command-line overrides to feature configuration
        /// </summary>
        public void ApplyToConfiguration(FeatureConfiguration config)
        {
            if (MmsExtraction.HasValue)
            {
                config.ExtractMMS = MmsExtraction.Value;
            }

            if (SplitByContact.HasValue)
            {
                config.SplitByContact = SplitByContact.Value;
            }

            if (EnableFiltering.HasValue)
            {
                config.EnableFiltering = EnableFiltering.Value;
            }

            if (ExportToSQLite.HasValue)
            {
                config.ExportToSQLite = ExportToSQLite.Value;
            }

            if (ExportToHTML.HasValue)
            {
                config.ExportToHTML = ExportToHTML.Value;
            }

            // NEW: Apply v1.5 options
            if (DateFrom.HasValue)
            {
                config.DateFrom = DateFrom;
            }

            if (DateTo.HasValue)
            {
                config.DateTo = DateTo;
            }

            if (SelectedContacts.Count > 0)
            {
                config.PreConfiguredContacts = SelectedContacts;
            }

            if (SelectedColumns.Count > 0)
            {
                // Ensure required columns are included
                HashSet<string> requiredColumns = FeatureConfiguration.GetRequiredColumns();
                foreach (var required in requiredColumns)
                {
                    SelectedColumns.Add(required);
                }
                config.SelectedColumns = SelectedColumns;
            }

            if (ContinueOnError.HasValue)
            {
                config.ContinueOnError = ContinueOnError.Value;
            }

            // NEW v1.7: Apply ML and advanced features
            if (EnableSentimentAnalysis)
            {
                config.EnableSentimentAnalysis = true;
            }

            if (EnableClustering)
            {
                config.EnableClustering = true;
            }

            if (GenerateNetworkGraph)
            {
                config.GenerateNetworkGraph = true;
            }

            if (GeneratePdfReport)
            {
                config.GeneratePdfReport = true;
            }

            if (!UseOllama)
            {
                config.UseOllama = false;
            }

            if (!string.IsNullOrEmpty(OllamaModel))
            {
                config.OllamaModel = OllamaModel;
            }
        }

        /// <summary>
        /// Apply command-line overrides to folder configuration
        /// </summary>
        public void ApplyToFolderConfiguration(FolderConfiguration config)
        {
            if (!string.IsNullOrEmpty(OutputDir))
            {
                config.OutputBasePath = OutputDir;
            }

            if (!string.IsNullOrEmpty(ContactsDir))
            {
                config.ContactsFolderName = ContactsDir;
            }
        }

        /// <summary>
        /// Check if automation is possible (no "Ask" modes remaining)
        /// </summary>
        public static bool CanAutomateWithConfiguration(FeatureConfiguration config)
        {
            return config.ExtractMMS != FeatureMode.Ask &&
                   config.SplitByContact != FeatureMode.Ask &&
                   config.EnableFiltering != FeatureMode.Ask &&
                   config.ExportToSQLite != FeatureMode.Ask &&
                   config.ExportToHTML != FeatureMode.Ask;
        }

        /// <summary>
        /// Show comprehensive help
        /// </summary>
        public static void ShowHelp()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
          Console.WriteLine("???????????????????????????????????????????????????????????????????????");
    Console.WriteLine($"  SMS BACKUP XML CONVERTER v{AppVersion.Version} - COMMAND LINE HELP");
            Console.WriteLine($"  {AppVersion.Codename} - {AppVersion.DevelopmentInfo}");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????");
        Console.ResetColor();
    Console.WriteLine();

            Console.WriteLine("USAGE:");
            Console.WriteLine("  SMSXmlToCsv [options] <input-file>");
            Console.WriteLine();

            Console.WriteLine("ARGUMENTS:");
            Console.WriteLine("  <input-file>              Path to SMS backup XML file");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("BASIC OPTIONS:");
            Console.ResetColor();
            Console.WriteLine("  --formats, -f <formats>   Output formats (comma-separated)");
            Console.WriteLine("                            Available: csv, json, parquet, sqlite, html");
            Console.WriteLine("  --user-name <name>        Your name");
            Console.WriteLine("  --user-phone <phone>      Your phone number");
            Console.WriteLine("  --output-dir <path>       Output directory");
            Console.WriteLine("  --contacts-dir <path>     Contacts directory");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("FEATURE OPTIONS:");
            Console.ResetColor();
            Console.WriteLine("  --mms <mode>              MMS extraction (enable/disable/ask)");
            Console.WriteLine("  --split <mode>            Split by contact");
            Console.WriteLine("  --filter <mode>           Contact filtering");
            Console.WriteLine("  --sqlite <mode>           SQLite export");
            Console.WriteLine("  --html <mode>             HTML export");
            Console.WriteLine("  --features <list>         Combined: mms=enable,split=enable,...");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("FILTERING OPTIONS (NEW in v1.5):");
            Console.ResetColor();
            Console.WriteLine("  --date-from <date>        Start date (YYYY-MM-DD)");
            Console.WriteLine("  --date-to <date>          End date (YYYY-MM-DD)");
            Console.WriteLine("  --select-contacts <list>  Contact list: \"Name1|Phone1,Name2|Phone2\"");
            Console.WriteLine("  --columns <list>          Columns to export: \"FromName,ToName,MessageText\"");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("ERROR HANDLING OPTIONS (NEW in v1.5):");
            Console.ResetColor();
            Console.WriteLine("  --continue-on-error       Continue processing on errors");
            Console.WriteLine("  --stop-on-error           Stop on first error (default)");
            Console.WriteLine("  --validate-mime           Enable MIME type validation");
            Console.WriteLine("  --no-validate-mime        Skip MIME validation (default)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("LOGGING OPTIONS (NEW in v1.5):");
            Console.ResetColor();
            Console.WriteLine("  --log-console             Log to console (in addition to file)");
            Console.WriteLine("  --no-console-log          Log to file only (default)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("v1.7 ADVANCED OPTIONS:");
            Console.ResetColor();
            Console.WriteLine("  --sentiment               Enable sentiment analysis (ML)");
            Console.WriteLine("  --clustering              Enable clustering (ML)");
            Console.WriteLine("  --network-graph           Generate network graph");
            Console.WriteLine("  --pdf-report              Generate PDF report");
            Console.WriteLine("  --search-mode             Enable interactive search mode");
            Console.WriteLine("  --ollama-model <model>    Set Ollama model (default: llama2)");
            Console.WriteLine("  --batch <path>            Enable batch mode with custom path");
            Console.WriteLine("  --template <name>         Apply template by name");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CONFIGURATION:");
            Console.ResetColor();
            Console.WriteLine("  --save-config, --save     Save settings to appsettings.json");
            Console.WriteLine("  --help, -h, /?           Show this help");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("EXAMPLES:");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  1. Basic with date filter:");
            Console.WriteLine("     SMSXmlToCsv backup.xml --date-from 2023-01-01 --date-to 2023-12-31");
            Console.WriteLine();
            Console.WriteLine("  2. Select specific contacts:");
            Console.WriteLine("     SMSXmlToCsv backup.xml --select-contacts \"John|+15551234,Jane|+15555678\"");
            Console.WriteLine();
            Console.WriteLine("  3. Export specific columns:");
            Console.WriteLine("     SMSXmlToCsv backup.xml --columns \"FromName,ToName,DateTime,MessageText\"");
            Console.WriteLine();
            Console.WriteLine("  4. Continue on errors with validation:");
            Console.WriteLine("     SMSXmlToCsv backup.xml --continue-on-error --validate-mime");
            Console.WriteLine();
            Console.WriteLine("  5. Full automation with all options:");
            Console.WriteLine("     SMSXmlToCsv backup.xml \\");
            Console.WriteLine("       --features mms=enable,split=enable,filter=disable \\");
            Console.WriteLine("       --date-from 2023-01-01 --date-to 2023-12-31 \\");
            Console.WriteLine("       --continue-on-error --log-console");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("NOTES:");
            Console.ResetColor();
            Console.WriteLine("  • Priority: Command line > .env > appsettings.json > Interactive");
            Console.WriteLine("  • PII (phone numbers, names) are masked in log files");
            Console.WriteLine("  • Required columns are always included in exports");
            Console.WriteLine("  • Date format: YYYY-MM-DD (e.g., 2023-12-31)");
            Console.WriteLine();
        }
    }
}
