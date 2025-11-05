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

            int lastSlash = OriginalSourcePath.LastIndexOf('/');
            int lastBackslash = OriginalSourcePath.LastIndexOf('\\');
            int lastSeparator = Math.Max(lastSlash, lastBackslash);

            return lastSeparator >= 0 
                ? OriginalSourcePath.Substring(lastSeparator + 1) 
                : OriginalSourcePath;
        }
    }
}
