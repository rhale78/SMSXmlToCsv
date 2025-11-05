using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters;

/// <summary>
/// Exports messages to CSV (Comma-Separated Values) format using CsvHelper.
/// </summary>
public class CsvExporter : IDataExporter
{
    public string FileExtension => "csv";

    public async Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName)
    {
        string filePath = Path.Combine(outputDirectory, $"{baseFileName}.{FileExtension}");
        
        using StreamWriter writer = new StreamWriter(filePath);
        using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write the CSV records
        IEnumerable<MessageCsvRecord> records = messages.Select(m => new MessageCsvRecord
        {
            SourceApplication = m.SourceApplication,
            From = m.From.Name,
            FromPhone = string.Join(";", m.From.PhoneNumbers),
            FromEmail = string.Join(";", m.From.Emails),
            To = m.To.Name,
            ToPhone = string.Join(";", m.To.PhoneNumbers),
            ToEmail = string.Join(";", m.To.Emails),
            TimestampUtc = m.TimestampUtc.ToString("o"), // ISO 8601 format
            Direction = m.Direction.ToString(),
            Body = m.Body,
            AttachmentCount = m.Attachments.Count,
            Attachments = string.Join(";", m.Attachments.Select(a => a.FileName))
        });

        await csv.WriteRecordsAsync(records);
    }

    /// <summary>
    /// Internal record for CSV serialization.
    /// </summary>
    private class MessageCsvRecord
    {
        public string SourceApplication { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string FromPhone { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string ToPhone { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public string TimestampUtc { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int AttachmentCount { get; set; }
        public string Attachments { get; set; } = string.Empty;
    }
}
