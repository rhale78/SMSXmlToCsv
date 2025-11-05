namespace SMSXmlToCsv.Models
{
    /// <summary>
    /// Feature enable modes
    /// </summary>
    public enum FeatureMode
    {
        Enable,
        Disable,
        Ask
    }

    /// <summary>
    /// Configuration for all converter features
    /// </summary>
    public class FeatureConfiguration
    {
        // Feature modes (from config/command-line)
        public FeatureMode ExtractMMS { get; set; } = FeatureMode.Ask;
        public FeatureMode SplitByContact { get; set; } = FeatureMode.Ask;
        public FeatureMode EnableFiltering { get; set; } = FeatureMode.Ask;
        public FeatureMode ExportToSQLite { get; set; } = FeatureMode.Ask;
        public FeatureMode ExportToHTML { get; set; } = FeatureMode.Ask;

        // NEW: Filter unknown contacts (no name, just phone number)
        public bool FilterUnknownContacts { get; set; } = false;

        // Runtime decisions (set during execution)
        public bool ShouldExtractMMS { get; set; }
        public bool ShouldSplitByContact { get; set; }
        public bool ShouldFilterContacts { get; set; }
        public bool ShouldExportToSQLite { get; set; }
        public bool ShouldExportToHTML { get; set; }

        // Date range filtering
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        // Contact filtering
        public HashSet<string> SelectedContacts { get; set; } = new HashSet<string>();
        public List<string> PreConfiguredContacts { get; set; } = new List<string>(); // From config file

        // Column selection
        public HashSet<string> SelectedColumns { get; set; } = new HashSet<string>();

        // Error handling
        public bool ContinueOnError { get; set; } = false;

        // NEW v1.6: Analysis features
        public bool EnableThreadAnalysis { get; set; } = false;
        public bool EnableResponseTimeAnalysis { get; set; } = false;
        public bool EnableAdvancedStatistics { get; set; } = false;

        // Thread analysis settings
        public int ThreadTimeoutMinutes { get; set; } = 60;
        public int MinimumThreadLength { get; set; } = 2;

        // Statistics export settings
        public bool ExportStatisticsJson { get; set; } = true;
        public bool ExportStatisticsMarkdown { get; set; } = true;

        // NEW v1.7: ML and Advanced Features
        public bool EnableSentimentAnalysis { get; set; } = false;
        public bool EnableClustering { get; set; } = false;
        public bool GenerateNetworkGraph { get; set; } = false;
        public bool GeneratePdfReport { get; set; } = false;
        public bool UseOllama { get; set; } = true;
        public string OllamaModel { get; set; } = "llama3.2";
        public int SentimentAnalysisMaxMessages { get; set; } = 1000;
        public int ClusterCount { get; set; } = 5;

        public static FeatureMode ParseMode(string? value)
        {
            return string.IsNullOrEmpty(value)
                ? FeatureMode.Ask
                : value.ToLowerInvariant() switch
                {
                    "enable" or "enabled" or "yes" or "true" => FeatureMode.Enable,
                    "disable" or "disabled" or "no" or "false" => FeatureMode.Disable,
                    _ => FeatureMode.Ask
                };
        }

        /// <summary>
        /// Get default columns (required + commonly used)
        /// </summary>
        public static HashSet<string> GetDefaultColumns()
        {
            return new HashSet<string>
            {
                "FromPhone",    // Required
                "ToPhone",      // Required
                "Direction",    // Required
                "DateTime",     // Required
                "UnixTimestamp", // Required
                "FromName",     // Default included
                "ToName",       // Default included
                "MessageText"   // Default included
            };
        }

        /// <summary>
        /// Get required columns that cannot be deselected
        /// </summary>
        public static HashSet<string> GetRequiredColumns()
        {
            return new HashSet<string>
            {
                "FromPhone",
                "ToPhone",
                "Direction",
                "DateTime",
                "UnixTimestamp"
            };
        }

        /// <summary>
        /// Get all available columns
        /// </summary>
        public static List<string> GetAvailableColumns()
        {
            return new List<string>
            {
                "FromName",
                "FromPhone",
                "ToName",
                "ToPhone",
                "Direction",
                "DateTime",
                "UnixTimestamp",
                "MessageText",
                "HasMMS",
                "MMS_Files"
            };
        }
    }

    /// <summary>
    /// Folder configuration
    /// </summary>
    public class FolderConfiguration
    {
        public string OutputBasePath { get; set; } = string.Empty;
        public string MMSFolderName { get; set; } = "MMS";
        public string ContactsFolderName { get; set; } = "Contacts";
    }

    /// <summary>
    /// MMS attachment information
    /// </summary>
    public class MmsAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string FilePath { get; set; } = string.Empty;
    }
}
