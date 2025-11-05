using System;
using System.IO;
using System.Linq;
using SMSXmlToCsv.Configuration;
using Serilog;

namespace SMSXmlToCsv.Services;

public class SolutionBackupOrchestrator
{
    private readonly BackupSettings _settings;
    private readonly PathBuilder _pathBuilder;
    private readonly ILogger _logger;

    public SolutionBackupOrchestrator(BackupSettings settings, PathBuilder pathBuilder, ILogger logger)
    {
        _settings = settings;
        _pathBuilder = pathBuilder;
        _logger = logger;
    }

    public void BackupSolution(string solutionDirectory)
    {
        if (!_settings.Enabled)
        {
            _logger.Information("Backup is disabled in configuration");
            return;
        }

        try
        {
            _logger.Information("Starting solution backup from {SolutionDirectory}", solutionDirectory);

            // Find all project directories
            string[] projectFiles = Directory.GetFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories);

            if (projectFiles.Length == 0)
            {
                _logger.Warning("No project files found in solution directory");
                return;
            }

            foreach (string projectFile in projectFiles)
            {
                string projectDirectory = Path.GetDirectoryName(projectFile) ?? string.Empty;
                string projectName = Path.GetFileNameWithoutExtension(projectFile);

                ProjectBackupService projectBackupService = new ProjectBackupService(_settings, _pathBuilder, _logger);
                projectBackupService.BackupProject(projectDirectory, projectName);
            }

            _logger.Information("Solution backup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error backing up solution");
        }
    }
}
