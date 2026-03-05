using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services;

namespace SMSXmlToCsv.Tests.Services;

public class ExportPathBuilderTests
{
    private readonly ExportPathBuilder _builder = new ExportPathBuilder();

    [Fact]
    public void BuildPath_ReplacesDatePlaceholder()
    {
        string path = _builder.BuildPath("output/{date}/file.csv");

        Assert.Matches(@"output/\d{4}-\d{2}-\d{2}/file\.csv", path);
    }

    [Fact]
    public void BuildPath_ReplacesTimePlaceholder()
    {
        string path = _builder.BuildPath("output/{time}/file.csv");

        Assert.Matches(@"output/\d{2}-\d{2}-\d{2}/file\.csv", path);
    }

    [Fact]
    public void BuildPath_ReplacesDatetimePlaceholder()
    {
        string path = _builder.BuildPath("output/{datetime}/file.csv");

        Assert.Matches(@"output/\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}/file\.csv", path);
    }

    [Fact]
    public void BuildPath_ReplacesContactNamePlaceholder()
    {
        Contact contact = Contact.FromName("Alice Smith");
        string path = _builder.BuildPath("contacts/{contact_name}/messages.csv", contact);

        Assert.Equal("contacts/Alice Smith/messages.csv", path);
    }

    [Fact]
    public void BuildPath_SanitizesInvalidChars_InContactName()
    {
        // Use a character that is universally invalid (path separator)
        Contact contact = Contact.FromName("Alice/Bob");
        string path = _builder.BuildPath("{contact_name}", contact);

        Assert.DoesNotContain("/", path);
    }

    [Fact]
    public void BuildPath_ReplacesProjectPlaceholder()
    {
        string path = _builder.BuildPath("projects/{project}/output.csv", projectName: "MyProject");

        Assert.Equal("projects/MyProject/output.csv", path);
    }

    [Fact]
    public void BuildPath_UsesUnknown_WhenProjectNameIsNull()
    {
        string path = _builder.BuildPath("projects/{project}/output.csv");

        Assert.Equal("projects/Unknown/output.csv", path);
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"ExportPathTest_{Guid.NewGuid():N}");
        try
        {
            Assert.False(Directory.Exists(tempDir));

            _builder.EnsureDirectoryExists(tempDir);

            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void EnsureDirectoryExists_DoesNotThrow_WhenDirectoryAlreadyExists()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"ExportPathTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            // Should not throw
            _builder.EnsureDirectoryExists(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
