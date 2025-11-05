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
    /// Asynchronously imports messages from a given file or directory path.
    /// </summary>
    /// <param name="sourcePath">The path to the data source file or directory.</param>
    /// <returns>A collection of unified Message objects.</returns>
    Task<IEnumerable<Message>> ImportAsync(string sourcePath);
}
