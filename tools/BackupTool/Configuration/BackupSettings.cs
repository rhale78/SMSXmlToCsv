using System.Collections.Generic;

namespace BackupTool.Configuration;

public class BackupSettings
{
    public bool Enabled { get; set; } = true;
    public string BackupDirectory { get; set; } = string.Empty;
    public List<string> ExcludedDirectories { get; set; } = new();
    public List<string> ExcludedFiles { get; set; } = new();
}
