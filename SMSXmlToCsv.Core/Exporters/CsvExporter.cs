using System.Text;
using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Core.Exporters;

/// <summary>
/// CSV format exporter that writes messages to comma-separated value files.
/// </summary>
public class CsvExporter : BaseDataExporter
{
    /// <inheritdoc/>
    public override string FileExtension => "csv";

    /// <inheritdoc/>
    protected override async Task ExportToFileAsync(IEnumerable<Message> messages, string filePath)
    {
        var sb = new StringBuilder();
        
        // Write CSV header
        sb.AppendLine("Id,Timestamp,Type,IsSent,PhoneNumber,ContactName,Body");

        // Write message rows
        foreach (var message in messages)
        {
            var contactName = message.Contact?.Name ?? "Unknown";
            var timestamp = message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            var body = EscapeCsvField(message.Body);
            
            sb.AppendLine($"{message.Id},{timestamp},{message.Type},{message.IsSent},{message.PhoneNumber},{contactName},{body}");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// Escapes a CSV field by wrapping it in quotes if it contains special characters.
    /// </summary>
    /// <param name="field">The field to escape.</param>
    /// <returns>The escaped field value.</returns>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
