using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Core.Exporters;

/// <summary>
/// Abstract base class for data exporters that provides common functionality.
/// Handles directory creation, file path management, and validation.
/// </summary>
public abstract class BaseDataExporter : IDataExporter
{
    /// <inheritdoc/>
    public abstract string FileExtension { get; }

    /// <inheritdoc/>
    public async Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));
        }

        if (string.IsNullOrEmpty(baseFileName))
        {
            throw new ArgumentException("Base file name cannot be null or empty.", nameof(baseFileName));
        }

        // Ensure the output directory exists
        EnsureDirectoryExists(outputDirectory);

        // Build the full file path
        var filePath = BuildFilePath(outputDirectory, baseFileName);

        // Perform the actual export (implemented by derived classes)
        await ExportToFileAsync(messages, filePath);
    }

    /// <summary>
    /// Performs the actual export logic to write messages to a file.
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="messages">The messages to export.</param>
    /// <param name="filePath">The full path to the output file.</param>
    /// <returns>A task representing the asynchronous export operation.</returns>
    protected abstract Task ExportToFileAsync(IEnumerable<Message> messages, string filePath);

    /// <summary>
    /// Ensures that the specified directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">The directory path to ensure exists.</param>
    protected virtual void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Builds the full file path by combining the directory, base file name, and extension.
    /// </summary>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="baseFileName">The base file name (without extension).</param>
    /// <returns>The full file path.</returns>
    protected virtual string BuildFilePath(string outputDirectory, string baseFileName)
    {
        var fileName = $"{baseFileName}.{FileExtension}";
        return Path.Combine(outputDirectory, fileName);
    }
}
