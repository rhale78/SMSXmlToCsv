using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace SMSXmlToCsv.Services.ErrorHandling;

/// <summary>
/// Collects and manages errors during processing with continue-on-error support
/// </summary>
public class ErrorCollector
{
    private readonly List<ErrorEntry> _errors = new List<ErrorEntry>();
    private readonly bool _continueOnError;
    private readonly bool _saveErrorReport;
    private readonly string? _errorReportPath;

    public ErrorCollector(bool continueOnError = false, bool saveErrorReport = false, string? errorReportPath = null)
    {
        _continueOnError = continueOnError;
        _saveErrorReport = saveErrorReport;
        _errorReportPath = errorReportPath;
    }

    public bool ContinueOnError => _continueOnError;
    public int ErrorCount => _errors.Count;
    public IReadOnlyList<ErrorEntry> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Add an error to the collection
    /// </summary>
    public void AddError(Exception exception, string operation, string? context = null)
    {
        ErrorEntry entry = new ErrorEntry
        {
            Timestamp = DateTime.UtcNow,
            Exception = exception,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            Operation = operation,
            Context = context,
            StackTrace = exception.StackTrace
        };

        _errors.Add(entry);
        Log.Warning(exception, "Error during {Operation}: {Message}. Context: {Context}", 
            operation, exception.Message, context ?? "N/A");
    }

    /// <summary>
    /// Execute an operation with error handling
    /// </summary>
    public bool TryExecute(Action operation, string operationName, string? context = null)
    {
        try
        {
            operation();
            return true;
        }
        catch (Exception ex)
        {
            AddError(ex, operationName, context);

            if (!_continueOnError)
            {
                throw;
            }

            return false;
        }
    }

    /// <summary>
    /// Execute an async operation with error handling
    /// </summary>
    public async Task<bool> TryExecuteAsync(Func<Task> operation, string operationName, string? context = null)
    {
        try
        {
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            AddError(ex, operationName, context);

            if (!_continueOnError)
            {
                throw;
            }

            return false;
        }
    }

    /// <summary>
    /// Display error summary
    /// </summary>
    public void DisplaySummary()
    {
        if (_errors.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine($"  ERROR SUMMARY: {_errors.Count} Error(s) Occurred");
        Console.WriteLine("═══════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        Dictionary<string, int> errorsByType = _errors
            .GroupBy(e => e.ExceptionType)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (KeyValuePair<string, int> kvp in errorsByType)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} occurrence(s)");
        }

        Console.WriteLine();
        Console.WriteLine("Recent errors:");

        foreach (ErrorEntry error in _errors.TakeLast(5))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  • {error.Operation}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    {error.Message}");
            if (!string.IsNullOrEmpty(error.Context))
            {
                Console.WriteLine($"    Context: {error.Context}");
            }
            Console.ResetColor();
        }

        if (_errors.Count > 5)
        {
            Console.WriteLine($"  ... and {_errors.Count - 5} more error(s)");
        }

        Console.WriteLine();

        if (_saveErrorReport && _errorReportPath != null)
        {
            try
            {
                SaveErrorReport();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Error report saved to: {_errorReportPath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to save error report: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// Save error report to file
    /// </summary>
    public void SaveErrorReport()
    {
        if (_errorReportPath == null)
        {
            throw new InvalidOperationException("Error report path not specified");
        }

        string? directory = Path.GetDirectoryName(_errorReportPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (StreamWriter writer = new StreamWriter(_errorReportPath))
        {
            writer.WriteLine("SMSXmlToCsv Error Report");
            writer.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            writer.WriteLine($"Total Errors: {_errors.Count}");
            writer.WriteLine();
            writer.WriteLine(new string('=', 80));
            writer.WriteLine();

            foreach (ErrorEntry error in _errors)
            {
                writer.WriteLine($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss} UTC] {error.ExceptionType}");
                writer.WriteLine($"Operation: {error.Operation}");
                if (!string.IsNullOrEmpty(error.Context))
                {
                    writer.WriteLine($"Context: {error.Context}");
                }
                writer.WriteLine($"Message: {error.Message}");
                if (!string.IsNullOrEmpty(error.StackTrace))
                {
                    writer.WriteLine("Stack Trace:");
                    writer.WriteLine(error.StackTrace);
                }
                writer.WriteLine();
                writer.WriteLine(new string('-', 80));
                writer.WriteLine();
            }
        }

        Log.Information("Error report saved to {Path}", _errorReportPath);
    }

    /// <summary>
    /// Clear all collected errors
    /// </summary>
    public void Clear()
    {
        _errors.Clear();
    }
}

/// <summary>
/// Represents a single error entry
/// </summary>
public class ErrorEntry
{
    public DateTime Timestamp { get; set; }
    public Exception? Exception { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? Context { get; set; }
    public string? StackTrace { get; set; }
}
