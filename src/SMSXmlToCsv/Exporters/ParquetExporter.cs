using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters;

/// <summary>
/// Exports messages to Apache Parquet format for efficient columnar storage and analytics.
/// </summary>
public class ParquetExporter : IDataExporter
{
    public string FileExtension => "parquet";

    public async Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName)
    {
        string filePath = Path.Combine(outputDirectory, $"{baseFileName}.{FileExtension}");
        List<Message> messageList = messages.ToList();

        if (!messageList.Any())
        {
            // Create an empty file with schema
            await CreateEmptyParquetFile(filePath);
            return;
        }

        // Define the Parquet schema
        ParquetSchema schema = new ParquetSchema(
            new DataField<string>("SourceApplication"),
            new DataField<string>("FromName"),
            new DataField<string>("FromPhone"),
            new DataField<string>("FromEmail"),
            new DataField<string>("ToName"),
            new DataField<string>("ToPhone"),
            new DataField<string>("ToEmail"),
            new DataField<DateTimeOffset>("TimestampUtc"),
            new DataField<string>("Direction"),
            new DataField<string>("Body"),
            new DataField<int>("AttachmentCount"),
            new DataField<string>("Attachments")
        );

        // Prepare data columns
        string[] sourceApplications = messageList.Select(m => m.SourceApplication).ToArray();
        string[] fromNames = messageList.Select(m => m.From.Name).ToArray();
        string[] fromPhones = messageList.Select(m => string.Join(";", m.From.PhoneNumbers)).ToArray();
        string[] fromEmails = messageList.Select(m => string.Join(";", m.From.Emails)).ToArray();
        string[] toNames = messageList.Select(m => m.To.Name).ToArray();
        string[] toPhones = messageList.Select(m => string.Join(";", m.To.PhoneNumbers)).ToArray();
        string[] toEmails = messageList.Select(m => string.Join(";", m.To.Emails)).ToArray();
        DateTimeOffset[] timestamps = messageList.Select(m => m.TimestampUtc).ToArray();
        string[] directions = messageList.Select(m => m.Direction.ToString()).ToArray();
        string[] bodies = messageList.Select(m => m.Body).ToArray();
        int[] attachmentCounts = messageList.Select(m => m.Attachments.Count).ToArray();
        string[] attachments = messageList.Select(m => 
            string.Join(";", m.Attachments.Select(a => a.FileName))).ToArray();

        // Create data columns
        DataColumn[] dataColumns = new DataColumn[]
        {
            new DataColumn(schema.DataFields[0], sourceApplications),
            new DataColumn(schema.DataFields[1], fromNames),
            new DataColumn(schema.DataFields[2], fromPhones),
            new DataColumn(schema.DataFields[3], fromEmails),
            new DataColumn(schema.DataFields[4], toNames),
            new DataColumn(schema.DataFields[5], toPhones),
            new DataColumn(schema.DataFields[6], toEmails),
            new DataColumn(schema.DataFields[7], timestamps),
            new DataColumn(schema.DataFields[8], directions),
            new DataColumn(schema.DataFields[9], bodies),
            new DataColumn(schema.DataFields[10], attachmentCounts),
            new DataColumn(schema.DataFields[11], attachments)
        };

        // Write to Parquet file
        using FileStream fileStream = File.Create(filePath);
        using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, fileStream);
        
        // Create a single row group
        using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
        
        foreach (DataColumn column in dataColumns)
        {
            await groupWriter.WriteColumnAsync(column);
        }
    }

    private async Task CreateEmptyParquetFile(string filePath)
    {
        ParquetSchema schema = new ParquetSchema(
            new DataField<string>("SourceApplication"),
            new DataField<string>("FromName"),
            new DataField<string>("ToName"),
            new DataField<DateTimeOffset>("TimestampUtc"),
            new DataField<string>("Body")
        );

        using FileStream fileStream = File.Create(filePath);
        using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, fileStream);
        // Empty file with just schema
    }
}
