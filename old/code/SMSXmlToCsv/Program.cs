using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml;

using DotNetEnv;

using Microsoft.Extensions.Configuration;

using SMSXmlToCsv.Analysis;  // Add this for ConsoleHelper
using SMSXmlToCsv.Exporters;
using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Utils;

namespace SMSXmlToCsv
{
    internal class Program
    {
        private static string _userName = "User";
        private static string _userPhone = "+0000000000";
        private static readonly object _consoleLock = new object();
        private static FeatureConfiguration _featureConfig = new FeatureConfiguration();
        private static FolderConfiguration _folderConfig = new FolderConfiguration();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Parse command-line arguments FIRST
            CommandLineParser cmdLine = CommandLineParser.Parse(args);

            // Show help if requested
            if (cmdLine.ShowHelpRequested)
            {
                CommandLineParser.ShowHelp();
                return;
            }

            // Load configuration from files (.env and appsettings.json)
            LoadConfiguration();

            // Initialize logging
            bool logToConsole = cmdLine.LogToConsole ?? false;
            AppLogger.Initialize(logToConsole);
            AppLogger.Information("Application started");

            // Configure error handling
            if (_featureConfig.ContinueOnError)
            {
                SMSXmlToCsv.Utils.ErrorHandler.SetContinueOnAllErrors(true);
                AppLogger.Information("Continue-on-error mode enabled");
            }

            // Apply command-line overrides (highest priority)
            if (!string.IsNullOrEmpty(cmdLine.UserName))
            {
                _userName = cmdLine.UserName;
            }
            if (!string.IsNullOrEmpty(cmdLine.UserPhone))
            {
                _userPhone = cmdLine.UserPhone;
            }

            cmdLine.ApplyToConfiguration(_featureConfig);
            cmdLine.ApplyToFolderConfiguration(_folderConfig);

            AppLogger.LogConfiguration("UserName", _userName);
            AppLogger.LogConfiguration("UserPhone", _userPhone);

            WriteColorLine("=== SMS Backup XML Converter ===\n", ConsoleColor.Cyan);
            WriteColorLine($"Version: {AppVersion.Version} - {AppVersion.Codename}", ConsoleColor.DarkCyan);
            WriteColorLine($"{AppVersion.DevelopmentInfo}\n", ConsoleColor.DarkGray);
            WriteColorLine($"Configured User: {_userName} ({_userPhone})\n", ConsoleColor.Gray);

            // Check if running in interactive mode (no command-line file specified)
            bool interactiveMode = string.IsNullOrEmpty(cmdLine.InputFile);

            if (interactiveMode)
            {
                // Interactive mode: Show configuration menu first
                bool anyAskMode = _featureConfig.ExtractMMS == FeatureMode.Ask ||
                                 _featureConfig.SplitByContact == FeatureMode.Ask ||
                                 _featureConfig.EnableFiltering == FeatureMode.Ask ||
                                 _featureConfig.ExportToSQLite == FeatureMode.Ask ||
                                 _featureConfig.ExportToHTML == FeatureMode.Ask;

                // Apply initial boolean values for the menu to display
                FeatureMenu.ApplyConfigurationModes(_featureConfig);

                if (anyAskMode)
                {
                    // Show interactive menu
                    bool savedFromMenu = await FeatureMenu.ShowConfigurationMenuAsync(_featureConfig);
                    cmdLine.SaveConfig = cmdLine.SaveConfig || savedFromMenu;
                }
            }
            else
            {
                // Command-line mode: Apply configuration without menu
                FeatureMenu.ApplyConfigurationModes(_featureConfig);

                // Show summary of enabled features (automated mode)
                Console.WriteLine();
                WriteColorLine("📋 Configuration (automated mode):", ConsoleColor.Cyan);
                WriteColorLine($"  MMS Extraction........... {(_featureConfig.ShouldExtractMMS ? "ON" : "OFF")}",
                    _featureConfig.ShouldExtractMMS ? ConsoleColor.Green : ConsoleColor.Red);
                WriteColorLine($"  Split by Contact......... {(_featureConfig.ShouldSplitByContact ? "ON" : "OFF")}",
                    _featureConfig.ShouldSplitByContact ? ConsoleColor.Green : ConsoleColor.Red);
                if (_featureConfig.ShouldSplitByContact)
                {
                    WriteColorLine($"  Contact Filtering........ {(_featureConfig.ShouldFilterContacts ? "ON" : "OFF")}",
                        _featureConfig.ShouldFilterContacts ? ConsoleColor.Green : ConsoleColor.Red);
                    WriteColorLine($"  SQLite Export............ {(_featureConfig.ShouldExportToSQLite ? "ON" : "OFF")}",
                        _featureConfig.ShouldExportToSQLite ? ConsoleColor.Green : ConsoleColor.Red);
                    WriteColorLine($"  HTML Export.............. {(_featureConfig.ShouldExportToHTML ? "ON" : "OFF")}",
                        _featureConfig.ShouldExportToHTML ? ConsoleColor.Green : ConsoleColor.Red);
                }
                Console.WriteLine();
                System.Threading.Thread.Sleep(1500);
            }

            // Save configuration if requested
            if (cmdLine.SaveConfig)
            {
                ConfigurationManager configManager = new ConfigurationManager();
                ConfigurationManager.ConvertDecisionsToModes(_featureConfig);
                await configManager.SaveFeatureConfigurationAsync(_featureConfig);
                await configManager.SaveFolderConfigurationAsync(_folderConfig);
                AppLogger.Information("Configuration saved to file");
            }

            // Get input file
            string? inputFile = cmdLine.InputFile;
            if (string.IsNullOrEmpty(inputFile))
            {
                inputFile = await SelectInputFileAsync();
                if (string.IsNullOrEmpty(inputFile))
                {
                    WriteColorLine("No file selected. Exiting.", ConsoleColor.Red);
                    AppLogger.Warning("No input file selected, exiting");
                    AppLogger.Close();
                    return;
                }
            }
            else
            {
                WriteColorLine($"Processing file: {Path.GetFileName(inputFile)}", ConsoleColor.Green);
            }

            AppLogger.Information($"Input file selected: {Path.GetFileName(inputFile)}");

            // Get output formats - use consistent menu-style selection
            HashSet<OutputFormat> formats = cmdLine.Formats;
            if (formats.Count == 0)
            {
                if (interactiveMode)
                {
                    formats = SelectOutputFormatsInteractive();
                }
                else
                {
                    // Default to Parquet in command-line mode
                    formats = new HashSet<OutputFormat> { OutputFormat.Parquet };
                }

                if (formats.Count == 0)
                {
                    WriteColorLine("No output formats selected. Exiting.", ConsoleColor.Red);
                    AppLogger.Warning("No output formats selected, exiting");
                    AppLogger.Close();
                    return;
                }
            }
            else
            {
                WriteColorLine($"Output formats: {string.Join(", ", formats)}", ConsoleColor.Green);
            }

            AppLogger.Information($"Output formats: {string.Join(", ", formats)}");

            // Process the file
            await ProcessSmsFileAsync(inputFile, formats);

            AppLogger.Information("Processing complete");
            AppLogger.Close();

            WriteColorLine("\n\nPress any key to exit...", ConsoleColor.Cyan);
            Console.ReadKey();
        }

        static void LoadConfiguration()
        {
            // Load .env file if it exists
            string solutionDir = FindProjectOrSolutionDirectory();
            string envPath = Path.Combine(solutionDir, ".env");

            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }

            // Build configuration from multiple sources (priority: env vars > appsettings.json > defaults)
            string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            ConfigurationBuilder builder = new ConfigurationBuilder();

            if (File.Exists(appSettingsPath))
            {
                builder.AddJsonFile(appSettingsPath, optional: true);
            }

            builder.AddEnvironmentVariables();
            IConfiguration config = builder.Build();

            // Try to get from environment variables first, then config file
            _userName = Environment.GetEnvironmentVariable("SMS_USER_NAME")
                       ?? config["SmsConverter:UserName"]
                       ?? _userName;

            _userPhone = Environment.GetEnvironmentVariable("SMS_USER_PHONE")
                        ?? config["SmsConverter:UserPhone"]
                        ?? _userPhone;

            // Load feature configuration
            _featureConfig.ExtractMMS = FeatureConfiguration.ParseMode(config["Features:ExtractMMS"]);
            _featureConfig.SplitByContact = FeatureConfiguration.ParseMode(config["Features:SplitByContact"]);
            _featureConfig.EnableFiltering = FeatureConfiguration.ParseMode(config["Features:EnableFiltering"]);
            _featureConfig.ExportToSQLite = FeatureConfiguration.ParseMode(config["Features:ExportToSQLite"]);
            _featureConfig.ExportToHTML = FeatureConfiguration.ParseMode(config["Features:ExportToHTML"]);

            // Load date filtering
            string? dateFromStr = config["Filtering:DateFrom"];
            string? dateToStr = config["Filtering:DateTo"];
            if (DateTime.TryParse(dateFromStr, out DateTime dateFrom))
            {
                _featureConfig.DateFrom = dateFrom;
            }
            if (DateTime.TryParse(dateToStr, out DateTime dateTo))
            {
                _featureConfig.DateTo = dateTo;
            }

            // Load pre-configured contacts (manual parsing)
            IConfigurationSection contactsSection = config.GetSection("Filtering:PreConfiguredContacts");
            if (contactsSection.Exists())
            {
                foreach (IConfigurationSection child in contactsSection.GetChildren())
                {
                    string? value = child.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        _featureConfig.PreConfiguredContacts.Add(value);
                    }
                }
            }

            // Load column selection (manual parsing)
            IConfigurationSection columnsSection = config.GetSection("Columns:Selected");
            if (columnsSection.Exists())
            {
                foreach (IConfigurationSection child in columnsSection.GetChildren())
                {
                    string? value = child.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        _featureConfig.SelectedColumns.Add(value);
                    }
                }
            }

            if (_featureConfig.SelectedColumns.Count == 0)
            {
                _featureConfig.SelectedColumns = FeatureConfiguration.GetDefaultColumns();
            }

            // Load error handling
            _featureConfig.ContinueOnError = bool.TryParse(config["ErrorHandling:ContinueOnError"], out bool continueOnError) && continueOnError;

            // Load folder configuration
            _folderConfig.OutputBasePath = config["Folders:OutputBasePath"] ?? string.Empty;
            _folderConfig.MMSFolderName = config["Folders:MMSFolderName"] ?? "MMS";
            _folderConfig.ContactsFolderName = config["Folders:ContactsFolderName"] ?? "Contacts";
        }

        static void WriteColor(string text, ConsoleColor color)
        {
            // Updated to use Spectre.Console for proper UTF-8/emoji support
            ConsoleHelper.Write(text, color);
        }

        static void WriteColorLine(string text, ConsoleColor color)
        {
            // Updated to use Spectre.Console for proper UTF-8/emoji support
            ConsoleHelper.WriteLine(text, color);
        }

        static async Task<string> SelectInputFileAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Input File Selection ===\n");
            Console.ResetColor();

            string searchDir = FindProjectOrSolutionDirectory();

            WriteColorLine($"Searching for XML files in: {searchDir}\n", ConsoleColor.Gray);

            string[] xmlFiles = Directory.GetFiles(searchDir, "*.xml", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToArray();

            if (xmlFiles.Length == 0)
            {
                WriteColorLine("No XML files found in the project/solution directory.", ConsoleColor.Yellow);
                Console.Write("Enter the full path to the SMS backup XML file: ");
                string path = Console.ReadLine()?.Trim() ?? string.Empty;
                return File.Exists(path) ? path : string.Empty;
            }

            WriteColorLine("Available XML files (sorted by newest):", ConsoleColor.White);
            for (int i = 0; i < xmlFiles.Length; i++)
            {
                FileInfo fi = new FileInfo(xmlFiles[i]);
                string relativePath = Path.GetRelativePath(searchDir, xmlFiles[i]);
                Console.WriteLine($"  {i + 1}. {relativePath} ({fi.Length / (1024.0 * 1024.0):F2} MB) - {fi.LastWriteTime:yyyy-MM-dd}");
            }

            Console.WriteLine();
            Console.Write($"Select file (1-{xmlFiles.Length}) or [Enter] for default [1]: ");
            string input = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                return xmlFiles[0];
            }

            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= xmlFiles.Length)
            {
                return xmlFiles[selection - 1];
            }

            WriteColorLine("Invalid selection. Using default (newest file).", ConsoleColor.Yellow);
            return xmlFiles[0];
        }

        static string FindProjectOrSolutionDirectory()
        {
            string currentDir = Directory.GetCurrentDirectory();
            DirectoryInfo currentDirInfo = new DirectoryInfo(currentDir);

            if (currentDir.Contains("\\bin\\") || currentDir.Contains("/bin/"))
            {
                DirectoryInfo? dir = currentDirInfo;

                while (dir != null)
                {
                    if (dir.GetFiles("*.csproj").Length > 0)
                    {
                        return dir.Parent != null && (dir.Parent.GetFiles("*.sln").Length > 0 || dir.Parent.GetFiles("*.slnx").Length > 0)
                            ? dir.Parent.FullName
                            : dir.FullName;
                    }

                    if (dir.GetFiles("*.sln").Length > 0 || dir.GetFiles("*.slnx").Length > 0)
                    {
                        return dir.FullName;
                    }

                    dir = dir.Parent;
                }

                return currentDir;
            }

            if (currentDirInfo.GetFiles("*.csproj").Length > 0)
            {
                return currentDirInfo.Parent != null &&
                    (currentDirInfo.Parent.GetFiles("*.sln").Length > 0 || currentDirInfo.Parent.GetFiles("*.slnx").Length > 0)
                    ? currentDirInfo.Parent.FullName
                    : currentDir;
            }

            if (currentDirInfo.GetFiles("*.sln").Length > 0 || currentDirInfo.GetFiles("*.slnx").Length > 0)
            {
                return currentDir;
            }

            DirectoryInfo[] subDirs = currentDirInfo.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs)
            {
                if (subDir.Name == "bin" || subDir.Name == "obj")
                {
                    continue;
                }

                if (subDir.GetFiles("*.csproj").Length > 0)
                {
                    return currentDir;
                }
            }

            return currentDir;
        }

        static HashSet<OutputFormat> SelectOutputFormatsInteractive()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Output Format Selection ===\n");
            Console.ResetColor();

            Console.WriteLine("Available formats:\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  1. 📊 CSV (Excel-friendly, ~40-50% size reduction)");
            Console.WriteLine("  2. 📄 JSON Lines (AI-friendly, ~60-70% size reduction)");
            Console.WriteLine("  3. 📦 Parquet (Best compression, ~90-94% size reduction, AI-optimized)");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Enter your choices separated by commas (e.g., '1,3' or '2' or '1,2,3') [default: 3]: ");

            string input = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                input = "3";
            }

            HashSet<OutputFormat> formats = new HashSet<OutputFormat>();
            string[] choices = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string choice in choices)
            {
                if (int.TryParse(choice, out int num))
                {
                    OutputFormat format = num switch
                    {
                        1 => OutputFormat.Csv,
                        2 => OutputFormat.JsonLines,
                        3 => OutputFormat.Parquet,
                        _ => OutputFormat.None
                    };

                    if (format != OutputFormat.None)
                    {
                        formats.Add(format);
                    }
                }
            }

            if (formats.Count > 0)
            {
                Console.WriteLine();
                WriteColorLine($"✓ Selected formats: {string.Join(", ", formats)}", ConsoleColor.Green);
                System.Threading.Thread.Sleep(1000);
            }

            return formats;
        }

        static async Task ProcessSmsFileAsync(string inputFile, HashSet<OutputFormat> formats)
        {
            WriteColorLine($"\nProcessing: {Path.GetFileName(inputFile)}", ConsoleColor.White);
            AppLogger.Information($"Starting processing of {Path.GetFileName(inputFile)}");

            Stopwatch sw = Stopwatch.StartNew();

            string outputBase = string.IsNullOrEmpty(_folderConfig.OutputBasePath)
                ? Path.GetDirectoryName(inputFile) ?? "."
                : _folderConfig.OutputBasePath;

            string basePath = Path.Combine(outputBase, Path.GetFileNameWithoutExtension(inputFile));

            // NEW: Get organized export paths
            string sourceFileName = Path.GetFileNameWithoutExtension(inputFile);
            ExportPaths exportPaths = SMSXmlToCsv.Utils.ExportPathHelper.GetExportPaths(outputBase, sourceFileName);

            WriteColorLine("\n📱 Reading and parsing XML...\n", ConsoleColor.Gray);
            List<SmsMessage> messages = await ReadSmsMessagesAsync(inputFile);

            messages.Sort((a, b) => a.UnixTimestamp.CompareTo(b.UnixTimestamp));

            WriteColorLine($"✓ Parsed {messages.Count:N0} messages in {sw.Elapsed.TotalSeconds:F2} seconds", ConsoleColor.Green);
            AppLogger.Information($"Parsed {messages.Count} messages");

            // ISSUE #1 FIX: Contact Merge Integration - Offer to merge duplicate contacts
            // This runs in interactive mode only, after messages are loaded but before filtering
            bool interactiveMode = string.IsNullOrEmpty(_folderConfig.OutputBasePath);
            if (interactiveMode)
            {
                WriteColorLine("\n🔗 Checking for duplicate contacts...", ConsoleColor.Cyan);

                // Load any saved merge decisions
                ConfigurationManager configManager = new ConfigurationManager();
                List<MergeDecision>? savedMerges = await configManager.LoadContactMergesAsync();

                // Show merge interface and get results
                (List<SmsMessage>? mergedMessages, List<MergeDecision>? newMergeDecisions) = await FeatureMenu.ShowContactMergeInterface(messages, _userPhone, savedMerges);
                messages = mergedMessages;

                // No need to save here as ShowContactMergeInterface handles saving
            }

            // Apply unknown contact filtering if enabled (v1.7.1)
            if (_featureConfig.FilterUnknownContacts)
            {
                int originalCount = messages.Count;
                messages = messages.Where(m =>
                {
                    string contactName = m.Direction == "Sent" ? m.ToName : m.FromName;
                    string contactPhone = m.Direction == "Sent" ? m.ToPhone : m.FromPhone;
                    return !FeatureMenu.IsUnknownContact(contactName, contactPhone);
                }).ToList();

                int filtered = originalCount - messages.Count;
                if (filtered > 0)
                {
                    int uniqueFiltered = messages
                        .Select(m => m.Direction == "Sent" ? m.ToPhone : m.FromPhone)
                        .Distinct()
                        .Count();

                    WriteColorLine($"  Filtered {filtered:N0} messages from unknown contacts", ConsoleColor.Yellow);
                    AppLogger.Information($"Filtered {filtered} messages from unknown contacts, {messages.Count} messages remaining");
                }
            }

            // Apply date range filter if configured
            if (_featureConfig.DateFrom.HasValue || _featureConfig.DateTo.HasValue)
            {
                int originalCount = messages.Count;
                messages = FilterMessagesByDateRange(messages, _featureConfig.DateFrom, _featureConfig.DateTo);
                WriteColorLine($"  Date filter: {messages.Count:N0} messages (filtered out {originalCount - messages.Count:N0})", ConsoleColor.Cyan);
                AppLogger.Information($"Date filtering: {messages.Count} messages remaining after filter");
            }

            if (_featureConfig.ShouldFilterContacts)
            {
                _featureConfig.SelectedContacts = FeatureMenu.SelectContacts(messages, _userPhone);

                if (_featureConfig.SelectedContacts.Count > 0)
                {
                    messages = FilterMessagesByContacts(messages, _featureConfig.SelectedContacts);
                    WriteColorLine($"  Filtered to {messages.Count:N0} messages from {_featureConfig.SelectedContacts.Count} contacts", ConsoleColor.Green);
                }
            }

            Dictionary<long, List<MmsAttachment>>? mmsAttachments = null;
            if (_featureConfig.ShouldExtractMMS)
            {
                AppLogger.Information("Starting MMS extraction");
                MmsExtractor extractor = new MmsExtractor(outputBase, _folderConfig.ContactsFolderName, _userName);
                mmsAttachments = await extractor.ExtractMmsAttachmentsAsync(inputFile);
                AppLogger.Information($"MMS extraction complete: {mmsAttachments?.Count ?? 0} messages with attachments");
            }

            if (_featureConfig.ShouldSplitByContact)
            {
                AppLogger.Information("Starting contact splitting");
                ContactSplitter splitter = new ContactSplitter(outputBase, _folderConfig.ContactsFolderName, _userPhone, _featureConfig.SelectedColumns);

                // Pass ollama instance AND sentiment flag
                SMSXmlToCsv.ML.OllamaIntegration? ollama = null;
                if (_featureConfig.GeneratePdfReport || _featureConfig.EnableSentimentAnalysis)
                {
                    ollama = new SMSXmlToCsv.ML.OllamaIntegration();
                    if (!await ollama.IsAvailableAsync())
                    {
                        WriteColorLine("  ⚠️  Ollama not available for AI analysis", ConsoleColor.Yellow);
                        ollama = null;
                    }
                    else
                    {
                        WriteColorLine("  ✓ Ollama available for AI analysis", ConsoleColor.Green);
                    }
                }

                await splitter.SplitMessagesByContactAsync(
                     messages,
                       formats,
                    mmsAttachments,
                    _featureConfig.SelectedContacts,
                      _featureConfig.GeneratePdfReport,
                     _userPhone,
               ollama,
                    _featureConfig.EnableSentimentAnalysis);
            }
            else
            {
                foreach (OutputFormat format in formats)
                {
                    if (format == OutputFormat.SQLite || format == OutputFormat.Html)
                    {
                        WriteColorLine($"\n⚠️  {format} export requires contact splitting to be enabled. Skipping...", ConsoleColor.Yellow);
                        continue;
                    }
                    await WriteOutputAsync(messages, basePath, format, mmsAttachments);
                }
            }

            if (_featureConfig.ShouldExportToSQLite && _featureConfig.ShouldSplitByContact)
            {
                WriteColorLine("\n🗄️  Exporting to SQLite database...", ConsoleColor.Cyan);
                AppLogger.Information("Starting SQLite export");
                SQLiteExporter sqliteExporter = new SQLiteExporter();
                await sqliteExporter.ExportAsync(messages, exportPaths.SqliteDb.Replace(".db", ""), mmsAttachments);
                WriteColorLine($"✓ SQLite database created: {exportPaths.SqliteDb}", ConsoleColor.Green);
            }

            if (_featureConfig.ShouldExportToHTML && _featureConfig.ShouldSplitByContact)
            {
                WriteColorLine("\n🌐 Creating HTML chat pages...", ConsoleColor.Cyan);
                AppLogger.Information("Starting HTML export");
                string htmlPath = Path.Combine(outputBase, sourceFileName);
                HtmlExporter htmlExporter = new HtmlExporter(_userName, _userPhone);
                await htmlExporter.ExportAsync(messages, htmlPath, mmsAttachments);
                WriteColorLine($"✓ HTML chat pages created in: {exportPaths.ContactsFolder}", ConsoleColor.Green);
            }

            // NEW v1.6: Run analysis features if enabled - USE ORGANIZED PATHS
            await RunAnalysisFeaturesAsync(messages, outputBase, sourceFileName);

            // NEW v1.7: Run ML and advanced features if enabled - USE ORGANIZED PATHS
            await RunAdvancedFeaturesAsync(messages, outputBase, sourceFileName, mmsAttachments);

            sw.Stop();

            ShowContactStatistics(messages);

            WriteColorLine($"\n⏱️  Total processing time: {sw.Elapsed.TotalSeconds:F2} seconds", ConsoleColor.Cyan);
            AppLogger.Information($"Total processing time: {sw.Elapsed.TotalSeconds:F2} seconds");
        }

        static List<SmsMessage> FilterMessagesByDateRange(List<SmsMessage> messages, DateTime? dateFrom, DateTime? dateTo)
        {
            return messages.Where(m =>
            {
                return dateFrom.HasValue && m.DateTime < dateFrom.Value
                    ? false
                    : !dateTo.HasValue || m.DateTime <= dateTo.Value.AddDays(1).AddSeconds(-1);
            }).ToList();
        }

        static List<SmsMessage> FilterMessagesByContacts(List<SmsMessage> allMessages, HashSet<string> selectedContacts)
        {
            List<SmsMessage> filtered = new List<SmsMessage>();

            foreach (SmsMessage msg in allMessages)
            {
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;
                string key = $"{contactName}|{contactPhone}";

                if (selectedContacts.Contains(key))
                {
                    filtered.Add(msg);
                }
            }

            return filtered;
        }

        static async Task<List<SmsMessage>> ReadSmsMessagesAsync(string inputFile)
        {
            List<SmsMessage> messages = new List<SmsMessage>();
            int currentLine = 0;
            Stopwatch sw = Stopwatch.StartNew();
            int lastCursorTop = Console.CursorTop;

            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            using (XmlReader reader = XmlReader.Create(sr, new XmlReaderSettings
            {
                Async = true,
                IgnoreWhitespace = true,
                IgnoreComments = true
            }))
            {
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "sms")
                    {
                        SmsMessage? message = ParseSmsElement(reader);
                        if (message != null)
                        {
                            messages.Add(message);
                            currentLine++;

                            if (currentLine % 500 == 0)
                            {
                                double messagesPerSecond = currentLine / sw.Elapsed.TotalSeconds;
                                double estimatedTotal = 30000;
                                double estimatedTimeRemaining = (estimatedTotal - currentLine) / messagesPerSecond;

                                lock (_consoleLock)
                                {
                                    // Clear the line
                                    int currentTop = Console.CursorTop;
                                    Console.SetCursorPosition(0, lastCursorTop);
                                    Console.Write(new string(' ', Console.WindowWidth - 1));
                                    Console.SetCursorPosition(0, lastCursorTop);

                                    // Write with colors
                                    Console.Write("  📱 Parsed ");
                                    WriteColor($"{currentLine:N0}", ConsoleColor.White);
                                    Console.Write(" messages | ");

                                    ConsoleColor dirColor = message.Direction == "Sent" ? ConsoleColor.Blue : ConsoleColor.Red;
                                    WriteColor(message.Direction, dirColor);

                                    Console.Write(" | ");
                                    Console.Write($"{message.DateTime:yyyy-MM-dd HH:mm} | {messagesPerSecond:F0} msg/s | ETA: {estimatedTimeRemaining:F0}s");

                                    lastCursorTop = Console.CursorTop;
                                }

                                AppLogger.LogProgress("XML Parsing", currentLine, (int)estimatedTotal);
                            }
                        }
                    }
                }
            }

            Console.WriteLine();
            return messages;
        }

        static SmsMessage? ParseSmsElement(XmlReader reader)
        {
            string? address = reader.GetAttribute("address");
            string? contactName = reader.GetAttribute("contact_name");
            string? type = reader.GetAttribute("type");
            string? date = reader.GetAttribute("date");
            string? body = reader.GetAttribute("body");

            if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(date))
            {
                return null;
            }

            // FIX: Correct Android SMS backup type values
            // type="1" = RECEIVED, type="2" = SENT
            bool isSent = type == "2";  // FIXED: was incorrectly "1"
            long unixTimestamp = long.Parse(date);

            // IMPORTANT: Most Android backup apps (SMS Backup & Restore, etc.) store timestamps 
            // in LOCAL device time, not UTC. Using .DateTime (not .LocalDateTime) preserves the
            // original time from the backup without double-conversion.
            // If your backup uses UTC timestamps, change this to .LocalDateTime
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).DateTime;

            return new SmsMessage
            {
                FromName = isSent ? _userName : (contactName ?? "(Unknown)"),
                FromPhone = isSent ? _userPhone : address,
                ToName = isSent ? (contactName ?? "(Unknown)") : _userName,
                ToPhone = isSent ? address : _userPhone,
                Direction = isSent ? "Sent" : "Received",
                DateTime = dateTime,
                UnixTimestamp = unixTimestamp,
                MessageText = body ?? string.Empty
            };
        }

        static void ShowContactStatistics(List<SmsMessage> messages)
        {
            WriteColorLine("\n\n=== Contact Statistics (Top 10) ===\n", ConsoleColor.Cyan);

            Dictionary<string, ContactStats> contactStats = new Dictionary<string, ContactStats>();

            foreach (SmsMessage msg in messages)
            {
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;

                // Skip self-messages
                if (contactPhone == _userPhone)
                {
                    continue;
                }

                string key = $"{contactName}|{contactPhone}";

                if (!contactStats.ContainsKey(key))
                {
                    contactStats[key] = new ContactStats
                    {
                        Name = contactName,
                        Phone = contactPhone,
                        FirstMessage = msg.DateTime,
                        LastMessage = msg.DateTime
                    };
                }

                ContactStats stats = contactStats[key];
                stats.TotalMessages++;

                if (msg.Direction == "Sent")
                {
                    stats.SentCount++;
                }
                else
                {
                    stats.ReceivedCount++;
                }

                if (msg.DateTime < stats.FirstMessage)
                {
                    stats.FirstMessage = msg.DateTime;
                }

                if (msg.DateTime > stats.LastMessage)
                {
                    stats.LastMessage = msg.DateTime;
                }
            }

            List<ContactStats> topContacts = contactStats.Values
                .OrderByDescending(c => c.TotalMessages)
                .Take(10)
                .ToList();

            int rank = 1;
            foreach (ContactStats contact in topContacts)
            {
                TimeSpan duration = contact.LastMessage - contact.FirstMessage;
                int durationDays = Math.Max(duration.Days, 1);
                double avgMessagesPerDay = (double)contact.TotalMessages / durationDays;

                WriteColor($"{rank,2}. ", ConsoleColor.White);
                WriteColor($"{contact.Name,-30}", ConsoleColor.Yellow);
                WriteColor($" {contact.Phone,-16}", ConsoleColor.Gray);
                Console.WriteLine();

                Console.Write("    Total: ");
                WriteColor($"{contact.TotalMessages,5:N0}", ConsoleColor.White);
                Console.Write(" messages | ");

                WriteColor($"Sent: {contact.SentCount,5:N0}", ConsoleColor.Blue);
                Console.Write(" | ");
                WriteColor($"Received: {contact.ReceivedCount,5:N0}", ConsoleColor.Red);
                Console.Write(" | ");
                WriteColor($"Avg: {avgMessagesPerDay:F1} msg/day", ConsoleColor.Cyan);
                Console.WriteLine();

                Console.Write("    Period: ");
                WriteColor($"{contact.FirstMessage:yyyy-MM-dd}", ConsoleColor.Cyan);
                Console.Write(" to ");
                WriteColor($"{contact.LastMessage:yyyy-MM-dd}", ConsoleColor.Cyan);
                Console.WriteLine($" ({duration.Days} days)");
                Console.WriteLine();

                rank++;
            }
        }

        static async Task WriteOutputAsync(List<SmsMessage> messages, string basePath, OutputFormat format, Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            WriteColorLine($"\nWriting {format} output...", ConsoleColor.Cyan);
            AppLogger.Information($"Exporting to {format} format");
            Stopwatch sw = Stopwatch.StartNew();

            string outputFile = format switch
            {
                OutputFormat.Csv => $"{basePath}.csv",
                OutputFormat.JsonLines => $"{basePath}.jsonl",
                OutputFormat.Parquet => $"{basePath}.parquet",
                OutputFormat.PostgreSQL => $"{basePath}.pgsql",
                OutputFormat.MySQL => $"{basePath}.mysql",
                OutputFormat.Markdown => $"{basePath}.md",
                _ => throw new ArgumentException("Invalid format")
            };

            IMessageExporter? exporter = format switch
            {
                OutputFormat.Csv => new CsvExporter(_featureConfig.SelectedColumns),
                OutputFormat.JsonLines => new JsonLinesExporter(_featureConfig.SelectedColumns),
                OutputFormat.Parquet => new ParquetExporter(_featureConfig.SelectedColumns),
                OutputFormat.PostgreSQL => new PostgreSQLExporter(),
                OutputFormat.MySQL => new MySQLExporter(),
                OutputFormat.Markdown => new MarkdownExporter(_userPhone, _featureConfig.SelectedColumns),
                _ => null
            };

            if (exporter != null)
            {
                await exporter.ExportAsync(messages, outputFile, mmsAttachments);
            }

            sw.Stop();
            FileInfo fi = new FileInfo(outputFile);
            double sizeMB = fi.Length / (1024.0 * 1024.0);

            WriteColor("✓ ", ConsoleColor.Green);
            WriteColor($"{format}", ConsoleColor.White);
            Console.WriteLine($" written: {outputFile}");
            Console.Write("  Size: ");
            WriteColor($"{sizeMB:F2} MB", ConsoleColor.Yellow);
            Console.WriteLine($" | Time: {sw.Elapsed.TotalSeconds:F2}s | Speed: {messages.Count / sw.Elapsed.TotalSeconds:F0} msg/s");

            AppLogger.Information($"{format} export complete: {sizeMB:F2} MB in {sw.Elapsed.TotalSeconds:F2}s");
        }

        /// <summary>
        /// Run v1.6 analysis features if enabled
        /// </summary>
        static async Task RunAnalysisFeaturesAsync(List<SmsMessage> messages, string outputBase, string sourceFileName)
        {
            if (!_featureConfig.EnableThreadAnalysis && !_featureConfig.EnableResponseTimeAnalysis &&
                !_featureConfig.EnableAdvancedStatistics)
            {
                return; // No analysis features enabled
            }

            WriteColorLine("\n📊 Running analysis features...", ConsoleColor.Cyan);

            // Get organized export paths
            ExportPaths paths = SMSXmlToCsv.Utils.ExportPathHelper.GetExportPaths(outputBase, sourceFileName);

            // Thread Analysis
            if (_featureConfig.EnableThreadAnalysis)
            {
                WriteColorLine("  Analyzing conversation threads...", ConsoleColor.Gray);
                SMSXmlToCsv.Analysis.ConversationThreadAnalyzer threadAnalyzer =
                    new SMSXmlToCsv.Analysis.ConversationThreadAnalyzer(
                        _featureConfig.ThreadTimeoutMinutes,
                        _featureConfig.MinimumThreadLength);

                List<SMSXmlToCsv.Analysis.ConversationThread> threads = threadAnalyzer.DetectThreads(messages);
                await threadAnalyzer.ExportThreadsAsync(threads, paths.ThreadsJson);

                WriteColor("  ✓ ", ConsoleColor.Green);
                Console.WriteLine($"Thread analysis complete: {threads.Count} threads detected");
            }

            // Response Time Analysis
            if (_featureConfig.EnableResponseTimeAnalysis)
            {
                WriteColorLine("  Analyzing response times...", ConsoleColor.Gray);
                SMSXmlToCsv.Analysis.ResponseTimeAnalyzer responseAnalyzer =
                    new SMSXmlToCsv.Analysis.ResponseTimeAnalyzer();

                Dictionary<string, SMSXmlToCsv.Analysis.ResponseTimeStats> responseStats =
                    responseAnalyzer.AnalyzeResponseTimes(messages, _userPhone);
                await responseAnalyzer.ExportAnalysisAsync(responseStats, paths.ResponseTimesJson);

                WriteColor("  ✓ ", ConsoleColor.Green);
                Console.WriteLine($"Response time analysis complete: {responseStats.Count} contacts analyzed");
            }

            // Advanced Statistics
            if (_featureConfig.EnableAdvancedStatistics)
            {
                WriteColorLine("  Generating comprehensive statistics...", ConsoleColor.Gray);
                SMSXmlToCsv.Analysis.AdvancedStatisticsExporter statsExporter =
                    new SMSXmlToCsv.Analysis.AdvancedStatisticsExporter();

                SMSXmlToCsv.Analysis.ComprehensiveStats stats =
                    statsExporter.GenerateStatistics(messages, _userPhone);

                if (_featureConfig.ExportStatisticsJson)
                {
                    await statsExporter.ExportJsonAsync(stats, paths.StatsJson);
                }

                if (_featureConfig.ExportStatisticsMarkdown)
                {
                    await statsExporter.ExportMarkdownAsync(stats, paths.StatsMarkdown);
                }

                WriteColor("  ✓ ", ConsoleColor.Green);
                Console.WriteLine("Advanced statistics export complete");
            }

            WriteColorLine($"✓ All analysis features complete → {paths.ExportsFolder}", ConsoleColor.Green);
        }

        /// <summary>
        /// Run v1.7 ML and advanced features if enabled
        /// </summary>
        static async Task RunAdvancedFeaturesAsync(List<SmsMessage> messages, string outputBase, string sourceFileName, Dictionary<long, List<MmsAttachment>>? mmsAttachments)
        {
            if (!_featureConfig.EnableSentimentAnalysis && !_featureConfig.EnableClustering &&
                !_featureConfig.GenerateNetworkGraph && !_featureConfig.GeneratePdfReport)
            {
                return; // No advanced features enabled
            }

            WriteColorLine("\n🤖 Running ML and advanced features...", ConsoleColor.Cyan);

            // Get organized export paths
            ExportPaths paths = SMSXmlToCsv.Utils.ExportPathHelper.GetExportPaths(outputBase, sourceFileName);

            // Initialize Ollama if needed
            SMSXmlToCsv.ML.OllamaIntegration? ollama = null;
            if (_featureConfig.UseOllama || _featureConfig.GenerateNetworkGraph)
            {
                ollama = new SMSXmlToCsv.ML.OllamaIntegration();
                if (await ollama.IsAvailableAsync())
                {
                    WriteColorLine("  ✓ Ollama detected and ready", ConsoleColor.Green);
                }
                else
                {
                    if (_featureConfig.GenerateNetworkGraph)
                    {
                        WriteColorLine("  ⚠️  Ollama not available - Network graph requires Ollama for topic detection", ConsoleColor.Yellow);
                        WriteColorLine("     Install Ollama from https://ollama.ai and run: ollama pull llama3.2", ConsoleColor.Yellow);
                    }
                    else
                    {
                        WriteColorLine("  ⚠️  Ollama not available, using fallback methods", ConsoleColor.Yellow);
                    }
                    ollama = null;
                }
            }

            // Sentiment Analysis
            if (_featureConfig.EnableSentimentAnalysis)
            {
                WriteColorLine("  Analyzing sentiment...", ConsoleColor.Gray);
                SMSXmlToCsv.Analysis.SentimentAnalyzer sentimentAnalyzer =
                    new SMSXmlToCsv.Analysis.SentimentAnalyzer(ollama);

                SMSXmlToCsv.Analysis.SentimentAnalysisResults sentimentResults =
                    await sentimentAnalyzer.AnalyzeMessagesAsync(messages, _featureConfig.SentimentAnalysisMaxMessages);
                await sentimentAnalyzer.ExportResultsAsync(sentimentResults, paths.SentimentJson);

                WriteColor("  ✓ ", ConsoleColor.Green);
                Console.WriteLine($"Sentiment analysis complete: {sentimentResults.TotalAnalyzed} messages analyzed");
            }

            // Conversation Clustering
            if (_featureConfig.EnableClustering)
            {
                WriteColorLine("  Clustering conversations...", ConsoleColor.Gray);
                SMSXmlToCsv.Analysis.ConversationClusterAnalyzer clusterAnalyzer =
                    new SMSXmlToCsv.Analysis.ConversationClusterAnalyzer(ollama);

                List<SMSXmlToCsv.Analysis.MessageCluster> clusters =
                    await clusterAnalyzer.ClusterConversationsAsync(messages, _featureConfig.ClusterCount);
                await clusterAnalyzer.ExportClustersAsync(clusters, paths.ClustersJson);

                WriteColor("  ✓ ", ConsoleColor.Green);
                Console.WriteLine($"Clustering complete: {clusters.Count} clusters identified");
            }

            // Network Graph
            if (_featureConfig.GenerateNetworkGraph)
            {
                WriteColorLine("  Generating network graph with AI topic detection...", ConsoleColor.Gray);
                SMSXmlToCsv.Visualization.NetworkGraphGenerator graphGen =
                    new SMSXmlToCsv.Visualization.NetworkGraphGenerator(_featureConfig.ShouldSplitByContact, ollama);

                await graphGen.GenerateGraphAsync(messages, paths.NetworkGraphJson, _userPhone);

                WriteColor("  ✓ ", ConsoleColor.Green);
                Console.WriteLine("Network graph generated (JSON + HTML viewer)");
            }

            // PDF Report
            if (_featureConfig.GeneratePdfReport)
            {
                WriteColorLine("  Generating PDF report...", ConsoleColor.Gray);

                // Generate stats first if not already done
                SMSXmlToCsv.Analysis.AdvancedStatisticsExporter statsExporter =
                    new SMSXmlToCsv.Analysis.AdvancedStatisticsExporter();
                SMSXmlToCsv.Analysis.ComprehensiveStats stats =
                    statsExporter.GenerateStatistics(messages, _userPhone);

                // Collect analysis results if available
                SMSXmlToCsv.Analysis.SentimentAnalysisResults? sentimentResults = null;
                Dictionary<string, SMSXmlToCsv.Analysis.ResponseTimeStats>? responseStats = null;
                Dictionary<string, int>? topicFrequency = null;

                // If sentiment analysis was run, try to load results
                if (_featureConfig.EnableSentimentAnalysis && File.Exists(paths.SentimentJson))
                {
                    try
                    {
                        string sentimentJson = await File.ReadAllTextAsync(paths.SentimentJson);
                        sentimentResults = System.Text.Json.JsonSerializer.Deserialize<SMSXmlToCsv.Analysis.SentimentAnalysisResults>(sentimentJson);
                    }
                    catch { /* Ignore if can't load */ }
                }

                // If response time analysis was run, try to load results
                if (_featureConfig.EnableResponseTimeAnalysis && File.Exists(paths.ResponseTimesJson))
                {
                    try
                    {
                        string responseJson = await File.ReadAllTextAsync(paths.ResponseTimesJson);
                        Dictionary<string, ResponseTimeStats>? responseData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, SMSXmlToCsv.Analysis.ResponseTimeStats>>(responseJson);
                        responseStats = responseData;
                    }
                    catch { /* Ignore if can't load */ }
                }

                // If network graph was generated, extract topic frequency
                if (_featureConfig.GenerateNetworkGraph && File.Exists(paths.NetworkGraphJson))
                {
                    try
                    {
                        string graphJson = await File.ReadAllTextAsync(paths.NetworkGraphJson);
                        using (JsonDocument doc = System.Text.Json.JsonDocument.Parse(graphJson))
                        {
                            topicFrequency = new Dictionary<string, int>();
                            if (doc.RootElement.TryGetProperty("nodes", out JsonElement nodes))
                            {
                                foreach (JsonElement node in nodes.EnumerateArray())
                                {
                                    if (node.TryGetProperty("group", out JsonElement group) && group.GetInt32() == 2) // Topic nodes
                                    {
                                        string topicName = node.GetProperty("name").GetString() ?? "";
                                        int messageCount = node.GetProperty("value").GetInt32();
                                        if (!string.IsNullOrEmpty(topicName))
                                        {
                                            topicFrequency[topicName] = messageCount;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { /* Ignore if can't load */ }
                }

                // Use EnhancedPdfReportGenerator if we have additional data, otherwise fallback to basic
                if (sentimentResults != null || responseStats != null || topicFrequency != null)
                {
                    WriteColorLine("    ✓ Enhanced PDF with AI insights", ConsoleColor.Green);
                    SMSXmlToCsv.Reports.EnhancedPdfReportGenerator enhancedPdfGen =
                        new SMSXmlToCsv.Reports.EnhancedPdfReportGenerator();
                    await enhancedPdfGen.GenerateComprehensiveReportAsync(
                        messages, stats, paths.ComprehensiveReportPdf, _userPhone,
                        sentimentResults, responseStats, topicFrequency);

                    WriteColor("  ✓ ", ConsoleColor.Green);
                    Console.WriteLine("Enhanced PDF report generated with AI insights");
                }
                else
                {
                    // Fallback to basic PDF
                    SMSXmlToCsv.Reports.PdfReportGenerator pdfGen =
                        new SMSXmlToCsv.Reports.PdfReportGenerator();
                    await pdfGen.GenerateComprehensiveReportAsync(messages, stats, paths.BasicReportPdf, _userPhone);

                    WriteColor("  ✓ ", ConsoleColor.Green);
                    Console.WriteLine("PDF report generated");
                }
            }

            WriteColorLine($"✓ All advanced features complete → {paths.ExportsFolder}", ConsoleColor.Green);
        }
    }
}
