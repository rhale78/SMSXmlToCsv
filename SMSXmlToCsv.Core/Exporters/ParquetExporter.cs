using Parquet;
using Parquet.Data;
using Parquet.Schema;
using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Core.Exporters;

/// <summary>
/// Apache Parquet format exporter that writes messages in columnar binary format.
/// </summary>
public class ParquetExporter : BaseDataExporter
{
    /// <inheritdoc/>
    public override string FileExtension => "parquet";

    /// <inheritdoc/>
    protected override async Task ExportToFileAsync(IEnumerable<Message> messages, string filePath)
    {
        var messagesList = messages.ToList();
        
        if (!messagesList.Any())
        {
            // Create an empty file for consistency
            await using var emptyStream = File.Create(filePath);
            return;
        }

        // Define Parquet schema
        var schema = new ParquetSchema(
            new DataField<string>("Id"),
            new DataField<DateTime>("Timestamp"),
            new DataField<string>("Type"),
            new DataField<bool>("IsSent"),
            new DataField<string>("PhoneNumber"),
            new DataField<string>("ContactName"),
            new DataField<string>("ContactId"),
            new DataField<string>("Body")
        );

        // Extract data columns
        var ids = messagesList.Select(m => m.Id).ToArray();
        var timestamps = messagesList.Select(m => m.Timestamp).ToArray();
        var types = messagesList.Select(m => m.Type).ToArray();
        var isSents = messagesList.Select(m => m.IsSent).ToArray();
        var phoneNumbers = messagesList.Select(m => m.PhoneNumber).ToArray();
        var contactNames = messagesList.Select(m => m.Contact?.Name ?? "Unknown").ToArray();
        var contactIds = messagesList.Select(m => m.Contact?.Id ?? string.Empty).ToArray();
        var bodies = messagesList.Select(m => m.Body).ToArray();

        // Write to Parquet file
        await using var stream = File.Create(filePath);
        using var writer = await ParquetWriter.CreateAsync(schema, stream);
        
        // Create a row group
        using var rowGroup = writer.CreateRowGroup();
        
        // Write columns
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[0], ids));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[1], timestamps));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[2], types));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[3], isSents));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[4], phoneNumbers));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[5], contactNames));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[6], contactIds));
        await rowGroup.WriteColumnAsync(new DataColumn(schema.DataFields[7], bodies));
    }
}
