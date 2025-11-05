using SMSXmlToCsv.Core.Exporters;
using SMSXmlToCsv.Core.Models;
using Parquet;

namespace SMSXmlToCsv.Tests.Exporters;

public class ParquetExporterTests
{
    [Fact]
    public async Task ExportAsync_WithValidMessages_CreatesCorrectParquetFile()
    {
        // Arrange
        var exporter = new ParquetExporter();
        var messages = new List<Message>
        {
            new Message
            {
                Id = "1",
                Timestamp = new DateTime(2023, 12, 25, 10, 30, 0),
                Type = "SMS",
                IsSent = true,
                PhoneNumber = "+1234567890",
                Contact = new Contact { Name = "John Doe", Id = "contact1" },
                Body = "Hello World"
            },
            new Message
            {
                Id = "2",
                Timestamp = new DateTime(2023, 12, 25, 11, 45, 0),
                Type = "MMS",
                IsSent = false,
                PhoneNumber = "+0987654321",
                Contact = new Contact { Name = "Jane Smith", Id = "contact2" },
                Body = "Test message"
            }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "parquet_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_export";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.parquet");
            Assert.True(File.Exists(filePath));
            
            // Verify file is not empty
            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0);
            
            // Verify we can read the Parquet file
            await using var stream = File.OpenRead(filePath);
            using var reader = await ParquetReader.CreateAsync(stream);
            Assert.Equal(1, reader.RowGroupCount);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_WithEmptyMessages_CreatesEmptyParquetFile()
    {
        // Arrange
        var exporter = new ParquetExporter();
        var messages = new List<Message>();
        var outputDir = Path.Combine(Path.GetTempPath(), "parquet_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_empty";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.parquet");
            Assert.True(File.Exists(filePath));
            
            // Empty file should be created
            var fileInfo = new FileInfo(filePath);
            Assert.Equal(0, fileInfo.Length);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    [Fact]
    public void FileExtension_ReturnsParquet()
    {
        // Arrange
        var exporter = new ParquetExporter();

        // Act
        var extension = exporter.FileExtension;

        // Assert
        Assert.Equal("parquet", extension);
    }
}
