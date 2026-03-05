using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Tests.Models;

public class MediaAttachmentTests
{
    [Fact]
    public void FileName_ReturnsFileNameFromUnixPath()
    {
        MediaAttachment attachment = new MediaAttachment("/some/path/photo.jpg", "image/jpeg");

        Assert.Equal("photo.jpg", attachment.FileName);
    }

    [Fact]
    public void FileName_ReturnsFileNameFromWindowsPath()
    {
        MediaAttachment attachment = new MediaAttachment(@"C:\Messages\Attachments\video.mp4", "video/mp4");

        Assert.Equal("video.mp4", attachment.FileName);
    }

    [Fact]
    public void FileName_ReturnsEmptyString_WhenPathIsEmpty()
    {
        MediaAttachment attachment = new MediaAttachment(string.Empty, "image/png");

        Assert.Equal(string.Empty, attachment.FileName);
    }

    [Fact]
    public void FileName_ReturnsName_WhenNoDirectorySeparator()
    {
        MediaAttachment attachment = new MediaAttachment("image.png", "image/png");

        Assert.Equal("image.png", attachment.FileName);
    }

    [Fact]
    public void Constructor_StoresMimeType()
    {
        MediaAttachment attachment = new MediaAttachment("file.pdf", "application/pdf");

        Assert.Equal("application/pdf", attachment.MimeType);
        Assert.Equal("file.pdf", attachment.OriginalSourcePath);
    }
}
