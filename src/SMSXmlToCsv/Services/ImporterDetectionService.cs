using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SMSXmlToCsv.Importers;

namespace SMSXmlToCsv.Services;

/// <summary>
/// Service that automatically detects which importers can handle a given directory path.
/// Scans the directory and returns all compatible importers.
/// </summary>
public class ImporterDetectionService
{
    private readonly List<IDataImporter> _availableImporters;

    public ImporterDetectionService()
    {
        // Initialize all available importers
        _availableImporters = new List<IDataImporter>
        {
            new SmsXmlImporter(),
            new GoogleTakeoutImporter(),
            new FacebookMessageImporter(),
            new InstagramMessageImporter(),
            new GoogleMailImporter(),
            new SignalBackupImporter()
        };
    }

    /// <summary>
    /// Scans the specified directory path and returns all importers that can handle files/folders within it.
    /// </summary>
    /// <param name="directoryPath">The directory path to scan.</param>
    /// <returns>A collection of compatible importers with their detected source paths.</returns>
    public IEnumerable<(IDataImporter Importer, string SourcePath)> DetectImporters(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        List<(IDataImporter, string)> detectedImporters = new List<(IDataImporter, string)>();
        HashSet<Type> detectedTypes = new HashSet<Type>();

        // Check if the directory itself is a valid source for any importer
        foreach (IDataImporter importer in _availableImporters)
        {
            if (importer.CanImport(directoryPath))
            {
                detectedImporters.Add((importer, directoryPath));
                detectedTypes.Add(importer.GetType());
            }
        }

        // Also check all files in the directory
        foreach (string filePath in Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly))
        {
            foreach (IDataImporter importer in _availableImporters)
            {
                if (!detectedTypes.Contains(importer.GetType()) && importer.CanImport(filePath))
                {
                    detectedImporters.Add((importer, filePath));
                    detectedTypes.Add(importer.GetType());
                }
            }
        }

        // Check subdirectories (one level deep only to avoid deep recursion)
        foreach (string subdirectory in Directory.GetDirectories(directoryPath))
        {
            foreach (IDataImporter importer in _availableImporters)
            {
                if (!detectedTypes.Contains(importer.GetType()) && importer.CanImport(subdirectory))
                {
                    detectedImporters.Add((importer, subdirectory));
                    detectedTypes.Add(importer.GetType());
                }
            }
        }

        return detectedImporters;
    }

    /// <summary>
    /// Gets a human-readable summary of all detected importers for a directory.
    /// </summary>
    /// <param name="directoryPath">The directory path to scan.</param>
    /// <returns>A formatted string describing all detected importers.</returns>
    public string GetDetectionSummary(string directoryPath)
    {
        IEnumerable<(IDataImporter Importer, string SourcePath)> detected = DetectImporters(directoryPath);
        
        if (!detected.Any())
        {
            return "No compatible importers detected in this directory.";
        }

        return string.Join("\n", detected.Select(d => 
            $"✓ {d.Importer.SourceName} - {Path.GetFileName(d.SourcePath)}"));
    }
}
