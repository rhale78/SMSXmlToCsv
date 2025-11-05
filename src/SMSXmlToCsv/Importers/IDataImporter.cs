using System.Collections.Generic;
using System.Threading.Tasks;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Importers;

/// <summary>
/// Defines the contract for data importers that convert various message formats
/// into the unified Message model.
/// </summary>
public interface IDataImporter
{
    /// <summary>
    /// Gets the name of the data source (e.g., "Android SMS Backup", "Facebook Messenger").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Determines whether this importer can handle the specified source path.
    /// </summary>
    /// <param name="sourcePath">The path to check for compatibility.</param>
    /// <returns>True if this importer can handle the source; otherwise, false.</returns>
    bool CanImport(string sourcePath);

    /// <summary>
    /// Asynchronously imports messages from a given file or directory path.
    /// </summary>
    /// <param name="sourcePath">The path to the data source file or directory.</param>
    /// <returns>A collection of unified Message objects.</returns>
    Task<IEnumerable<Message>> ImportAsync(string sourcePath);
}
