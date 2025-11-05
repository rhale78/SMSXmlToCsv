using SMSXmlToCsv.Core.Exporters;
using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Tests.Exporters;

public class CsvExporterTests
{
    [Fact]
    public async Task ExportAsync_WithValidMessages_CreatesCorrectCsvFile()
    {
        // Arrange
        var exporter = new CsvExporter();
        var messages = new List<Message>
        {
            new Message
            {
                Id = "1",
                Timestamp = new DateTime(2023, 12, 25, 10, 30, 0),
                Type = "SMS",
                IsSent = true,
                PhoneNumber = "+1234567890",
                Contact = new Contact { Name = "John Doe" },
                Body = "Hello World"
            },
            new Message
            {
                Id = "2",
                Timestamp = new DateTime(2023, 12, 25, 11, 45, 0),
                Type = "MMS",
                IsSent = false,
                PhoneNumber = "+0987654321",
                Contact = new Contact { Name = "Jane Smith" },
                Body = "Test message"
            }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "csv_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_export";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.csv");
            Assert.True(File.Exists(filePath));

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Id,Timestamp,Type,IsSent,PhoneNumber,ContactName,Body", content);
            Assert.Contains("1,2023-12-25 10:30:00,SMS,True,+1234567890,John Doe,Hello World", content);
            Assert.Contains("2,2023-12-25 11:45:00,MMS,False,+0987654321,Jane Smith,Test message", content);
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
    public async Task ExportAsync_WithMessagesContainingCommas_EscapesFieldsCorrectly()
    {
        // Arrange
        var exporter = new CsvExporter();
        var messages = new List<Message>
        {
            new Message
            {
                Id = "1",
                Timestamp = DateTime.Now,
                Type = "SMS",
                IsSent = true,
                PhoneNumber = "+1234567890",
                Contact = new Contact { Name = "John Doe" },
                Body = "Hello, World, Test"
            }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "csv_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_commas";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.csv");
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("\"Hello, World, Test\"", content);
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
    public async Task ExportAsync_WithMessagesContainingQuotes_EscapesQuotesCorrectly()
    {
        // Arrange
        var exporter = new CsvExporter();
        var messages = new List<Message>
        {
            new Message
            {
                Id = "1",
                Timestamp = DateTime.Now,
                Type = "SMS",
                IsSent = true,
                PhoneNumber = "+1234567890",
                Contact = new Contact { Name = "John Doe" },
                Body = "He said \"Hello\""
            }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "csv_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_quotes";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.csv");
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("\"He said \"\"Hello\"\"\"", content);
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
    public async Task ExportAsync_WithEmptyMessageBody_HandlesCorrectly()
    {
        // Arrange
        var exporter = new CsvExporter();
        var messages = new List<Message>
        {
            new Message
            {
                Id = "1",
                Timestamp = DateTime.Now,
                Type = "SMS",
                IsSent = true,
                PhoneNumber = "+1234567890",
                Contact = new Contact { Name = "John Doe" },
                Body = ""
            }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "csv_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_empty";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.csv");
            Assert.True(File.Exists(filePath));
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Id,Timestamp,Type,IsSent,PhoneNumber,ContactName,Body", content);
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
    public void FileExtension_ReturnsCsv()
    {
        // Arrange
        var exporter = new CsvExporter();

        // Act
        var extension = exporter.FileExtension;

        // Assert
        Assert.Equal("csv", extension);
    }
}
