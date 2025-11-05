using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using BackupTool.Configuration;
using BackupTool.Services;

namespace BackupTool;

public class Program
{
    public static int Main(string[] args)
    {
        // Load configuration from the main project if available, otherwise use defaults
        string configPath = "appsettings.json";
        
        // If run from bin directory, look for appsettings in various locations
        if (!File.Exists(configPath))
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "../../appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "../../../appsettings.json")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    configPath = path;
                    break;
                }
            }
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: true, reloadOnChange: false)
            .Build();

        // Configure Serilog - minimal console output for build tool
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            BackupSettings backupSettings = new BackupSettings();
            IConfigurationSection backupSection = configuration.GetSection("BackupSettings");
            
            if (backupSection.Exists())
            {
                backupSection.Bind(backupSettings);
            }
            else
            {
                // Use defaults
                backupSettings.Enabled = true;
                backupSettings.BackupDirectory = "../Backups/{date}/{time}";
                backupSettings.ExcludedDirectories = new System.Collections.Generic.List<string> 
                { 
                    ".git", ".github", "copilot", "bin", "obj", ".vs", "packages", "node_modules", "Backups" 
                };
                backupSettings.ExcludedFiles = new System.Collections.Generic.List<string> 
                { 
                    "*.tmp", "*.cache", "*.log" 
                };
            }

            if (!backupSettings.Enabled)
            {
                Log.Information("Backup is disabled");
                return 0;
            }

            // Determine solution directory
            string solutionDirectory = args.Length > 0 && Directory.Exists(args[0])
                ? args[0]
                : Directory.GetCurrentDirectory();

            Log.Information("Backing up solution from {SolutionDirectory}", solutionDirectory);

            PathBuilder pathBuilder = new PathBuilder();
            SolutionBackupOrchestrator orchestrator = new SolutionBackupOrchestrator(
                backupSettings,
                pathBuilder,
                Log.Logger);

            orchestrator.BackupSolution(solutionDirectory);

            Log.Information("Backup completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Backup failed");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
