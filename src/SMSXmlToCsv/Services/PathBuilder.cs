using System;

namespace SMSXmlToCsv.Services;

public class PathBuilder
{
    public string BuildPath(string template, string? projectName = null)
    {
        DateTime now = DateTime.Now;
        string path = template
            .Replace("{date}", now.ToString("yyyy-MM-dd"))
            .Replace("{time}", now.ToString("HH-mm-ss"))
            .Replace("{datetime}", now.ToString("yyyy-MM-dd_HH-mm-ss"))
            .Replace("{project}", projectName ?? "Unknown");
        
        return path;
    }
}
