namespace SMSXmlToCsv.Models
{
    /// <summary>
    /// Available output formats
    /// </summary>
    public enum OutputFormat
    {
        None = 0,
        Csv = 1,
        JsonLines = 2,
        Parquet = 3,
        SQLite = 4,
        Html = 5,
        PostgreSQL = 6,     // NEW v1.6
        MySQL = 7,          // NEW v1.6
        Markdown = 8        // NEW v1.6
    }
}
