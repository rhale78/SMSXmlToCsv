using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters;

/// <summary>
/// Exports messages to JSONL (JSON Lines) format.
/// Each line is a separate JSON object representing a single message.
/// </summary>
public class JsonlExporter : IDataExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string FileExtension => "jsonl";

    public async Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName)
    {
        string filePath = Path.Combine(outputDirectory, $"{baseFileName}.{FileExtension}");

        using StreamWriter streamWriter = new StreamWriter(filePath);

        foreach (Message message in messages)
        {
            // Serialize the message to JSON
            string jsonString = JsonSerializer.Serialize(message, JsonOptions);
            await streamWriter.WriteLineAsync(jsonString);
        }
    }
}
