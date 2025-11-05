using Spectre.Console;
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
    private readonly bool _showProgress;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportOrchestrator"/> class.
    /// </summary>
    /// <param name="exporter">The exporter to use for generating output files.</param>
    /// <param name="showProgress">Whether to display progress using Spectre.Console.</param>
    public ExportOrchestrator(IDataExporter exporter, bool showProgress = true)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _pathBuilder = new PathBuilder();
        _showProgress = showProgress;
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

        if (_showProgress)
        {
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[green]Exporting {messagesList.Count} messages using {strategy} strategy[/]");
                    
                    switch (strategy)
                    {
                        case ExportStrategy.AllInOne:
                            await ExportAllInOneAsync(messagesList, baseOutputDirectory, fileNameTemplate, task);
                            break;

                        case ExportStrategy.PerContact:
                            await ExportPerContactAsync(messagesList, baseOutputDirectory, fileNameTemplate, task);
                            break;

                        default:
                            throw new ArgumentException($"Unknown export strategy: {strategy}", nameof(strategy));
                    }
                    
                    task.Value = 100;
                });
            
            AnsiConsole.MarkupLine($"[bold green]✓[/] Export completed to: [blue]{baseOutputDirectory}[/]");
        }
        else
        {
            switch (strategy)
            {
                case ExportStrategy.AllInOne:
                    await ExportAllInOneAsync(messagesList, baseOutputDirectory, fileNameTemplate, null);
                    break;

                case ExportStrategy.PerContact:
                    await ExportPerContactAsync(messagesList, baseOutputDirectory, fileNameTemplate, null);
                    break;

                default:
                    throw new ArgumentException($"Unknown export strategy: {strategy}", nameof(strategy));
            }
        }
    }

    /// <summary>
    /// Exports all messages to a single file.
    /// </summary>
    private async Task ExportAllInOneAsync(List<Message> messages, string baseOutputDirectory, string fileNameTemplate, ProgressTask? progressTask)
    {
        progressTask?.StartTask();
        
        if (_showProgress)
        {
            AnsiConsole.MarkupLine($"[yellow]→[/] Exporting all messages to single file...");
        }
        
        var fileName = _pathBuilder.BuildPath(fileNameTemplate);
        await _exporter.ExportAsync(messages, baseOutputDirectory, fileName);
        
        if (_showProgress)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Exported {messages.Count} messages to [blue]{fileName}.{_exporter.FileExtension}[/]");
        }
        
        progressTask?.Increment(100);
    }

    /// <summary>
    /// Exports messages in separate files per contact, organized in contact-specific folders.
    /// </summary>
    private async Task ExportPerContactAsync(List<Message> messages, string baseOutputDirectory, string fileNameTemplate, ProgressTask? progressTask)
    {
        // Group messages by contact
        var groupedMessages = messages
            .Where(m => m.Contact != null)
            .GroupBy(m => m.Contact!.Id)
            .ToList();

        var orphanedMessages = messages.Where(m => m.Contact == null).ToList();
        var totalGroups = groupedMessages.Count + (orphanedMessages.Any() ? 1 : 0);
        
        progressTask?.StartTask();
        var progressIncrement = totalGroups > 0 ? 100.0 / totalGroups : 100;

        // Create a "contacts" subdirectory
        var contactsDirectory = Path.Combine(baseOutputDirectory, "contacts");

        // Export each contact's messages to their own subdirectory
        for (int i = 0; i < groupedMessages.Count; i++)
        {
            var group = groupedMessages[i];
            var contact = group.First().Contact!;
            
            if (_showProgress)
            {
                AnsiConsole.MarkupLine($"[yellow]→[/] Exporting messages for [cyan]{contact.Name}[/] ({i + 1}/{groupedMessages.Count})...");
            }
            
            // Build contact-specific directory path
            var contactDirTemplate = Path.Combine(contactsDirectory, "{contact_name}");
            var contactDirectory = _pathBuilder.BuildPath(contactDirTemplate, contact);

            // Build file name with contact context
            var fileName = _pathBuilder.BuildPath(fileNameTemplate, contact);

            // Export the contact's messages
            await _exporter.ExportAsync(group, contactDirectory, fileName);
            
            if (_showProgress)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Exported {group.Count()} messages for [cyan]{contact.Name}[/]");
            }
            
            progressTask?.Increment(progressIncrement);
        }

        // Handle messages without a contact (if any)
        if (orphanedMessages.Any())
        {
            if (_showProgress)
            {
                AnsiConsole.MarkupLine($"[yellow]→[/] Exporting orphaned messages...");
            }
            
            var unknownDirectory = Path.Combine(contactsDirectory, "Unknown");
            var fileName = _pathBuilder.BuildPath(fileNameTemplate);
            await _exporter.ExportAsync(orphanedMessages, unknownDirectory, fileName);
            
            if (_showProgress)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Exported {orphanedMessages.Count} orphaned messages");
            }
            
            progressTask?.Increment(progressIncrement);
        }
    }
}
