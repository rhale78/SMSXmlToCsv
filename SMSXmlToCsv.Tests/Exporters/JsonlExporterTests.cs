using SMSXmlToCsv.Core.Exporters;
using SMSXmlToCsv.Core.Models;
using System.Text.Json;

namespace SMSXmlToCsv.Tests.Exporters;

public class JsonlExporterTests
{
    [Fact]
    public async Task ExportAsync_WithValidMessages_CreatesCorrectJsonlFile()
    {
        // Arrange
        var exporter = new JsonlExporter();
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
        var outputDir = Path.Combine(Path.GetTempPath(), "jsonl_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_export";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.jsonl");
            Assert.True(File.Exists(filePath));

            var lines = await File.ReadAllLinesAsync(filePath);
            Assert.Equal(2, lines.Length);

            // Verify first line is valid JSON
            var firstLine = JsonSerializer.Deserialize<JsonElement>(lines[0]);
            Assert.Equal("1", firstLine.GetProperty("id").GetString());
            Assert.Equal("John Doe", firstLine.GetProperty("contactName").GetString());
            Assert.Equal("Hello World", firstLine.GetProperty("body").GetString());

            // Verify second line is valid JSON
            var secondLine = JsonSerializer.Deserialize<JsonElement>(lines[1]);
            Assert.Equal("2", secondLine.GetProperty("id").GetString());
            Assert.Equal("Jane Smith", secondLine.GetProperty("contactName").GetString());
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
    public async Task ExportAsync_WithEmptyMessages_CreatesEmptyJsonlFile()
    {
        // Arrange
        var exporter = new JsonlExporter();
        var messages = new List<Message>();
        var outputDir = Path.Combine(Path.GetTempPath(), "jsonl_export_test", Guid.NewGuid().ToString());
        var baseFileName = "test_empty";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            var filePath = Path.Combine(outputDir, $"{baseFileName}.jsonl");
            Assert.True(File.Exists(filePath));
            
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Empty(content.Trim());
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
    public void FileExtension_ReturnsJsonl()
    {
        // Arrange
        var exporter = new JsonlExporter();

        // Act
        var extension = exporter.FileExtension;

        // Assert
        Assert.Equal("jsonl", extension);
    }
}
