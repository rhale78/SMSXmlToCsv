namespace SMSXmlToCsv.Models;

/// <summary>
/// Represents a media file attached to a message (MMS/MIME).
/// </summary>
public record MediaAttachment(
    string OriginalSourcePath,
    string MimeType
)
{
    /// <summary>
    /// Gets the file name from the original source path.
    /// </summary>
    public string FileName
    {
        get
        {
            if (string.IsNullOrEmpty(OriginalSourcePath))
            {
                return string.Empty;
            }

            // Use Path.GetFileName for robust cross-platform path handling
            try
            {
                return Path.GetFileName(OriginalSourcePath);
            }
            catch (ArgumentException)
            {
                // If path contains invalid characters, fall back to the original path
                return OriginalSourcePath;
            }
        }
    }
}
