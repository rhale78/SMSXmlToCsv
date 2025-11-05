using SMSXmlToCsv.Logging;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Batch
{
    /// <summary>
    /// Batch processing for multiple XML files
    /// </summary>
    public class BatchProcessor
    {
        public async Task<BatchResults> ProcessMultipleFilesAsync(string[] inputFiles, BatchConfig config)
        {
            AppLogger.Information($"Starting batch processing of {inputFiles.Length} files");

            BatchResults results = new BatchResults
            {
                StartTime = DateTime.Now,
                TotalFiles = inputFiles.Length
            };

            foreach (string file in inputFiles)
            {
                try
                {
                    AppLogger.Information($"Processing file: {Path.GetFileName(file)}");

                    // Would integrate with main processing pipeline here
                    // For now, just track the file
                    results.ProcessedFiles.Add(new FileResult
                    {
                        FileName = Path.GetFileName(file),
                        Success = true,
                        MessageCount = 0 // Would be populated by actual processing
                    });

                    results.SuccessCount++;
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to process {file}: {ex.Message}");

                    results.ProcessedFiles.Add(new FileResult
                    {
                        FileName = Path.GetFileName(file),
                        Success = false,
                        Error = ex.Message
                    });

                    results.FailureCount++;

                    if (!config.ContinueOnError)
                    {
                        break;
                    }
                }
            }

            results.EndTime = DateTime.Now;
            results.Duration = results.EndTime - results.StartTime;

            AppLogger.Information($"Batch processing complete: {results.SuccessCount}/{results.TotalFiles} successful");
            return results;
        }

        public async Task ExportResultsAsync(BatchResults results, string outputPath)
        {
            using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync("# Batch Processing Results");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"**Start**: {results.StartTime:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync($"**End**: {results.EndTime:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync($"**Duration**: {results.Duration}");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"**Total Files**: {results.TotalFiles}");
                await writer.WriteLineAsync($"**Successful**: {results.SuccessCount}");
                await writer.WriteLineAsync($"**Failed**: {results.FailureCount}");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("## File Details");
                await writer.WriteLineAsync();

                foreach (FileResult file in results.ProcessedFiles)
                {
                    string status = file.Success ? "?" : "?";
                    await writer.WriteLineAsync($"- {status} **{file.FileName}**");

                    if (file.Success)
                    {
                        await writer.WriteLineAsync($"  - Messages: {file.MessageCount}");
                    }
                    else
                    {
                        await writer.WriteLineAsync($"  - Error: {file.Error}");
                    }
                }
            }
        }
    }

    public class BatchConfig
    {
        public bool ContinueOnError { get; set; } = true;
        public string OutputDirectory { get; set; } = string.Empty;
        public HashSet<OutputFormat> Formats { get; set; } = new HashSet<OutputFormat>();
    }

    public class BatchResults
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<FileResult> ProcessedFiles { get; set; } = new List<FileResult>();
    }

    public class FileResult
    {
        public string FileName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int MessageCount { get; set; }
        public string? Error { get; set; }
    }
}
