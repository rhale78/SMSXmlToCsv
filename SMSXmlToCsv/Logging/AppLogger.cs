using System.Text.RegularExpressions;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace SMSXmlToCsv.Logging
{
    /// <summary>
    /// Application logger with PII masking and structured logging
    /// </summary>
    public static class AppLogger
    {
        private static ILogger? _logger;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the logger with file and optional console output
        /// </summary>
        public static void Initialize(bool logToConsole = false, string? logFilePath = null)
        {
            if (_isInitialized)
            {
                return;
            }

            logFilePath ??= Path.Combine(AppContext.BaseDirectory, "Logs", "sms-converter-.log");

            LoggerConfiguration config = new LoggerConfiguration()
                .Enrich.WithPiiMasking()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                );

            if (logToConsole)
            {
                config.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                );
            }

            _logger = config.CreateLogger();
            _isInitialized = true;

            Information("SMS Converter application started");
            Information($"Log file: {logFilePath}");
        }

        public static void Debug(string message) => _logger?.Debug(message);
        public static void Information(string message) => _logger?.Information(message);
        public static void Warning(string message) => _logger?.Warning(message);
        public static void Error(string message) => _logger?.Error(message);
        public static void Error(Exception ex, string message) => _logger?.Error(ex, message);
        public static void Fatal(string message) => _logger?.Fatal(message);
        public static void Fatal(Exception ex, string message) => _logger?.Fatal(ex, message);

        /// <summary>
        /// Log configuration settings (with PII masking applied automatically)
        /// </summary>
        public static void LogConfiguration(string settingName, object value)
        {
            Information($"Config: {settingName} = {value}");
        }

        /// <summary>
        /// Log operation progress
        /// </summary>
        public static void LogProgress(string operation, int current, int total)
        {
            Debug($"Progress: {operation} - {current}/{total} ({(double)current / total * 100:F1}%)");
        }

        /// <summary>
        /// Close and flush the logger
        /// </summary>
        public static void Close()
        {
            if (_isInitialized)
            {
                Information("SMS Converter application stopping");
                (_logger as IDisposable)?.Dispose();
                _isInitialized = false;
            }
        }
    }

    /// <summary>
    /// Enricher to mask PII in log messages
    /// </summary>
    public class PiiMaskingEnricher : ILogEventEnricher
    {
        // Regex patterns for PII detection
        private static readonly Regex PhoneRegex = new Regex(@"\+?\d{10,15}", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.MessageTemplate.Text.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                logEvent.MessageTemplate.Text.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                // Message contains potential PII keywords - apply masking to properties
                List<LogEventProperty> newProperties = new List<LogEventProperty>();

                foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties)
                {
                    if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
                    {
                        string maskedValue = MaskPii(stringValue);
                        newProperties.Add(new LogEventProperty(property.Key, new ScalarValue(maskedValue)));
                    }
                    else
                    {
                        newProperties.Add(new LogEventProperty(property.Key, property.Value));
                    }
                }
            }
        }

        private static string MaskPii(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Mask phone numbers (show last 4 digits)
            string masked = PhoneRegex.Replace(input, match =>
            {
                string digits = new string(match.Value.Where(char.IsDigit).ToArray());
                return digits.Length >= 4 ? $"+****{digits.Substring(digits.Length - 4)}" : "****";
            });

            // Mask emails (show first letter and domain)
            masked = EmailRegex.Replace(masked, match =>
            {
                string[] parts = match.Value.Split('@');
                return parts.Length == 2 && parts[0].Length > 0 ? $"{parts[0][0]}***@{parts[1]}" : "***@***";
            });

            return masked;
        }
    }

    /// <summary>
    /// Extension methods for logger configuration
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration WithPiiMasking(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration == null
                ? throw new ArgumentNullException(nameof(enrichmentConfiguration))
                : enrichmentConfiguration.With<PiiMaskingEnricher>();
        }
    }
}
