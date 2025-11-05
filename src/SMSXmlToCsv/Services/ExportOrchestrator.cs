using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SMSXmlToCsv.Exporters;
using SMSXmlToCsv.Models;
using Serilog;

namespace SMSXmlToCsv.Services;

/// <summary>
/// Orchestrates the export of messages to various formats.
/// Supports both all-in-one and per-contact export strategies.
/// </summary>
public class ExportOrchestrator
{
    private readonly ExportPathBuilder _pathBuilder;
    private readonly ILogger _logger;

    public ExportOrchestrator(ExportPathBuilder pathBuilder, ILogger logger)
    {
        _pathBuilder = pathBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Exports all messages to a single file using the specified exporter.
    /// </summary>
    public async Task ExportAllInOneAsync(
        IEnumerable<Message> messages,
        IDataExporter exporter,
        string outputDirectory,
        string baseFileName)
    {
        try
        {
            _logger.Information("Starting all-in-one export to {OutputDirectory}/{BaseFileName}.{Extension}",
                outputDirectory, baseFileName, exporter.FileExtension);

            _pathBuilder.EnsureDirectoryExists(outputDirectory);
            
            await exporter.ExportAsync(messages, outputDirectory, baseFileName);

            _logger.Information("All-in-one export completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during all-in-one export");
            throw;
        }
    }

    /// <summary>
    /// Exports messages grouped by contact, creating separate files for each contact.
    /// </summary>
    public async Task ExportPerContactAsync(
        IEnumerable<Message> messages,
        IDataExporter exporter,
        string baseOutputDirectory)
    {
        try
        {
            _logger.Information("Starting per-contact export to {BaseOutputDirectory}", baseOutputDirectory);

            string contactsDirectory = Path.Combine(baseOutputDirectory, "contacts");
            _pathBuilder.EnsureDirectoryExists(contactsDirectory);

            // Group messages by contact (using From contact as the key)
            Dictionary<string, List<Message>> messagesByContact = messages
                .GroupBy(m => m.From.Name)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Use path builder's timestamp for consistency
            string fileName = _pathBuilder.BuildPath("messages_{date}");

            foreach (KeyValuePair<string, List<Message>> contactGroup in messagesByContact)
            {
                string contactName = contactGroup.Key;
                List<Message> contactMessages = contactGroup.Value;

                // Get the contact from the first message
                Contact contact = contactMessages.First().From;

                string contactDirectory = _pathBuilder.BuildPath(
                    Path.Combine(contactsDirectory, "{contact_name}"),
                    contact);

                _pathBuilder.EnsureDirectoryExists(contactDirectory);
                
                _logger.Information("Exporting {MessageCount} messages for contact {ContactName}",
                    contactMessages.Count, contactName);

                await exporter.ExportAsync(contactMessages, contactDirectory, fileName);
            }

            _logger.Information("Per-contact export completed successfully for {ContactCount} contacts",
                messagesByContact.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during per-contact export");
            throw;
        }
    }
}
