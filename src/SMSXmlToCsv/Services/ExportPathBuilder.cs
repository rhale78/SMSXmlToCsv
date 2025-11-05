using System;
using System.IO;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Services;

/// <summary>
/// Builds output paths with support for placeholders.
/// </summary>
public class ExportPathBuilder
{
    private readonly DateTime _timestamp;

    public ExportPathBuilder()
    {
        _timestamp = DateTime.Now;
    }

    /// <summary>
    /// Builds a path from a template with placeholder substitution.
    /// Supported placeholders: {date}, {time}, {datetime}, {contact_name}, {project}
    /// </summary>
    /// <param name="template">The path template with placeholders.</param>
    /// <param name="contact">Optional contact for {contact_name} placeholder.</param>
    /// <param name="projectName">Optional project name for {project} placeholder.</param>
    /// <returns>The resolved path with placeholders replaced.</returns>
    public string BuildPath(string template, Contact? contact = null, string? projectName = null)
    {
        string path = template
            .Replace("{date}", _timestamp.ToString("yyyy-MM-dd"))
            .Replace("{time}", _timestamp.ToString("HH-mm-ss"))
            .Replace("{datetime}", _timestamp.ToString("yyyy-MM-dd_HH-mm-ss"))
            .Replace("{project}", projectName ?? "Unknown");

        if (contact != null)
        {
            string contactName = SanitizeFileName(contact.Name);
            path = path.Replace("{contact_name}", contactName);
        }

        return path;
    }

    /// <summary>
    /// Ensures the directory exists, creating it if necessary.
    /// </summary>
    public void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Removes invalid characters from a file name.
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        
        // Use string.Join with Split for better performance
        string[] parts = fileName.Split(invalidChars, StringSplitOptions.None);
        return string.Join("_", parts);
    }
}
