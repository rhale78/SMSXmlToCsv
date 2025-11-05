using SMSXmlToCsv.Core.Exporters;
using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Tests.Exporters;

public class BaseDataExporterTests
{
    private class TestExporter : BaseDataExporter
    {
        public override string FileExtension => "test";

        public List<Message> ExportedMessages { get; } = new();
        public string? ExportedFilePath { get; private set; }

        protected override Task ExportToFileAsync(IEnumerable<Message> messages, string filePath)
        {
            ExportedMessages.AddRange(messages);
            ExportedFilePath = filePath;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExportAsync_WithValidInputs_CreatesDirectoryAndExports()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message>
        {
            new Message { Id = "1", Body = "Test message 1" },
            new Message { Id = "2", Body = "Test message 2" }
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "test_exports", Guid.NewGuid().ToString());
        var baseFileName = "test_file";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            Assert.True(Directory.Exists(outputDir));
            Assert.Equal(2, exporter.ExportedMessages.Count);
            Assert.Equal("test_file.test", Path.GetFileName(exporter.ExportedFilePath));
            Assert.Equal(outputDir, Path.GetDirectoryName(exporter.ExportedFilePath));
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
    public async Task ExportAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var exporter = new TestExporter();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            exporter.ExportAsync(null!, "/some/path", "filename"));
    }

    [Fact]
    public async Task ExportAsync_WithEmptyOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            exporter.ExportAsync(messages, "", "filename"));
    }

    [Fact]
    public async Task ExportAsync_WithNullOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            exporter.ExportAsync(messages, null!, "filename"));
    }

    [Fact]
    public async Task ExportAsync_WithEmptyBaseFileName_ThrowsArgumentException()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            exporter.ExportAsync(messages, "/some/path", ""));
    }

    [Fact]
    public async Task ExportAsync_WithNullBaseFileName_ThrowsArgumentException()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            exporter.ExportAsync(messages, "/some/path", null!));
    }

    [Fact]
    public async Task ExportAsync_WithEmptyMessageCollection_CallsExportWithEmptyCollection()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message>();
        var outputDir = Path.Combine(Path.GetTempPath(), "test_exports", Guid.NewGuid().ToString());
        var baseFileName = "empty_test";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            Assert.Empty(exporter.ExportedMessages);
            Assert.NotNull(exporter.ExportedFilePath);
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
    public async Task ExportAsync_WithNestedDirectory_CreatesAllDirectories()
    {
        // Arrange
        var exporter = new TestExporter();
        var messages = new List<Message> { new Message { Id = "1" } };
        var outputDir = Path.Combine(Path.GetTempPath(), "test_exports", Guid.NewGuid().ToString(), "nested", "deep");
        var baseFileName = "nested_test";

        try
        {
            // Act
            await exporter.ExportAsync(messages, outputDir, baseFileName);

            // Assert
            Assert.True(Directory.Exists(outputDir));
            Assert.Equal(outputDir, Path.GetDirectoryName(exporter.ExportedFilePath));
        }
        finally
        {
            // Cleanup
            var rootDir = Path.Combine(Path.GetTempPath(), "test_exports");
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, true);
            }
        }
    }

    [Fact]
    public void FileExtension_ReturnsCorrectExtension()
    {
        // Arrange
        var exporter = new TestExporter();

        // Act
        var extension = exporter.FileExtension;

        // Assert
        Assert.Equal("test", extension);
    }
}
