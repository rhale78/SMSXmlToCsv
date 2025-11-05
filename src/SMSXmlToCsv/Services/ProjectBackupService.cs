using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SMSXmlToCsv.Configuration;
using Serilog;

namespace SMSXmlToCsv.Services;

public class ProjectBackupService
{
    private readonly BackupSettings _settings;
    private readonly PathBuilder _pathBuilder;
    private readonly ILogger _logger;

    public ProjectBackupService(BackupSettings settings, PathBuilder pathBuilder, ILogger logger)
    {
        _settings = settings;
        _pathBuilder = pathBuilder;
        _logger = logger;
    }

    public void BackupProject(string projectDirectory, string projectName)
    {
        if (!_settings.Enabled)
        {
            _logger.Information("Backup is disabled in configuration");
            return;
        }

        try
        {
            string backupPath = _pathBuilder.BuildPath(_settings.BackupDirectory, projectName);
            
            // Make path relative to project directory if it starts with ../
            if (backupPath.StartsWith("../") || backupPath.StartsWith("..\\"))
            {
                backupPath = Path.GetFullPath(Path.Combine(projectDirectory, backupPath));
            }

            _logger.Information("Starting backup of project {ProjectName} to {BackupPath}", projectName, backupPath);

            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            CopyDirectory(projectDirectory, backupPath, projectName);

            _logger.Information("Backup completed successfully for {ProjectName}", projectName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error backing up project {ProjectName}", projectName);
        }
    }

    private void CopyDirectory(string sourceDir, string destDir, string projectName)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        
        foreach (FileInfo file in dir.GetFiles())
        {
            if (ShouldExcludeFile(file.Name))
            {
                continue;
            }

            string targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            if (ShouldExcludeDirectory(subDir.Name))
            {
                continue;
            }

            string newDestinationDir = Path.Combine(destDir, subDir.Name);
            Directory.CreateDirectory(newDestinationDir);
            CopyDirectory(subDir.FullName, newDestinationDir, projectName);
        }
    }

    private bool ShouldExcludeDirectory(string directoryName)
    {
        return _settings.ExcludedDirectories.Any(excluded => 
            directoryName.Equals(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldExcludeFile(string fileName)
    {
        foreach (string pattern in _settings.ExcludedFiles)
        {
            if (pattern.Contains("*"))
            {
                string regexPattern = "^" + pattern.Replace(".", "\\.").Replace("*", ".*") + "$";
                if (System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            else if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
