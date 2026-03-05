using System.IO;
using SMSXmlToCsv.Exporters;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Tests.Exporters;

public class CsvExporterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CsvExporter _exporter;

    public CsvExporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CsvExporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _exporter = new CsvExporter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static Message MakeMsg(
        string from, string to,
        string body = "Hello",
        MessageDirection dir = MessageDirection.Received,
        string? phone = null, string? email = null)
    {
        Contact fromContact = phone != null
            ? Contact.FromPhoneNumber(from, phone)
            : email != null ? Contact.FromEmail(from, email) : Contact.FromName(from);
        Contact toContact = Contact.FromName(to);
        return Message.CreateTextMessage("TestApp", fromContact, toContact,
            new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero), body, dir);
    }

    [Fact]
    public void FileExtension_IsCsv()
    {
        Assert.Equal("csv", _exporter.FileExtension);
    }

    [Fact]
    public async Task ExportAsync_CreatesFile()
    {
        List<Message> messages = new List<Message> { MakeMsg("Alice", "Me") };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        Assert.True(File.Exists(Path.Combine(_tempDir, "test_export.csv")));
    }

    [Fact]
    public async Task ExportAsync_WritesHeaderRow()
    {
        List<Message> messages = new List<Message> { MakeMsg("Alice", "Me") };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        Assert.Contains("SourceApplication", content);
        Assert.Contains("From", content);
        Assert.Contains("Body", content);
    }

    [Fact]
    public async Task ExportAsync_WritesMessageContent()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me", body: "Hello World", phone: "+15551234567")
        };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        Assert.Contains("Alice", content);
        Assert.Contains("Hello World", content);
        Assert.Contains("+15551234567", content);
    }

    [Fact]
    public async Task ExportAsync_WritesMultipleMessages()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me", "First message"),
            MakeMsg("Bob", "Me", "Second message"),
            MakeMsg("Charlie", "Me", "Third message"),
        };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        Assert.Contains("First message", content);
        Assert.Contains("Second message", content);
        Assert.Contains("Third message", content);
    }

    [Fact]
    public async Task ExportAsync_WritesISO8601Timestamp()
    {
        List<Message> messages = new List<Message> { MakeMsg("Alice", "Me") };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        // ISO 8601 format includes 'T' separator
        Assert.Contains("2024-01-15T12:00:00", content);
    }

    [Fact]
    public async Task ExportAsync_HandlesEmptyMessages()
    {
        await _exporter.ExportAsync(new List<Message>(), _tempDir, "empty_export");

        string filePath = Path.Combine(_tempDir, "empty_export.csv");
        Assert.True(File.Exists(filePath));
        // Header line should still exist
        string content = File.ReadAllText(filePath);
        Assert.Contains("SourceApplication", content);
    }

    [Fact]
    public async Task ExportAsync_WritesAttachmentInfo()
    {
        List<MediaAttachment> attachments = new List<MediaAttachment>
        {
            new MediaAttachment("photo.jpg", "image/jpeg"),
            new MediaAttachment("video.mp4", "video/mp4"),
        };
        Contact from = Contact.FromName("Alice");
        Contact to = Contact.FromName("Me");
        Message msgWithAttachments = new Message(
            "TestApp", from, to,
            new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
            "See attachments", MessageDirection.Received, attachments);

        await _exporter.ExportAsync(new[] { msgWithAttachments }, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        Assert.Contains("photo.jpg", content);
        Assert.Contains("video.mp4", content);
    }

    [Fact]
    public async Task ExportAsync_WritesEmailContact()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me", email: "alice@example.com")
        };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        Assert.Contains("alice@example.com", content);
    }

    [Fact]
    public async Task ExportAsync_HandlesBodyWithCommas()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me", "Hello, World, this has commas")
        };

        await _exporter.ExportAsync(messages, _tempDir, "test_export");

        string content = File.ReadAllText(Path.Combine(_tempDir, "test_export.csv"));
        Assert.Contains("Hello, World, this has commas", content);
    }
}
