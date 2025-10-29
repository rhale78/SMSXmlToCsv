using System.Text.Json;

using SMSXmlToCsv.Models;
using SMSXmlToCsv.Utils;

namespace SMSXmlToCsv
{
    /// <summary>
    /// Manages reading and writing configuration files
    /// </summary>
    public class ConfigurationManager
    {
        private readonly string _appSettingsPath;

        public ConfigurationManager()
        {
            _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        }

        /// <summary>
        /// Save feature configuration to appsettings.json
        /// </summary>
        public async Task SaveFeatureConfigurationAsync(FeatureConfiguration config)
        {
            try
            {
                // Read existing configuration
                Dictionary<string, object> existingConfig = new Dictionary<string, object>();

                if (File.Exists(_appSettingsPath))
                {
                    string existingJson = await File.ReadAllTextAsync(_appSettingsPath);
                    Dictionary<string, JsonElement>? existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);

                    if (existing != null)
                    {
                        foreach (KeyValuePair<string, JsonElement> kvp in existing)
                        {
                            existingConfig[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Update Features section
                existingConfig["Features"] = new Dictionary<string, string>
                {
                    ["ExtractMMS"] = config.ExtractMMS.ToString(),
                    ["SplitByContact"] = config.SplitByContact.ToString(),
                    ["EnableFiltering"] = config.EnableFiltering.ToString(),
                    ["ExportToSQLite"] = config.ExportToSQLite.ToString(),
                    ["ExportToHTML"] = config.ExportToHTML.ToString()
                };

                // Write back to file
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(existingConfig, options);
                await File.WriteAllTextAsync(_appSettingsPath, json);

                ConsoleHelper.WriteLine($"\n✓ Configuration saved to: {_appSettingsPath}", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"\n✗ Failed to save configuration: {ex.Message}", ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Save folder configuration to appsettings.json
        /// </summary>
        public async Task SaveFolderConfigurationAsync(FolderConfiguration config)
        {
            try
            {
                // Read existing configuration
                Dictionary<string, object> existingConfig = new Dictionary<string, object>();

                if (File.Exists(_appSettingsPath))
                {
                    string existingJson = await File.ReadAllTextAsync(_appSettingsPath);
                    Dictionary<string, JsonElement>? existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);

                    if (existing != null)
                    {
                        foreach (KeyValuePair<string, JsonElement> kvp in existing)
                        {
                            existingConfig[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Update Folders section
                existingConfig["Folders"] = new Dictionary<string, string>
                {
                    ["OutputBasePath"] = config.OutputBasePath ?? string.Empty,
                    ["MMSFolderName"] = config.MMSFolderName,
                    ["ContactsFolderName"] = config.ContactsFolderName
                };

                // Write back to file
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(existingConfig, options);
                await File.WriteAllTextAsync(_appSettingsPath, json);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"\n✗ Failed to save folder configuration: {ex.Message}", ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Convert boolean decisions back to FeatureMode for saving
        /// </summary>
        public static void ConvertDecisionsToModes(FeatureConfiguration config)
        {
            // If the user made a decision, save it as Enable/Disable (not Ask)
            config.ExtractMMS = config.ShouldExtractMMS ? FeatureMode.Enable : FeatureMode.Disable;
            config.SplitByContact = config.ShouldSplitByContact ? FeatureMode.Enable : FeatureMode.Disable;
            config.EnableFiltering = config.ShouldFilterContacts ? FeatureMode.Enable : FeatureMode.Disable;
            config.ExportToSQLite = config.ShouldExportToSQLite ? FeatureMode.Enable : FeatureMode.Disable;
            config.ExportToHTML = config.ShouldExportToHTML ? FeatureMode.Enable : FeatureMode.Disable;
        }

        /// <summary>
        /// Save contact merge decisions to configuration
        /// </summary>
        public async Task SaveContactMergesAsync(List<MergeDecision> merges)
        {
            try
            {
                // Read existing configuration
                Dictionary<string, object> existingConfig = new Dictionary<string, object>();

                if (File.Exists(_appSettingsPath))
                {
                    string existingJson = await File.ReadAllTextAsync(_appSettingsPath);
                    Dictionary<string, JsonElement>? existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);

                    if (existing != null)
                    {
                        foreach (KeyValuePair<string, JsonElement> kvp in existing)
                        {
                            existingConfig[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Add ContactMerges section
                existingConfig["ContactMerges"] = merges.Select(m => new
                {
                    SourceContacts = m.SourceContacts,
                    TargetName = m.TargetName,
                    TargetPhone = m.TargetPhone,
                    MergedAt = m.MergedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Reason = m.Reason,
                    IsSkipped = m.IsSkipped
                }).ToList();

                // Write back to file
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(existingConfig, options);
                await File.WriteAllTextAsync(_appSettingsPath, json);

                ConsoleHelper.WriteLine($"\n✓ Saved {merges.Count} contact merge(s) to configuration", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"\n✗ Failed to save contact merges: {ex.Message}", ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Load saved contact merge decisions from configuration
        /// </summary>
        public async Task<List<MergeDecision>> LoadContactMergesAsync()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    return new List<MergeDecision>();
                }

                string json = await File.ReadAllTextAsync(_appSettingsPath);
                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ContactMerges", out JsonElement mergesElement))
                {
                    List<MergeDecision> merges = new List<MergeDecision>();

                    foreach (JsonElement mergeElement in mergesElement.EnumerateArray())
                    {
                        MergeDecision merge = new MergeDecision
                        {
                            SourceContacts = new List<string>(),
                            TargetName = mergeElement.GetProperty("TargetName").GetString() ?? string.Empty,
                            TargetPhone = mergeElement.GetProperty("TargetPhone").GetString() ?? string.Empty
                        };

                        // Parse MergedAt
                        if (mergeElement.TryGetProperty("MergedAt", out JsonElement mergedAtElement))
                        {
                            if (DateTime.TryParse(mergedAtElement.GetString(), out DateTime mergedAt))
                            {
                                merge.MergedAt = mergedAt;
                            }
                        }

                        // Parse Reason
                        if (mergeElement.TryGetProperty("Reason", out JsonElement reasonElement))
                        {
                            merge.Reason = reasonElement.GetString();
                        }

                        // Parse IsSkipped
                        if (mergeElement.TryGetProperty("IsSkipped", out JsonElement isSkippedElement))
                        {
                            merge.IsSkipped = isSkippedElement.GetBoolean();
                        }

                        // Parse SourceContacts array
                        if (mergeElement.TryGetProperty("SourceContacts", out JsonElement sourcesElement))
                        {
                            foreach (JsonElement sourceElement in sourcesElement.EnumerateArray())
                            {
                                string? source = sourceElement.GetString();
                                if (!string.IsNullOrEmpty(source))
                                {
                                    merge.SourceContacts.Add(source);
                                }
                            }
                        }

                        if (merge.SourceContacts.Count > 0)
                        {
                            merges.Add(merge);
                        }
                    }

                    return merges;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"\n⚠️  Failed to load contact merges: {ex.Message}", ConsoleColor.Yellow);
            }

            return new List<MergeDecision>();
        }
    }
}
