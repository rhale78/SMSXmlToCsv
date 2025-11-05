using System.Collections.Generic;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Exporters;

/// <summary>
/// Defines the contract for data exporters that write messages to various formats.
/// </summary>
public interface IDataExporter
{
    /// <summary>
    /// Gets the file extension for this exporter (e.g., "csv", "jsonl", "html").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Asynchronously exports a collection of messages to the specified path.
    /// </summary>
    /// <param name="messages">The messages to export.</param>
    /// <param name="outputDirectory">The directory to save the file in.</param>
    /// <param name="baseFileName">The base name for the output file (without extension).</param>
    /// <returns>A task that represents the asynchronous export operation.</returns>
    Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName);
}
