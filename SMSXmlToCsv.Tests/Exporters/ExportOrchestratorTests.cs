using SMSXmlToCsv.Core.Exporters;
using SMSXmlToCsv.Core.Models;

namespace SMSXmlToCsv.Tests.Exporters;

public class ExportOrchestratorTests
{
    [Fact]
    public async Task ExportAsync_WithAllInOneStrategy_CreatesSingleFile()
    {
        // Arrange
        var exporter = new CsvExporter();
        var orchestrator = new ExportOrchestrator(exporter, showProgress: false);
        var contact1 = new Contact { Id = "1", Name = "John Doe", PhoneNumber = "+1234567890" };
        var contact2 = new Contact { Id = "2", Name = "Jane Smith", PhoneNumber = "+0987654321" };
        
        var messages = new List<Message>
        {
            new Message { Id = "1", Contact = contact1, Body = "Message 1", Timestamp = DateTime.Now },
            new Message { Id = "2", Contact = contact2, Body = "Message 2", Timestamp = DateTime.Now },
            new Message { Id = "3", Contact = contact1, Body = "Message 3", Timestamp = DateTime.Now }
        };

        var outputDir = Path.Combine(Path.GetTempPath(), "export_test", Guid.NewGuid().ToString());

        try
        {
            // Act
            await orchestrator.ExportAsync(messages, outputDir, ExportStrategy.AllInOne);

            // Assert
            var files = Directory.GetFiles(outputDir, "*.csv", SearchOption.AllDirectories);
            Assert.Single(files); // Should have exactly one file
            Assert.Contains("messages_", Path.GetFileName(files[0]));

            // Verify all messages are in the file
            var content = await File.ReadAllTextAsync(files[0]);
            Assert.Contains("Message 1", content);
            Assert.Contains("Message 2", content);
            Assert.Contains("Message 3", content);
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
    public async Task ExportAsync_WithPerContactStrategy_CreatesSeparateFilesPerContact()
    {
        // Arrange
        var exporter = new CsvExporter();
        var orchestrator = new ExportOrchestrator(exporter, showProgress: false);
        var contact1 = new Contact { Id = "1", Name = "John Doe", PhoneNumber = "+1234567890" };
        var contact2 = new Contact { Id = "2", Name = "Jane Smith", PhoneNumber = "+0987654321" };
        
        var messages = new List<Message>
        {
            new Message { Id = "1", Contact = contact1, Body = "Message from John 1", Timestamp = DateTime.Now, Type = "SMS", IsSent = true, PhoneNumber = contact1.PhoneNumber },
            new Message { Id = "2", Contact = contact2, Body = "Message from Jane 1", Timestamp = DateTime.Now, Type = "SMS", IsSent = false, PhoneNumber = contact2.PhoneNumber },
            new Message { Id = "3", Contact = contact1, Body = "Message from John 2", Timestamp = DateTime.Now, Type = "SMS", IsSent = true, PhoneNumber = contact1.PhoneNumber }
        };

        var outputDir = Path.Combine(Path.GetTempPath(), "export_test", Guid.NewGuid().ToString());

        try
        {
            // Act
            await orchestrator.ExportAsync(messages, outputDir, ExportStrategy.PerContact);

            // Assert
            var contactsDir = Path.Combine(outputDir, "contacts");
            Assert.True(Directory.Exists(contactsDir));

            var johnDoeDir = Path.Combine(contactsDir, "John Doe");
            var janeSmithDir = Path.Combine(contactsDir, "Jane Smith");
            
            Assert.True(Directory.Exists(johnDoeDir));
            Assert.True(Directory.Exists(janeSmithDir));

            // Verify John Doe's file
            var johnFiles = Directory.GetFiles(johnDoeDir, "*.csv");
            Assert.Single(johnFiles);
            var johnContent = await File.ReadAllTextAsync(johnFiles[0]);
            Assert.Contains("Message from John 1", johnContent);
            Assert.Contains("Message from John 2", johnContent);
            Assert.DoesNotContain("Message from Jane", johnContent);

            // Verify Jane Smith's file
            var janeFiles = Directory.GetFiles(janeSmithDir, "*.csv");
            Assert.Single(janeFiles);
            var janeContent = await File.ReadAllTextAsync(janeFiles[0]);
            Assert.Contains("Message from Jane 1", janeContent);
            Assert.DoesNotContain("Message from John", janeContent);
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
    public async Task ExportAsync_WithPerContactStrategy_HandlesOrphanedMessages()
    {
        // Arrange
        var exporter = new CsvExporter();
        var orchestrator = new ExportOrchestrator(exporter, showProgress: false);
        var contact1 = new Contact { Id = "1", Name = "John Doe", PhoneNumber = "+1234567890" };
        
        var messages = new List<Message>
        {
            new Message { Id = "1", Contact = contact1, Body = "Message from John", Timestamp = DateTime.Now, Type = "SMS", IsSent = true, PhoneNumber = contact1.PhoneNumber },
            new Message { Id = "2", Contact = null, Body = "Orphaned message", Timestamp = DateTime.Now, Type = "SMS", IsSent = false, PhoneNumber = "+9999999999" }
        };

        var outputDir = Path.Combine(Path.GetTempPath(), "export_test", Guid.NewGuid().ToString());

        try
        {
            // Act
            await orchestrator.ExportAsync(messages, outputDir, ExportStrategy.PerContact);

            // Assert
            var unknownDir = Path.Combine(outputDir, "contacts", "Unknown");
            Assert.True(Directory.Exists(unknownDir));

            var unknownFiles = Directory.GetFiles(unknownDir, "*.csv");
            Assert.Single(unknownFiles);
            
            var content = await File.ReadAllTextAsync(unknownFiles[0]);
            Assert.Contains("Orphaned message", content);
            Assert.DoesNotContain("Message from John", content);
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
    public async Task ExportAsync_WithCustomFileNameTemplate_UsesTemplate()
    {
        // Arrange
        var exporter = new CsvExporter();
        var orchestrator = new ExportOrchestrator(exporter, showProgress: false);
        var messages = new List<Message>
        {
            new Message { Id = "1", Body = "Test", Timestamp = DateTime.Now, Contact = new Contact { Name = "Test" } }
        };

        var outputDir = Path.Combine(Path.GetTempPath(), "export_test", Guid.NewGuid().ToString());

        try
        {
            // Act
            await orchestrator.ExportAsync(messages, outputDir, ExportStrategy.AllInOne, "custom_{date}_{time}");

            // Assert
            var files = Directory.GetFiles(outputDir, "*.csv");
            Assert.Single(files);
            Assert.Contains("custom_", Path.GetFileName(files[0]));
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
        var exporter = new CsvExporter();
        var orchestrator = new ExportOrchestrator(exporter, showProgress: false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.ExportAsync(null!, "/some/path", ExportStrategy.AllInOne));
    }

    [Fact]
    public async Task ExportAsync_WithEmptyOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var exporter = new CsvExporter();
        var orchestrator = new ExportOrchestrator(exporter, showProgress: false);
        var messages = new List<Message>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.ExportAsync(messages, "", ExportStrategy.AllInOne));
    }

    [Fact]
    public void Constructor_WithNullExporter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExportOrchestrator(null!));
    }
}
