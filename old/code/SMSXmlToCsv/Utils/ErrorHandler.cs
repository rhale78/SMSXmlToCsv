using SMSXmlToCsv.Logging;

namespace SMSXmlToCsv.Utils
{
    /// <summary>
    /// Centralized error handling with user recovery options
    /// </summary>
    public static class ErrorHandler
    {
        public enum ErrorAction
        {
            Continue,
            Retry,
            Stop,
            IgnoreSimilar
        }

        private static readonly HashSet<string> _ignoredErrorTypes = new HashSet<string>();
        private static bool _continueOnAllErrors = false;

        /// <summary>
        /// Handle an error with user interaction (or auto-continue if configured)
        /// </summary>
        public static ErrorAction HandleError(
            Exception ex,
            string operation,
            string? context = null,
            bool allowRetry = true,
            bool allowIgnoreSimilar = true,
            bool autoContinue = false)
        {
            string errorType = ex.GetType().Name;

            // Check if we should auto-ignore this error type
            if (_continueOnAllErrors || _ignoredErrorTypes.Contains(errorType))
            {
                AppLogger.Warning($"Auto-continuing after error in {operation}: {ex.Message}");
                return ErrorAction.Continue;
            }

            // Auto-continue if configured
            if (autoContinue)
            {
                AppLogger.Warning($"Auto-continuing (config): {operation} - {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"??  Warning: {operation} failed, continuing...");
                Console.ResetColor();
                return ErrorAction.Continue;
            }

            // Log the full error
            AppLogger.Error(ex, $"Error during {operation}");

            // Show interactive error dialog
            return ShowErrorDialog(ex, operation, context, allowRetry, allowIgnoreSimilar);
        }

        private static ErrorAction ShowErrorDialog(
            Exception ex,
            string operation,
            string? context,
            bool allowRetry,
            bool allowIgnoreSimilar)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("??????????????????????????????????????????????????????????????????");
            Console.WriteLine("?                    ??  ERROR OCCURRED                          ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????????");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Operation: {operation}");
            if (!string.IsNullOrEmpty(context))
            {
                Console.WriteLine($"Context:   {context}");
            }
            Console.WriteLine($"Error:     {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();

            // Show stack trace for detailed errors
            if (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Details: {ex.GetType().Name}");
                Console.ResetColor();
            }

            Console.WriteLine("Options:");
            Console.WriteLine("  [C] Continue (skip this item)");
            if (allowRetry)
            {
                Console.WriteLine("  [R] Retry");
            }
            Console.WriteLine("  [S] Stop processing");
            if (allowIgnoreSimilar)
            {
                Console.WriteLine("  [I] Ignore similar errors (continue for all)");
            }
            Console.WriteLine();
            Console.Write("Your choice [C]: ");

            string? input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(input) || input == "C")
            {
                return ErrorAction.Continue;
            }
            else if (input == "R" && allowRetry)
            {
                return ErrorAction.Retry;
            }
            else if (input == "S")
            {
                return ErrorAction.Stop;
            }
            else if (input == "I" && allowIgnoreSimilar)
            {
                _ignoredErrorTypes.Add(ex.GetType().Name);
                AppLogger.Information($"Now ignoring all {ex.GetType().Name} errors");
                return ErrorAction.Continue;
            }
            else
            {
                return ErrorAction.Continue;
            }
        }

        /// <summary>
        /// Set global continue-on-error mode
        /// </summary>
        public static void SetContinueOnAllErrors(bool continueOnAll)
        {
            _continueOnAllErrors = continueOnAll;
            if (continueOnAll)
            {
                AppLogger.Information("Continue-on-error mode enabled globally");
            }
        }

        /// <summary>
        /// Execute an operation with error handling
        /// </summary>
        public static async Task<bool> ExecuteWithErrorHandling(
            Func<Task> operation,
            string operationName,
            string? context = null,
            bool allowRetry = true,
            bool autoContinue = false,
            int maxRetries = 3)
        {
            int retryCount = 0;

            while (true)
            {
                try
                {
                    await operation();
                    return true; // Success
                }
                catch (Exception ex)
                {
                    ErrorAction action = HandleError(
                        ex,
                        operationName,
                        context,
                        allowRetry && retryCount < maxRetries,
                        allowIgnoreSimilar: true,
                        autoContinue);

                    switch (action)
                    {
                        case ErrorAction.Continue:
                            return false; // Continue but mark as failed

                        case ErrorAction.Retry:
                            retryCount++;
                            AppLogger.Information($"Retrying {operationName} (attempt {retryCount + 1})");
                            continue; // Loop again

                        case ErrorAction.Stop:
                            throw new OperationCanceledException("User requested stop");

                        case ErrorAction.IgnoreSimilar:
                            return false; // Continue but mark as failed

                        default:
                            return false;
                    }
                }
            }
        }

        /// <summary>
        /// Synchronous version for non-async operations
        /// </summary>
        public static bool ExecuteWithErrorHandling(
            Action operation,
            string operationName,
            string? context = null,
            bool allowRetry = true,
            bool autoContinue = false,
            int maxRetries = 3)
        {
            int retryCount = 0;

            while (true)
            {
                try
                {
                    operation();
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorAction action = HandleError(
                        ex,
                        operationName,
                        context,
                        allowRetry && retryCount < maxRetries,
                        allowIgnoreSimilar: true,
                        autoContinue);

                    switch (action)
                    {
                        case ErrorAction.Continue:
                            return false;

                        case ErrorAction.Retry:
                            retryCount++;
                            AppLogger.Information($"Retrying {operationName} (attempt {retryCount + 1})");
                            continue;

                        case ErrorAction.Stop:
                            throw new OperationCanceledException("User requested stop");

                        case ErrorAction.IgnoreSimilar:
                            return false;

                        default:
                            return false;
                    }
                }
            }
        }

        /// <summary>
        /// Reset ignored error types (for testing or new operations)
        /// </summary>
        public static void ResetIgnoredErrors()
        {
            _ignoredErrorTypes.Clear();
            _continueOnAllErrors = false;
        }
    }
}
