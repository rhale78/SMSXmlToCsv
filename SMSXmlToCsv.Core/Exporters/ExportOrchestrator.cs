using SMSXmlToCsv.Core.Models;
using SMSXmlToCsv.Core.Utilities;

namespace SMSXmlToCsv.Core.Exporters;

/// <summary>
/// Orchestrates the export of messages using different strategies and exporters.
/// Handles routing messages to appropriate directories based on the chosen export strategy.
/// </summary>
public class ExportOrchestrator
{
    private readonly IDataExporter _exporter;
    private readonly PathBuilder _pathBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportOrchestrator"/> class.
    /// </summary>
    /// <param name="exporter">The exporter to use for generating output files.</param>
    public ExportOrchestrator(IDataExporter exporter)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _pathBuilder = new PathBuilder();
    }

    /// <summary>
    /// Exports messages using the specified strategy.
    /// </summary>
    /// <param name="messages">The messages to export.</param>
    /// <param name="baseOutputDirectory">The base output directory.</param>
    /// <param name="strategy">The export strategy to use.</param>
    /// <param name="fileNameTemplate">Template for the output file name (default: "messages_{date}").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExportAsync(
        IEnumerable<Message> messages,
        string baseOutputDirectory,
        ExportStrategy strategy,
        string fileNameTemplate = "messages_{date}")
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (string.IsNullOrEmpty(baseOutputDirectory))
        {
            throw new ArgumentException("Base output directory cannot be null or empty.", nameof(baseOutputDirectory));
        }

        var messagesList = messages.ToList();

        switch (strategy)
        {
            case ExportStrategy.AllInOne:
                await ExportAllInOneAsync(messagesList, baseOutputDirectory, fileNameTemplate);
                break;

            case ExportStrategy.PerContact:
                await ExportPerContactAsync(messagesList, baseOutputDirectory, fileNameTemplate);
                break;

            default:
                throw new ArgumentException($"Unknown export strategy: {strategy}", nameof(strategy));
        }
    }

    /// <summary>
    /// Exports all messages to a single file.
    /// </summary>
    private async Task ExportAllInOneAsync(List<Message> messages, string baseOutputDirectory, string fileNameTemplate)
    {
        var fileName = _pathBuilder.BuildPath(fileNameTemplate);
        await _exporter.ExportAsync(messages, baseOutputDirectory, fileName);
    }

    /// <summary>
    /// Exports messages in separate files per contact, organized in contact-specific folders.
    /// </summary>
    private async Task ExportPerContactAsync(List<Message> messages, string baseOutputDirectory, string fileNameTemplate)
    {
        // Group messages by contact
        var groupedMessages = messages
            .Where(m => m.Contact != null)
            .GroupBy(m => m.Contact!.Id)
            .ToList();

        // Create a "contacts" subdirectory
        var contactsDirectory = Path.Combine(baseOutputDirectory, "contacts");

        // Export each contact's messages to their own subdirectory
        foreach (var group in groupedMessages)
        {
            var contact = group.First().Contact!;
            
            // Build contact-specific directory path
            var contactDirTemplate = Path.Combine(contactsDirectory, "{contact_name}");
            var contactDirectory = _pathBuilder.BuildPath(contactDirTemplate, contact);

            // Build file name with contact context
            var fileName = _pathBuilder.BuildPath(fileNameTemplate, contact);

            // Export the contact's messages
            await _exporter.ExportAsync(group, contactDirectory, fileName);
        }

        // Handle messages without a contact (if any)
        var orphanedMessages = messages.Where(m => m.Contact == null).ToList();
        if (orphanedMessages.Any())
        {
            var unknownDirectory = Path.Combine(contactsDirectory, "Unknown");
            var fileName = _pathBuilder.BuildPath(fileNameTemplate);
            await _exporter.ExportAsync(orphanedMessages, unknownDirectory, fileName);
        }
    }
}
