namespace SMSXmlToCsv.Utils
{
    /// <summary>
    /// Helper for organizing export file paths in a structured manner
    /// </summary>
    public static class ExportPathHelper
    {
        private const string EXPORTS_FOLDER = "Exports";
        private const string CONTACTS_FOLDER = "Contacts";
        private const string MMS_FOLDER = "MMS";

        /// <summary>
        /// Get the exports folder path for analysis results (PDFs, graphs, stats)
        /// </summary>
        /// <param name="baseOutputPath">Base output directory</param>
        /// <param name="sourceFileName">Original XML filename without extension</param>
        /// <returns>Path like: baseOutput/Exports/sourceFileName/</returns>
        public static string GetExportsFolder(string baseOutputPath, string sourceFileName)
        {
            string exportsDir = Path.Combine(baseOutputPath, EXPORTS_FOLDER, sourceFileName);
            Directory.CreateDirectory(exportsDir);
            return exportsDir;
        }

        /// <summary>
        /// Get a specific export file path
        /// </summary>
        public static string GetExportPath(string baseOutputPath, string sourceFileName, string suffix, string extension)
        {
            string exportsFolder = GetExportsFolder(baseOutputPath, sourceFileName);
            return Path.Combine(exportsFolder, $"{sourceFileName}{suffix}.{extension}");
        }

        /// <summary>
        /// Get contacts folder path
        /// </summary>
        public static string GetContactsFolder(string baseOutputPath)
        {
            string contactsDir = Path.Combine(baseOutputPath, CONTACTS_FOLDER);
            Directory.CreateDirectory(contactsDir);
            return contactsDir;
        }

        /// <summary>
        /// Get MMS folder path
        /// </summary>
        public static string GetMmsFolder(string baseOutputPath)
        {
            string mmsDir = Path.Combine(baseOutputPath, MMS_FOLDER);
            Directory.CreateDirectory(mmsDir);
            return mmsDir;
        }

        /// <summary>
        /// Get all common export paths for a source file
        /// </summary>
        public static ExportPaths GetExportPaths(string baseOutputPath, string sourceFileName)
        {
            return new ExportPaths(baseOutputPath, sourceFileName);
        }
    }

    /// <summary>
    /// Strongly-typed export paths for all analysis outputs
    /// </summary>
    public class ExportPaths
    {
        private readonly string _baseOutputPath;
        private readonly string _sourceFileName;
        private readonly string _exportsFolder;

        public ExportPaths(string baseOutputPath, string sourceFileName)
        {
            _baseOutputPath = baseOutputPath;
            _sourceFileName = sourceFileName;
            _exportsFolder = ExportPathHelper.GetExportsFolder(baseOutputPath, sourceFileName);
        }

        // Analysis Results
        public string ComprehensiveReportPdf => Path.Combine(_exportsFolder, $"{_sourceFileName}_comprehensive_report.pdf");
        public string BasicReportPdf => Path.Combine(_exportsFolder, $"{_sourceFileName}_report.pdf");
        public string StatsJson => Path.Combine(_exportsFolder, $"{_sourceFileName}_stats.json");
        public string StatsMarkdown => Path.Combine(_exportsFolder, $"{_sourceFileName}_stats.md");
        public string SentimentJson => Path.Combine(_exportsFolder, $"{_sourceFileName}_sentiment.json");
        public string ResponseTimesJson => Path.Combine(_exportsFolder, $"{_sourceFileName}_response_times.json");
        public string ThreadsJson => Path.Combine(_exportsFolder, $"{_sourceFileName}_threads.json");
        public string ClustersJson => Path.Combine(_exportsFolder, $"{_sourceFileName}_clusters.json");
        public string NetworkGraphJson => Path.Combine(_exportsFolder, $"{_sourceFileName}_network_graph.json");
        public string NetworkGraphHtml => Path.Combine(_exportsFolder, $"{_sourceFileName}_network_graph.html");

        // Folder paths
        public string ExportsFolder => _exportsFolder;
        public string ContactsFolder => ExportPathHelper.GetContactsFolder(_baseOutputPath);
        public string MmsFolder => ExportPathHelper.GetMmsFolder(_baseOutputPath);

        // Database exports (if not split by contact)
        public string SqliteDb => Path.Combine(_baseOutputPath, $"{_sourceFileName}.db");

        // Non-split exports (if not split by contact)
        public string CsvFile => Path.Combine(_baseOutputPath, $"{_sourceFileName}.csv");
        public string JsonLinesFile => Path.Combine(_baseOutputPath, $"{_sourceFileName}.jsonl");
        public string ParquetFile => Path.Combine(_baseOutputPath, $"{_sourceFileName}.parquet");
    }
}
