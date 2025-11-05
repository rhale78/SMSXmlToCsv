using System.Text.Json;

using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Templates
{
    /// <summary>
    /// Export template system for saving/loading export configurations
    /// </summary>
    public class ExportTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HashSet<OutputFormat> Formats { get; set; } = new HashSet<OutputFormat>();
        public HashSet<string> SelectedColumns { get; set; } = new HashSet<string>();
        public bool ExtractMMS { get; set; }
        public bool SplitByContact { get; set; }
        public bool ExportToSQLite { get; set; }
        public bool ExportToHTML { get; set; }
        public bool EnableThreadAnalysis { get; set; }
        public bool EnableResponseTimeAnalysis { get; set; }
        public bool EnableAdvancedStatistics { get; set; }
        public bool EnableSentimentAnalysis { get; set; }
        public bool EnableClustering { get; set; }
        public bool GenerateNetworkGraph { get; set; }
        public bool GeneratePdfReport { get; set; }

        public void ApplyToConfiguration(FeatureConfiguration config)
        {
            config.ShouldExtractMMS = ExtractMMS;
            config.ShouldSplitByContact = SplitByContact;
            config.ShouldExportToSQLite = ExportToSQLite;
            config.ShouldExportToHTML = ExportToHTML;
            config.SelectedColumns = SelectedColumns;
            config.EnableThreadAnalysis = EnableThreadAnalysis;
            config.EnableResponseTimeAnalysis = EnableResponseTimeAnalysis;
            config.EnableAdvancedStatistics = EnableAdvancedStatistics;
            config.EnableSentimentAnalysis = EnableSentimentAnalysis;
            config.EnableClustering = EnableClustering;
            config.GenerateNetworkGraph = GenerateNetworkGraph;
            config.GeneratePdfReport = GeneratePdfReport;
        }

        public static ExportTemplate FromConfiguration(FeatureConfiguration config, string name, string description)
        {
            return new ExportTemplate
            {
                Name = name,
                Description = description,
                ExtractMMS = config.ShouldExtractMMS,
                SplitByContact = config.ShouldSplitByContact,
                ExportToSQLite = config.ShouldExportToSQLite,
                ExportToHTML = config.ShouldExportToHTML,
                SelectedColumns = config.SelectedColumns,
                EnableThreadAnalysis = config.EnableThreadAnalysis,
                EnableResponseTimeAnalysis = config.EnableResponseTimeAnalysis,
                EnableAdvancedStatistics = config.EnableAdvancedStatistics,
                EnableSentimentAnalysis = config.EnableSentimentAnalysis,
                EnableClustering = config.EnableClustering,
                GenerateNetworkGraph = config.GenerateNetworkGraph,
                GeneratePdfReport = config.GeneratePdfReport
            };
        }

        public async Task SaveAsync(string path)
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }

        public static async Task<ExportTemplate?> LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<ExportTemplate>(json);
        }
    }

    /// <summary>
    /// Built-in export templates
    /// </summary>
    public static class BuiltInTemplates
    {
        public static ExportTemplate QuickExport => new ExportTemplate
        {
            Name = "Quick Export",
            Description = "Fast CSV export with basic columns",
            Formats = new HashSet<OutputFormat> { OutputFormat.Csv },
            SelectedColumns = new HashSet<string> { "DateTime", "Direction", "FromName", "ToName", "MessageText" },
            ExtractMMS = false,
            SplitByContact = false
        };

        public static ExportTemplate FullAnalysis => new ExportTemplate
        {
            Name = "Full Analysis",
            Description = "Complete analysis with all features",
            Formats = new HashSet<OutputFormat> { OutputFormat.Csv, OutputFormat.Parquet, OutputFormat.Markdown },
            SelectedColumns = FeatureConfiguration.GetDefaultColumns(),
            ExtractMMS = true,
            SplitByContact = true,
            ExportToSQLite = true,
            ExportToHTML = true,
            EnableThreadAnalysis = true,
            EnableResponseTimeAnalysis = true,
            EnableAdvancedStatistics = true,
            GenerateNetworkGraph = true,
            GeneratePdfReport = true
        };

        public static ExportTemplate AIOptimized => new ExportTemplate
        {
            Name = "AI Optimized",
            Description = "Optimized for AI processing (Parquet, minimal columns)",
            Formats = new HashSet<OutputFormat> { OutputFormat.Parquet },
            SelectedColumns = new HashSet<string> { "DateTime", "Direction", "MessageText", "FromPhone", "ToPhone" },
            ExtractMMS = false,
            SplitByContact = false,
            EnableAdvancedStatistics = true
        };

        public static ExportTemplate DatabaseReady => new ExportTemplate
        {
            Name = "Database Ready",
            Description = "SQL exports for database import",
            Formats = new HashSet<OutputFormat> { OutputFormat.SQLite, OutputFormat.PostgreSQL, OutputFormat.MySQL },
            SelectedColumns = FeatureConfiguration.GetDefaultColumns(),
            ExtractMMS = true,
            SplitByContact = false
        };

        public static List<ExportTemplate> GetAll()
        {
            return new List<ExportTemplate>
            {
                QuickExport,
                FullAnalysis,
                AIOptimized,
                DatabaseReady
            };
        }
    }
}
