using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Core.Utilities;

/// <summary>
/// Utility class for building file paths with placeholder replacement.
/// Supports dynamic path generation based on templates with placeholders like {date}, {time}, {contact_name}, etc.
/// </summary>
public class PathBuilder
{
    /// <summary>
    /// Builds a path from a template by replacing placeholders with actual values.
    /// </summary>
    /// <param name="template">The path template containing placeholders.</param>
    /// <param name="contact">Optional contact information for contact-specific placeholders.</param>
    /// <returns>The resolved path with all placeholders replaced.</returns>
    public string BuildPath(string template, Contact? contact = null)
    {
        return BuildPath(template, DateTime.Now, contact);
    }

    /// <summary>
    /// Builds a path from a template with a specific DateTime value.
    /// </summary>
    /// <param name="template">The path template containing placeholders.</param>
    /// <param name="dateTime">The DateTime to use for date/time placeholders.</param>
    /// <param name="contact">Optional contact information for contact-specific placeholders.</param>
    /// <returns>The resolved path with all placeholders replaced.</returns>
    public string BuildPath(string template, DateTime dateTime, Contact? contact = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            throw new ArgumentException("Template cannot be null or empty.", nameof(template));
        }

        var path = ReplaceDateTimePlaceholders(template, dateTime);
        path = ReplaceContactPlaceholders(path, contact);

        return path;
    }

    /// <summary>
    /// Replaces date and time placeholders in the template with the provided DateTime.
    /// </summary>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="dateTime">The DateTime to use for replacement.</param>
    /// <returns>The template with date/time placeholders replaced.</returns>
    private string ReplaceDateTimePlaceholders(string template, DateTime dateTime)
    {
        var path = template;
        path = path.Replace("{date}", dateTime.ToString("yyyy-MM-dd"));
        path = path.Replace("{time}", dateTime.ToString("HH-mm-ss"));
        path = path.Replace("{datetime}", dateTime.ToString("yyyy-MM-dd_HH-mm-ss"));
        path = path.Replace("{year}", dateTime.ToString("yyyy"));
        path = path.Replace("{month}", dateTime.ToString("MM"));
        path = path.Replace("{day}", dateTime.ToString("dd"));
        return path;
    }

    /// <summary>
    /// Replaces contact-specific placeholders in the template.
    /// </summary>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="contact">The contact information to use for replacement.</param>
    /// <returns>The template with contact placeholders replaced.</returns>
    private string ReplaceContactPlaceholders(string template, Contact? contact)
    {
        if (contact == null)
        {
            return template;
        }

        var path = template;
        path = path.Replace("{contact_name}", SanitizeFileName(contact.Name));
        path = path.Replace("{contact_id}", contact.Id);
        path = path.Replace("{phone_number}", SanitizeFileName(contact.PhoneNumber));
        return path;
    }

    /// <summary>
    /// Sanitizes a string to be safe for use as a file or directory name.
    /// Removes or replaces invalid characters.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <returns>A sanitized file name safe for the file system.</returns>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return "Unknown";
        }

        // Replace invalid characters with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        // Explicitly handle additional characters that may be valid on some platforms (e.g., Linux)
        // but invalid on others (e.g., Windows). This ensures cross-platform compatibility.
        fileName = fileName.Replace(':', '_');
        fileName = fileName.Replace('*', '_');

        var sanitized = fileName.Trim();
        
        // If after trimming the name is empty, return "Unknown"
        return string.IsNullOrEmpty(sanitized) ? "Unknown" : sanitized;
    }
}
