using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Core.Exporters;

/// <summary>
/// Defines the contract for data exporters that convert messages to specific file formats.
/// </summary>
public interface IDataExporter
{
    /// <summary>
    /// Gets the file extension for this exporter (e.g., "csv", "jsonl").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Asynchronously exports a collection of messages to the specified path.
    /// </summary>
    /// <param name="messages">The messages to export.</param>
    /// <param name="outputDirectory">The directory to save the file in.</param>
    /// <param name="baseFileName">The base name for the output file (without extension).</param>
    /// <returns>A task representing the asynchronous export operation.</returns>
    Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName);
}
