using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;
using SMSXmlToCsv.Utils;

namespace SMSXmlToCsv
{
    public static class FeatureMenu
    {
        public static async Task<bool> ShowConfigurationMenuAsync(FeatureConfiguration config)
        {
            bool exitMenu = false;
            bool saveRequested = false;

            while (!exitMenu)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== SMS Backup XML Converter - Configuration ===\n");
                Console.ResetColor();

                Console.WriteLine("Current Settings:");
                DisplaySetting("1", "MMS Extraction", config.ShouldExtractMMS);
                DisplaySetting("2", "Split by Contact", config.ShouldSplitByContact);

                if (config.ShouldSplitByContact)
                {
                    DisplaySetting("3", "Contact Filtering", config.ShouldFilterContacts, "    ");
                    DisplaySetting("4", "SQLite Export", config.ShouldExportToSQLite, "    ");
                    DisplaySetting("5", "HTML Export", config.ShouldExportToHTML, "    ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  3-5. [Requires Split by Contact (#2)]");
                    Console.ResetColor();
                }

                string columnStatus = config.SelectedColumns.Count > 0
                       ? $"{config.SelectedColumns.Count} selected"
                    : "Defaults";
                Console.WriteLine($"  6. Column Selection.......... {columnStatus}");

                string dateStatus = config.DateFrom.HasValue || config.DateTo.HasValue
                     ? $"{config.DateFrom?.ToString("yyyy-MM-dd") ?? "Any"} to {config.DateTo?.ToString("yyyy-MM-dd") ?? "Any"}"
           : "All Dates";
                Console.WriteLine($"  7. Date Range Filter......... {dateStatus}");

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Analysis Features (v1.6):");
                Console.ResetColor();
                DisplaySetting("8", "Thread Analysis", config.EnableThreadAnalysis);
                DisplaySetting("9", "Response Time Analysis", config.EnableResponseTimeAnalysis);
                DisplaySetting("10", "Advanced Statistics", config.EnableAdvancedStatistics);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("ML & Advanced Features (v1.7):");
                Console.ResetColor();
                DisplaySetting("11", "Sentiment Analysis", config.EnableSentimentAnalysis);
                DisplaySetting("12", "Conversation Clustering", config.EnableClustering);
                DisplaySetting("13", "Network Graph", config.GenerateNetworkGraph);
                DisplaySetting("14", "PDF Report", config.GeneratePdfReport);
                DisplaySetting("15", "Filter Unknown Contacts", config.FilterUnknownContacts);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Options: [1-15] Toggle/Configure | [M] Merge Contacts | [S] Save | [H] Help | [ESC] Exit | [Enter] Continue");
                Console.ResetColor();
                Console.Write("Your choice: ");

                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: false);

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠️  Exiting menu...");
                    Console.ResetColor();
                    Thread.Sleep(500);
                    exitMenu = true;
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Continuing with current settings...");
                    Console.ResetColor();
                    Thread.Sleep(800);
                    exitMenu = true;
                    continue;
                }

                string input = keyInfo.KeyChar.ToString().ToLowerInvariant();
                string? restOfLine = Console.ReadLine();
                if (!string.IsNullOrEmpty(restOfLine))
                {
                    input += restOfLine.ToLowerInvariant();
                }

                if (input == "s" || input == "save")
                {
                    saveRequested = true;
                    ConfigurationManager configManager = new ConfigurationManager();
                    ConfigurationManager.ConvertDecisionsToModes(config);
                    await configManager.SaveFeatureConfigurationAsync(config);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✓ Configuration saved to appsettings.json");
                    Console.ResetColor();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
                else if (input == "h" || input == "help" || input == "?")
                {
                    ShowHelp();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
                else if (input == "m" || input == "merge")
                {
                    await ShowContactMergeMenuAsync(config);
                }
                else if (int.TryParse(input, out int choice))
                {
                    await HandleMenuChoice(choice, config);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n✗ Invalid input. Press any key to continue...");
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }

            return saveRequested;
        }

        private static async Task HandleMenuChoice(int choice, FeatureConfiguration config)
        {
            switch (choice)
            {
                case 1:
                    config.ShouldExtractMMS = !config.ShouldExtractMMS;
                    AppLogger.Information($"MMS Extraction set to: {config.ShouldExtractMMS}");
                    break;

                case 2:
                    config.ShouldSplitByContact = !config.ShouldSplitByContact;
                    if (!config.ShouldSplitByContact)
                    {
                        config.ShouldFilterContacts = false;
                        config.ShouldExportToSQLite = false;
                        config.ShouldExportToHTML = false;
                    }
                    AppLogger.Information($"Split by Contact set to: {config.ShouldSplitByContact}");
                    break;

                case 3:
                    if (config.ShouldSplitByContact)
                    {
                        config.ShouldFilterContacts = !config.ShouldFilterContacts;
                        AppLogger.Information($"Contact Filtering set to: {config.ShouldFilterContacts}");
                    }
                    else
                    {
                        ShowRequirementWarning("Contact Filtering", "Split by Contact (#2)");
                    }
                    break;

                case 4:
                    if (config.ShouldSplitByContact)
                    {
                        config.ShouldExportToSQLite = !config.ShouldExportToSQLite;
                        AppLogger.Information($"SQLite Export set to: {config.ShouldExportToSQLite}");
                    }
                    else
                    {
                        ShowRequirementWarning("SQLite Export", "Split by Contact (#2)");
                    }
                    break;

                case 5:
                    if (config.ShouldSplitByContact)
                    {
                        config.ShouldExportToHTML = !config.ShouldExportToHTML;
                        AppLogger.Information($"HTML Export set to: {config.ShouldExportToHTML}");
                    }
                    else
                    {
                        ShowRequirementWarning("HTML Export", "Split by Contact (#2)");
                    }
                    break;

                case 6:
                    await ConfigureColumns(config);
                    break;

                case 7:
                    ConfigureDateRange(config);
                    break;

                case 8:
                    config.EnableThreadAnalysis = !config.EnableThreadAnalysis;
                    AppLogger.Information($"Thread Analysis set to: {config.EnableThreadAnalysis}");
                    break;

                case 9:
                    config.EnableResponseTimeAnalysis = !config.EnableResponseTimeAnalysis;
                    AppLogger.Information($"Response Time Analysis set to: {config.EnableResponseTimeAnalysis}");
                    break;

                case 10:
                    config.EnableAdvancedStatistics = !config.EnableAdvancedStatistics;
                    AppLogger.Information($"Advanced Statistics set to: {config.EnableAdvancedStatistics}");
                    break;

                case 11:
                    config.EnableSentimentAnalysis = !config.EnableSentimentAnalysis;
                    AppLogger.Information($"Sentiment Analysis set to: {config.EnableSentimentAnalysis}");
                    break;

                case 12:
                    config.EnableClustering = !config.EnableClustering;
                    AppLogger.Information($"Conversation Clustering set to: {config.EnableClustering}");
                    break;

                case 13:
                    config.GenerateNetworkGraph = !config.GenerateNetworkGraph;
                    AppLogger.Information($"Network Graph set to: {config.GenerateNetworkGraph}");
                    break;

                case 14:
                    config.GeneratePdfReport = !config.GeneratePdfReport;
                    AppLogger.Information($"PDF Report set to: {config.GeneratePdfReport}");
                    break;

                case 15:
                    config.FilterUnknownContacts = !config.FilterUnknownContacts;
                    AppLogger.Information($"Filter Unknown Contacts set to: {config.FilterUnknownContacts}");
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n✗ Invalid option. Press any key...");
                    Console.ResetColor();
                    Console.ReadKey();
                    break;
            }
        }

        private static void DisplaySetting(string number, string name, bool enabled, string indent = "")
        {
            string status = enabled ? "ON" : "OFF";
            ConsoleColor statusColor = enabled ? ConsoleColor.Green : ConsoleColor.Red;

            int totalWidth = 33;
            int nameWidth = totalWidth - number.Length - 3 - status.Length;
            string dots = new string('.', Math.Max(1, nameWidth - name.Length));

            Console.Write($"{indent}  {number}. {name}{dots} ");
            Console.ForegroundColor = statusColor;
            Console.WriteLine(status);
            Console.ResetColor();
        }

        private static void ShowRequirementWarning(string feature, string requirement)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  {feature} requires {requirement} to be enabled first!");
            Console.ResetColor();
            Thread.Sleep(1500);
        }

        private static async Task ConfigureColumns(FeatureConfiguration config)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Column Selection ===\n");
            Console.ResetColor();

            List<string> availableColumns = FeatureConfiguration.GetAvailableColumns();
            HashSet<string> requiredColumns = FeatureConfiguration.GetRequiredColumns();

            if (config.SelectedColumns.Count == 0)
            {
                config.SelectedColumns = FeatureConfiguration.GetDefaultColumns();
            }

            Console.WriteLine("Select columns to export ([R] = Required, cannot be deselected):\n");

            for (int i = 0; i < availableColumns.Count; i++)
            {
                string column = availableColumns[i];
                bool isRequired = requiredColumns.Contains(column);
                bool isSelected = config.SelectedColumns.Contains(column);

                string marker = isRequired ? "[R]" : (isSelected ? "[✓]" : "[ ]");
                Console.ForegroundColor = isRequired ? ConsoleColor.Yellow : (isSelected ? ConsoleColor.Green : ConsoleColor.Gray);
                Console.WriteLine($"  {i + 1,2}. {marker} {column}");
                Console.ResetColor();
            }

            Console.WriteLine("\nOptions:");
            Console.WriteLine("  [1-10] Toggle column");
            Console.WriteLine("  [A] Select all");
            Console.WriteLine("  [D] Reset to defaults");
            Console.WriteLine("  [Enter] Done");
            Console.Write("\nYour choice: ");

            string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            else if (input == "a")
            {
                config.SelectedColumns = new HashSet<string>(availableColumns);
            }
            else if (input == "d")
            {
                config.SelectedColumns = FeatureConfiguration.GetDefaultColumns();
            }
            else if (int.TryParse(input, out int choice) && choice >= 1 && choice <= availableColumns.Count)
            {
                string column = availableColumns[choice - 1];
                if (!requiredColumns.Contains(column))
                {
                    if (config.SelectedColumns.Contains(column))
                    {
                        config.SelectedColumns.Remove(column);
                    }
                    else
                    {
                        config.SelectedColumns.Add(column);
                    }
                }
                await ConfigureColumns(config);
            }
        }

        private static void ConfigureDateRange(FeatureConfiguration config)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Date Range Filter ===\n");
            Console.ResetColor();

            Console.WriteLine($"Current: {(config.DateFrom?.ToString("yyyy-MM-dd") ?? "Any")} to {(config.DateTo?.ToString("yyyy-MM-dd") ?? "Any")}\n");

            Console.WriteLine("Options:");
            Console.WriteLine("  1. Set start date (YYYY-MM-DD)");
            Console.WriteLine("  2. Set end date (YYYY-MM-DD)");
            Console.WriteLine("  3. Clear filter (all dates)");
            Console.WriteLine("  4. Back to main menu");
            Console.Write("\nYour choice: ");

            string? input = Console.ReadLine()?.Trim();

            if (input == "1")
            {
                Console.Write("Enter start date (YYYY-MM-DD): ");
                string? dateStr = Console.ReadLine();
                if (DateTime.TryParse(dateStr, out DateTime date))
                {
                    config.DateFrom = date;
                    AppLogger.Information($"Date filter FROM set to: {date:yyyy-MM-dd}");
                }
                ConfigureDateRange(config);
            }
            else if (input == "2")
            {
                Console.Write("Enter end date (YYYY-MM-DD): ");
                string? dateStr = Console.ReadLine();
                if (DateTime.TryParse(dateStr, out DateTime date))
                {
                    config.DateTo = date;
                    AppLogger.Information($"Date filter TO set to: {date:yyyy-MM-dd}");
                }
                ConfigureDateRange(config);
            }
            else if (input == "3")
            {
                config.DateFrom = null;
                config.DateTo = null;
                AppLogger.Information("Date filter cleared");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Filter cleared");
                Console.ResetColor();
                Thread.Sleep(1000);
            }
        }

        public static void ApplyConfigurationModes(FeatureConfiguration config)
        {
            config.ShouldExtractMMS = config.ExtractMMS == FeatureMode.Enable;
            config.ShouldSplitByContact = config.SplitByContact == FeatureMode.Enable;
            config.ShouldFilterContacts = config.EnableFiltering == FeatureMode.Enable;
            config.ShouldExportToSQLite = config.ExportToSQLite == FeatureMode.Enable;
            config.ShouldExportToHTML = config.ExportToHTML == FeatureMode.Enable;

            if (!config.ShouldSplitByContact)
            {
                config.ShouldFilterContacts = false;
                config.ShouldExportToSQLite = false;
                config.ShouldExportToHTML = false;
            }
        }

        public static HashSet<string> SelectContacts(List<SmsMessage> messages, string userPhone)
        {
            HashSet<string> selected = new HashSet<string>();
            Dictionary<string, ContactInfo> contacts = BuildContactList(messages, userPhone);

            if (contacts.Count == 0)
            {
                Console.WriteLine("No contacts found.");
                return selected;
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Contact Selection ===\n");
            Console.ResetColor();

            Console.WriteLine($"Found {contacts.Count} contacts\n");
            Console.WriteLine("Display options:");
            Console.WriteLine("  1. Top 10 (recommended)");
            Console.WriteLine("  2. Top 25");
            Console.WriteLine("  3. Top 50");
            Console.WriteLine("  4. Top 100");
            Console.WriteLine("  5. All contacts");
            Console.Write("\nChoice [1]: ");

            string choice = Console.ReadLine()?.Trim() ?? "1";
            int displayCount = choice switch
            {
                "2" => 25,
                "3" => 50,
                "4" => 100,
                "5" => contacts.Count,
                _ => 10
            };

            List<ContactInfo> sortedContacts = contacts.Values
                    .OrderByDescending(c => c.MessageCount)
                 .Take(displayCount)
                 .ToList();

            ShowContactSelectionMenu(sortedContacts, selected);

            AppLogger.Information($"Contact filtering: {selected.Count} contacts selected");
            return selected;
        }

        private static void ShowContactSelectionMenu(List<ContactInfo> contacts, HashSet<string> selected)
        {
            bool done = false;

            while (!done)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== Select Contacts to Export ===\n");
                Console.ResetColor();

                for (int i = 0; i < contacts.Count; i++)
                {
                    ContactInfo contact = contacts[i];
                    string maskedPhone = MaskPhoneNumber(contact.Phone);
                    bool isSelected = selected.Contains(contact.Key);

                    string marker = isSelected ? "[✓]" : "[ ]";
                    Console.ForegroundColor = isSelected ? ConsoleColor.Green : ConsoleColor.Gray;
                    Console.WriteLine($"  {i + 1,3}. {marker} {contact.Name,-30} ({maskedPhone}) - {contact.MessageCount:N0} msgs");
                    Console.ResetColor();
                }

                Console.WriteLine("\nOptions:");
                Console.WriteLine("  [Numbers] Toggle: '1,3,5' or '1-10'");
                Console.WriteLine("  [A] Select all  [N] Select none");
                Console.WriteLine("  [Enter] Done");
                Console.Write("\nYour choice: ");

                string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

                if (string.IsNullOrEmpty(input))
                {
                    done = true;
                }
                else if (input == "a")
                {
                    foreach (ContactInfo contact in contacts)
                    {
                        selected.Add(contact.Key);
                    }
                }
                else if (input == "n")
                {
                    selected.Clear();
                }
                else
                {
                    HashSet<int> indices = ParseSelection(input, contacts.Count);
                    foreach (int idx in indices)
                    {
                        if (idx >= 0 && idx < contacts.Count)
                        {
                            string key = contacts[idx].Key;
                            if (selected.Contains(key))
                            {
                                selected.Remove(key);
                            }
                            else
                            {
                                selected.Add(key);
                            }
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ {selected.Count} contact(s) selected");
            Console.ResetColor();
            Thread.Sleep(1000);
        }

        private static Dictionary<string, ContactInfo> BuildContactList(List<SmsMessage> messages, string userPhone)
        {
            Dictionary<string, ContactInfo> contacts = new Dictionary<string, ContactInfo>();

            foreach (SmsMessage msg in messages)
            {
                string contactName = msg.Direction == "Sent" ? msg.ToName : msg.FromName;
                string contactPhone = msg.Direction == "Sent" ? msg.ToPhone : msg.FromPhone;

                if (contactPhone == userPhone)
                {
                    continue;
                }

                string key = $"{contactName}|{contactPhone}";

                if (!contacts.ContainsKey(key))
                {
                    contacts[key] = new ContactInfo
                    {
                        Key = key,
                        Name = contactName,
                        Phone = contactPhone,
                        MessageCount = 0
                    };
                }

                contacts[key].MessageCount++;
            }

            return contacts;
        }

        private static HashSet<int> ParseSelection(string input, int max)
        {
            HashSet<int> indices = new HashSet<int>();
            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string part in parts)
            {
                if (part.Contains('-'))
                {
                    string[] range = part.Split('-');
                    if (range.Length == 2 &&
                   int.TryParse(range[0], out int start) &&
                    int.TryParse(range[1], out int end))
                    {
                        for (int i = start; i <= end && i <= max; i++)
                        {
                            if (i > 0)
                            {
                                indices.Add(i - 1);
                            }
                        }
                    }
                }
                else if (int.TryParse(part, out int num))
                {
                    if (num > 0 && num <= max)
                    {
                        indices.Add(num - 1);
                    }
                }
            }

            return indices;
        }

        private static string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            {
                return "+****";
            }

            string digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 ? $"+****{digits.Substring(digits.Length - 4)}" : "+****";
        }

        public static bool IsUnknownContact(string contactName, string contactPhone)
        {
            return string.IsNullOrWhiteSpace(contactName) ||
        contactName == "(Unknown)" ||
          contactName == contactPhone;
        }

        private static async Task ShowContactMergeMenuAsync(FeatureConfiguration config)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Contact Merge Configuration ===\n");
            Console.ResetColor();

            Console.WriteLine("This feature allows you to merge duplicate contacts that may appear");
            Console.WriteLine("with different phone numbers or names in your backup.\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("NOTE: This feature requires message parsing and will be available");
            Console.WriteLine("after selecting the input file in the next step.\n");
            Console.ResetColor();

            Console.WriteLine("To use contact merging:");
            Console.WriteLine("  1. Select your SMS backup file");
            Console.WriteLine("  2. The system will detect potential duplicate contacts");
            Console.WriteLine("  3. You can choose which contacts to merge");
            Console.WriteLine("  4. Select the resulting name and phone number for merged contacts\n");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Contact merging will be available in the next processing step.");
            Console.ResetColor();

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Configuration Help ===\n");
            Console.ResetColor();

            Console.WriteLine("FEATURES:\n");
            Console.WriteLine("1. MMS Extraction - Extract images, videos from MMS messages");
            Console.WriteLine("2. Split by Contact - Separate folders per contact");
            Console.WriteLine("3. Contact Filtering - Select specific contacts to export");
            Console.WriteLine("4. SQLite Export - Create searchable database");
            Console.WriteLine("5. HTML Export - Generate chat-style HTML pages");
            Console.WriteLine("6. Column Selection - Choose which data fields to export");
            Console.WriteLine("7. Date Range Filter - Limit messages by date");
            Console.WriteLine("8. Thread Analysis - Analyze message threads for insights");
            Console.WriteLine("9. Response Time Analysis - Measure response times between messages");
            Console.WriteLine("10. Advanced Statistics - Enable detailed messaging statistics");
            Console.WriteLine("11. Sentiment Analysis - Analyze sentiment of messages");
            Console.WriteLine("12. Conversation Clustering - Cluster similar conversations");
            Console.WriteLine("13. Network Graph - Generate a graph of contacts and interactions");
            Console.WriteLine("14. PDF Report - Generate a PDF report of the analysis results");
            Console.WriteLine("15. Filter Unknown Contacts - Exclude contacts without names\n");

            Console.WriteLine("SPECIAL OPTIONS:\n");
            Console.WriteLine("M. Merge Contacts - Combine duplicate contacts into one\n");

            Console.WriteLine("NOTES:");
            Console.WriteLine("• Options 3-5 require Split by Contact (#2) to be enabled");
            Console.WriteLine("• Option 15 filters out contacts with no name (only phone number)");
            Console.WriteLine("• Contact merging is available after file selection");
            Console.WriteLine("• Settings can be saved with 'S' command");
            Console.WriteLine("• Press ESC in any menu to go back");
            Console.WriteLine("• Command-line args override these settings");
        }

        private class ContactInfo
        {
            public string Key { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public int MessageCount { get; set; }
        }

        // COMPLETE MERGE INTERFACE WITH SKIP FUNCTIONALITY
        public static async Task<(List<SmsMessage> messages, List<MergeDecision>? mergeDecisions)> ShowContactMergeInterface(
  List<SmsMessage> messages,
 string userPhone,
List<MergeDecision>? savedMerges = null)
        {
            ContactMerger merger = new SMSXmlToCsv.Utils.ContactMerger();

            // Load and apply saved merges (excluding skipped ones)
            if (savedMerges != null && savedMerges.Count > 0)
            {
                List<MergeDecision> activeMerges = savedMerges.Where(m => !m.IsSkipped).ToList();

                if (activeMerges.Count > 0)
                {
                    Console.Clear();
                    Console.WriteLine($"ℹ️  Found {activeMerges.Count} saved merge(s)\n");
                    Console.Write("Apply saved merges? (y/n) [y]: ");
                    string? apply = Console.ReadLine()?.Trim().ToLowerInvariant();

                    if (string.IsNullOrEmpty(apply) || apply == "y" || apply == "yes")
                    {
                        foreach (MergeDecision? merge in activeMerges)
                        {
                            if (merge.SourceContacts.Count >= 2)
                            {
                                var parts1 = merge.SourceContacts[0].Split('|');
                                var parts2 = merge.SourceContacts[1].Split('|');

                                if (parts1.Length == 2 && parts2.Length == 2)
                                {
                                    merger.AddMergeRule(
                                 parts1[1], parts1[0],
                                  parts2[1], parts2[0],
                                   merge.TargetPhone, merge.TargetName
                                    );
                                }
                            }
                        }

                        messages = merger.ApplyMerges(messages);
                        Console.WriteLine($"✓ Applied {activeMerges.Count} merge(s)");
                        Thread.Sleep(1000);

                        // DON'T return - allow user to add more merges
                        Console.WriteLine("\nYou can now add additional merges or continue.");
                        Thread.Sleep(1500);
                        // Fall through to merge menu instead of returning
                    }
                }
            }

            bool continueMerging = true;
            bool mergesApplied = false;

            while (continueMerging)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== Contact Merge ===\n");
                Console.ResetColor();

                Console.WriteLine($"Total merge rules configured: {merger.MergeRuleCount}\n");

                Console.WriteLine("Options:");
                Console.WriteLine("  1. Detect potential duplicates");
                Console.WriteLine("  2. Search and merge contacts");
                Console.WriteLine("  3. Manual merge (enter phone numbers)");
                Console.WriteLine("4. Apply merges and continue");
                Console.WriteLine("  5. Skip merging");
                Console.Write("\nYour choice: ");

                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await DetectAndMergeDuplicates(messages, userPhone, merger, savedMerges);
                        break;

                    case "2":
                        await SearchAndMergeContacts(messages, userPhone, merger);
                        break;

                    case "3":
                        await ManualMerge(messages, userPhone, merger);
                        break;

                    case "4":
                        if (merger.MergeRuleCount > 0)
                        {
                            messages = merger.ApplyMerges(messages);
                            mergesApplied = true;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n✓ Applied {merger.MergeRuleCount} merge rules");
                            Console.ResetColor();

                            Console.Write("\nSave merge decisions for future use? (y/n) [y]: ");
                            string? saveChoice = Console.ReadLine()?.Trim().ToLowerInvariant();

                            if (string.IsNullOrEmpty(saveChoice) || saveChoice == "y" || saveChoice == "yes")
                            {
                                List<MergeDecision> mergeDecisions = merger.GetMergeDecisions();
                                ConfigurationManager configManager = new ConfigurationManager();

                                // Merge with existing skipped decisions
                                List<MergeDecision> existingMerges = await configManager.LoadContactMergesAsync() ?? new List<MergeDecision>();
                                List<MergeDecision> skippedMerges = existingMerges.Where(m => m.IsSkipped).ToList();
                                List<MergeDecision> allMerges = skippedMerges.Concat(mergeDecisions).ToList();

                                await configManager.SaveContactMergesAsync(allMerges);
                                Thread.Sleep(1500);
                                continueMerging = false;
                                return (messages, mergeDecisions);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("⚠️  Merge decisions not saved");
                                Console.ResetColor();
                                Thread.Sleep(1000);
                            }
                        }
                        continueMerging = false;
                        break;

                    case "5":
                    case "":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n⚠️  Skipping contact merge");
                        Console.ResetColor();
                        Thread.Sleep(1000);
                        continueMerging = false;
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n✗ Invalid choice");
                        Console.ResetColor();
                        Thread.Sleep(1000);
                        break;
                }
            }

            if (mergesApplied && merger.MergeRuleCount > 0)
            {
                return (messages, merger.GetMergeDecisions());
            }

            return (messages, null);
        }

        // DETECT DUPLICATES WITH SKIP FUNCTIONALITY
        private static async Task DetectAndMergeDuplicates(
        List<SmsMessage> messages,
           string userPhone,
               SMSXmlToCsv.Utils.ContactMerger merger,
     List<MergeDecision>? savedMerges)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Detect Duplicates ===\n");
            Console.ResetColor();

            Console.WriteLine("Detecting potential duplicate contacts...\n");

            List<(string Phone1, string Name1, string Phone2, string Name2, string Reason)> potentialDuplicates = SMSXmlToCsv.Utils.ContactMerger.DetectPotentialDuplicates(messages, userPhone);

            // Filter out already processed (merged or skipped) pairs
            if (savedMerges != null && savedMerges.Count > 0)
            {
                List<(string Phone1, string Name1, string Phone2, string Name2, string Reason)> filtered = new List<(string Phone1, string Name1, string Phone2, string Name2, string Reason)>();
                foreach ((string Phone1, string Name1, string Phone2, string Name2, string Reason) dup in potentialDuplicates)
                {
                    bool alreadyProcessed = savedMerges.Any(saved =>
                           {
                               if (saved.SourceContacts.Count >= 2)
                               {
                                   HashSet<string> phones = saved.SourceContacts.Select(s => s.Split('|').Last()).ToHashSet();
                                   return phones.Contains(dup.Phone1) && phones.Contains(dup.Phone2);
                               }
                               return false;
                           });

                    if (!alreadyProcessed)
                    {
                        filtered.Add(dup);
                    }
                }

                potentialDuplicates = filtered;

                if (potentialDuplicates.Count < SMSXmlToCsv.Utils.ContactMerger.DetectPotentialDuplicates(messages, userPhone).Count)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"ℹ️  Filtered out previously processed pairs\n");
                    Console.ResetColor();
                }
            }

            if (potentialDuplicates.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No new potential duplicate contacts detected!");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Found {potentialDuplicates.Count} potential duplicate(s):\n");

            for (int i = 0; i < potentialDuplicates.Count; i++)
            {
                (string Phone1, string Name1, string Phone2, string Name2, string Reason) dup = potentialDuplicates[i];
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{i + 1}. {dup.Reason}");
                Console.ResetColor();
                Console.WriteLine($"   A: {dup.Name1} ({MaskPhoneNumber(dup.Phone1)})");
                Console.WriteLine($"   B: {dup.Name2} ({MaskPhoneNumber(dup.Phone2)})");

                int impact = SMSXmlToCsv.Utils.ContactMerger.GetMergeImpact(messages, dup.Phone1, dup.Phone2);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"   Impact: {impact:N0} messages\n");
                Console.ResetColor();
            }

            Console.WriteLine("Options:");
            Console.WriteLine("  [1-N] Merge a specific pair");
            Console.WriteLine("  [A] Add all as merge rules");
            Console.WriteLine("  [K] Skip pair permanently (won't show again)");
            Console.WriteLine("  [S] Skip all");
            Console.Write("\nYour choice: ");

            string? choice = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (choice == "s" || string.IsNullOrEmpty(choice))
            {
                return;
            }

            // SKIP FUNCTIONALITY
            if (choice == "k")
            {
                Console.Write("Which pair to skip permanently? [1-N or 'all']: ");
                string? skipChoice = Console.ReadLine()?.Trim().ToLowerInvariant();

                if (skipChoice == "all")
                {
                    ConfigurationManager configManager = new ConfigurationManager();
                    List<MergeDecision> existingMerges = await configManager.LoadContactMergesAsync() ?? new List<MergeDecision>();

                    foreach ((string Phone1, string Name1, string Phone2, string Name2, string Reason) dup in potentialDuplicates)
                    {
                        existingMerges.Add(new MergeDecision
                        {
                            SourceContacts = new List<string> { $"{dup.Name1}|{dup.Phone1}", $"{dup.Name2}|{dup.Phone2}" },
                            TargetName = dup.Name1,
                            TargetPhone = dup.Phone1,
                            IsSkipped = true,
                            Reason = $"Skipped: {dup.Reason}"
                        });
                    }

                    await configManager.SaveContactMergesAsync(existingMerges);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ Skipped {potentialDuplicates.Count} pair(s) - won't show again");
                    Console.ResetColor();
                    Thread.Sleep(1500);
                }
                else if (int.TryParse(skipChoice, out int skipIdx) && skipIdx >= 1 && skipIdx <= potentialDuplicates.Count)
                {
                    (string Phone1, string Name1, string Phone2, string Name2, string Reason) dup = potentialDuplicates[skipIdx - 1];
                    ConfigurationManager configManager = new ConfigurationManager();
                    List<MergeDecision> existingMerges = await configManager.LoadContactMergesAsync() ?? new List<MergeDecision>();

                    existingMerges.Add(new MergeDecision
                    {
                        SourceContacts = new List<string> { $"{dup.Name1}|{dup.Phone1}", $"{dup.Name2}|{dup.Phone2}" },
                        TargetName = dup.Name1,
                        TargetPhone = dup.Phone1,
                        IsSkipped = true,
                        Reason = $"Skipped: {dup.Reason}"
                    });

                    await configManager.SaveContactMergesAsync(existingMerges);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ Skipped pair - won't show again");
                    Console.ResetColor();
                    Thread.Sleep(1500);
                }

                return;
            }

            if (choice == "a")
            {
                foreach ((string Phone1, string Name1, string Phone2, string Name2, string Reason) dup in potentialDuplicates)
                {
                    (string phone, string name)? result = PromptMergeDetails(dup.Phone1, dup.Name1, dup.Phone2, dup.Name2);
                    if (result.HasValue)
                    {
                        merger.AddMergeRule(
                      dup.Phone1, dup.Name1,
                    dup.Phone2, dup.Name2,
                        result.Value.phone, result.Value.name
                       );
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✓ Added {merger.MergeRuleCount} merge rules");
                Console.ResetColor();
                Thread.Sleep(1500);
            }
            else if (int.TryParse(choice, out int index) && index >= 1 && index <= potentialDuplicates.Count)
            {
                (string Phone1, string Name1, string Phone2, string Name2, string Reason) dup = potentialDuplicates[index - 1];
                (string phone, string name)? result = PromptMergeDetails(dup.Phone1, dup.Name1, dup.Phone2, dup.Name2);
                if (result.HasValue)
                {
                    merger.AddMergeRule(
          dup.Phone1, dup.Name1,
                     dup.Phone2, dup.Name2,
             result.Value.phone, result.Value.name
                  );

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✓ Merge rule added");
                    Console.ResetColor();
                    Thread.Sleep(1500);
                }
            }
        }

        private static async Task SearchAndMergeContacts(List<SmsMessage> messages, string userPhone, SMSXmlToCsv.Utils.ContactMerger merger)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Search Contacts ===\n");
            Console.ResetColor();

            Console.WriteLine("Enter search term (name or partial phone):");
            Console.WriteLine("Examples: 'Fran' (finds Frances, Francis, etc.), '1234' (finds phones containing 1234)");
            Console.Write("\nSearch: ");

            string? searchTerm = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }

            List<(string Phone, string Name, int MessageCount)> results = SMSXmlToCsv.Utils.ContactMerger.SearchContacts(messages, userPhone, searchTerm);

            if (results.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠️  No contacts found matching '{searchTerm}'");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"=== Search Results: {results.Count} matches for '{searchTerm}' ===\n");
            Console.ResetColor();

            for (int i = 0; i < results.Count && i < 20; i++)
            {
                (string Phone, string Name, int MessageCount) searchResult = results[i];
                string maskedPhone = MaskPhoneNumber(searchResult.Phone);
                Console.WriteLine($"  {i + 1}. {searchResult.Name} ({maskedPhone}) - {searchResult.MessageCount:N0} msgs");
            }

            if (results.Count > 20)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n... and {results.Count - 20} more results (showing top 20)");
                Console.ResetColor();
            }

            Console.WriteLine("\nSelect contacts to merge:");
            Console.WriteLine("Enter two numbers separated by comma (e.g., '1,3' to merge contact 1 with contact 3)");
            Console.WriteLine("Or press Enter to cancel");
            Console.Write("\nYour choice: ");

            string? mergeChoice = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(mergeChoice))
            {
                return;
            }

            string[] parts = mergeChoice.Split(',');
            if (parts.Length != 2 ||
            !int.TryParse(parts[0].Trim(), out int idx1) ||
                  !int.TryParse(parts[1].Trim(), out int idx2) ||
            idx1 < 1 || idx1 > results.Count ||
                     idx2 < 1 || idx2 > results.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ Invalid selection");
                Console.ResetColor();
                Thread.Sleep(1500);
                return;
            }

            (string Phone, string Name, int MessageCount) contact1 = results[idx1 - 1];
            (string Phone, string Name, int MessageCount) contact2 = results[idx2 - 1];

            (string phone, string name)? result = PromptMergeDetails(contact1.Phone, contact1.Name, contact2.Phone, contact2.Name);
            if (result.HasValue)
            {
                merger.AddMergeRule(
                        contact1.Phone, contact1.Name,
                            contact2.Phone, contact2.Name,
                        result.Value.phone, result.Value.name
                    );

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Merge rule added. You can add more merges or apply all at once.");
                Console.ResetColor();
                Thread.Sleep(1500);
            }
        }

        private static async Task ManualMerge(List<SmsMessage> messages, string userPhone, SMSXmlToCsv.Utils.ContactMerger merger)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Manual Merge ===\n");
            Console.ResetColor();

            Console.WriteLine("Enter details for two contacts to merge:");
            Console.WriteLine("(You can use search to find exact phone numbers)\n");

            Console.Write("First contact phone: ");
            string? phone1 = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(phone1))
            {
                return;
            }

            Console.Write("First contact name: ");
            string? name1 = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name1))
            {
                return;
            }

            Console.Write("Second contact phone: ");
            string? phone2 = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(phone2))
            {
                return;
            }

            Console.Write("Second contact name: ");
            string? name2 = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name2))
            {
                return;
            }

            (string phone, string name)? result = PromptMergeDetails(phone1, name1, phone2, name2);
            if (result.HasValue)
            {
                merger.AddMergeRule(
                        phone1, name1,
                         phone2, name2,
                      result.Value.phone, result.Value.name
                                );

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Merge rule added");
                Console.ResetColor();
                Thread.Sleep(1500);
            }
        }

        private static (string phone, string name)? PromptMergeDetails(string phone1, string name1, string phone2, string name2)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Configure Merge ===\n");
            Console.ResetColor();

            Console.WriteLine("Merging:");
            Console.WriteLine($"  A: {name1} ({phone1})");
            Console.WriteLine($"  B: {name2} ({phone2})\n");

            Console.WriteLine("Which phone number to use?");
            Console.WriteLine($"  1. {phone1}");
            Console.WriteLine($"  2. {phone2}");
            Console.Write("Choice [1]: ");

            string? phoneChoice = Console.ReadLine()?.Trim();
            string resultPhone = phoneChoice == "2" ? phone2 : phone1;

            Console.WriteLine("\nWhich name to use?");
            Console.WriteLine($"  1. {name1}");
            Console.WriteLine($"  2. {name2}");
            Console.WriteLine($"  3. Custom name");
            Console.Write("Choice [1]: ");

            string? nameChoice = Console.ReadLine()?.Trim();
            string resultName;

            if (nameChoice == "2")
            {
                resultName = name2;
            }
            else if (nameChoice == "3")
            {
                Console.Write("Enter custom name: ");
                resultName = Console.ReadLine()?.Trim() ?? name1;
            }
            else
            {
                resultName = name1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Will merge as: {resultName} ({resultPhone})");
            Console.ResetColor();
            Thread.Sleep(1000);

            return (resultPhone, resultName);
        }
    }
}