using System.Text;
using System.Text.Json;
using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Core.Exporters;

/// <summary>
/// JSON Lines (JSONL) format exporter that writes each message as a separate JSON object on its own line.
/// </summary>
public class JsonlExporter : BaseDataExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public override string FileExtension => "jsonl";

    /// <inheritdoc/>
    protected override async Task ExportToFileAsync(IEnumerable<Message> messages, string filePath)
    {
        var sb = new StringBuilder();

        foreach (var message in messages)
        {
            var jsonObject = new
            {
                Id = message.Id,
                Timestamp = message.Timestamp,
                Type = message.Type,
                IsSent = message.IsSent,
                PhoneNumber = message.PhoneNumber,
                ContactName = message.Contact?.Name ?? "Unknown",
                ContactId = message.Contact?.Id,
                Body = message.Body
            };

            var json = JsonSerializer.Serialize(jsonObject, JsonOptions);
            sb.AppendLine(json);
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }
}
