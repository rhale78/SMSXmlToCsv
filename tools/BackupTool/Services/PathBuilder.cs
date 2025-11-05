using System;

namespace BackupTool.Services;

public class PathBuilder
{
    private readonly DateTime _timestamp;

    public PathBuilder()
    {
        _timestamp = DateTime.Now;
    }

    public string BuildPath(string template, string? projectName = null)
    {
        string path = template
            .Replace("{date}", _timestamp.ToString("yyyy-MM-dd"))
            .Replace("{time}", _timestamp.ToString("HH-mm-ss"))
            .Replace("{datetime}", _timestamp.ToString("yyyy-MM-dd_HH-mm-ss"))
            .Replace("{project}", projectName ?? "Unknown");
        
        return path;
    }
}
