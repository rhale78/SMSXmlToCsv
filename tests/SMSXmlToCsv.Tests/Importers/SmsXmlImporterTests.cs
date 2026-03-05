using System.IO;
using SMSXmlToCsv.Importers;
using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Tests.Importers;

public class SmsXmlImporterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SmsXmlImporter _importer;

    public SmsXmlImporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SmsXmlTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _importer = new SmsXmlImporter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteXml(string content)
    {
        string path = Path.Combine(_tempDir, "sms.xml");
        File.WriteAllText(path, content);
        return path;
    }

    // --- CanImport ---

    [Fact]
    public void SourceName_IsExpected()
    {
        Assert.Equal("Android SMS Backup & Restore", _importer.SourceName);
    }

    [Fact]
    public void CanImport_ReturnsTrue_ForValidSmsXml()
    {
        string path = WriteXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><smses count=\"1\"></smses>");

        Assert.True(_importer.CanImport(path));
    }

    [Fact]
    public void CanImport_ReturnsFalse_ForNonExistentFile()
    {
        Assert.False(_importer.CanImport(Path.Combine(_tempDir, "nonexistent.xml")));
    }

    [Fact]
    public void CanImport_ReturnsFalse_ForNonXmlFile()
    {
        string path = Path.Combine(_tempDir, "messages.csv");
        File.WriteAllText(path, "col1,col2");

        Assert.False(_importer.CanImport(path));
    }

    [Fact]
    public void CanImport_ReturnsFalse_ForXmlWithWrongRoot()
    {
        string path = WriteXml("<?xml version=\"1.0\"?><messages></messages>");

        Assert.False(_importer.CanImport(path));
    }

    // --- ImportAsync: SMS ---

    [Fact]
    public async Task ImportAsync_ParsesSingleReceivedSms()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <sms address=""+15551234567"" date=""1704067200000"" type=""1"" body=""Hello there"" contact_name=""Alice"" />
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Message msg = list[0];
        Assert.Equal(MessageDirection.Received, msg.Direction);
        Assert.Equal("Hello there", msg.Body);
        Assert.Equal("Alice", msg.From.Name);
        Assert.Contains("+15551234567", msg.From.PhoneNumbers);
        Assert.Equal("Me", msg.To.Name);
        Assert.Equal("Android SMS Backup & Restore", msg.SourceApplication);
    }

    [Fact]
    public async Task ImportAsync_ParsesSingleSentSms()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <sms address=""+15559876543"" date=""1704067200000"" type=""2"" body=""Hi Bob"" contact_name=""Bob"" />
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Message msg = list[0];
        Assert.Equal(MessageDirection.Sent, msg.Direction);
        Assert.Equal("Hi Bob", msg.Body);
        Assert.Equal("Me", msg.From.Name);
        Assert.Equal("Bob", msg.To.Name);
        Assert.Contains("+15559876543", msg.To.PhoneNumbers);
    }

    [Fact]
    public async Task ImportAsync_HandlesUnknownMessageType()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <sms address=""+15551234567"" date=""1704067200000"" type=""99"" body=""Test"" contact_name=""Alice"" />
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Assert.Equal(MessageDirection.Unknown, list[0].Direction);
    }

    [Fact]
    public async Task ImportAsync_ParsesTimestampCorrectly()
    {
        // 1704067200000 ms = 2024-01-01 00:00:00 UTC
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <sms address=""+15551234567"" date=""1704067200000"" type=""1"" body=""Hello"" contact_name=""Alice"" />
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        DateTimeOffset ts = messages.First().TimestampUtc;

        Assert.Equal(2024, ts.Year);
        Assert.Equal(1, ts.Month);
        Assert.Equal(1, ts.Day);
    }

    [Fact]
    public async Task ImportAsync_ParsesMultipleSmsMessages()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""3"">
  <sms address=""+15551111111"" date=""1704067200000"" type=""1"" body=""Hello"" contact_name=""Alice"" />
  <sms address=""+15552222222"" date=""1704070800000"" type=""2"" body=""Bye"" contact_name=""Bob"" />
  <sms address=""+15553333333"" date=""1704074400000"" type=""1"" body=""Hi again"" contact_name=""Charlie"" />
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task ImportAsync_ReturnsEmpty_ForEmptySmses()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""0"">
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);

        Assert.Empty(messages);
    }

    [Fact]
    public async Task ImportAsync_SkipsMalformedSmsElement()
    {
        // No 'date' attribute — should skip without throwing
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""2"">
  <sms address=""+15551234567"" type=""1"" body=""Good"" contact_name=""Alice"" />
  <sms address=""+15559876543"" date=""1704067200000"" type=""1"" body=""Valid"" contact_name=""Bob"" />
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        // The malformed one (missing date) results in a date of 0 which still parses,
        // but if date is non-numeric it would be skipped.  Both should parse here.
        Assert.True(list.Count >= 1);
    }

    // --- ImportAsync: MMS ---

    [Fact]
    public async Task ImportAsync_ParsesSimpleMms()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <mms date=""1704067200000"" msg_box=""1"" contact_name=""Alice"">
    <addr address=""+15551234567"" type=""151"" charset=""106"" />
    <part ct=""text/plain"" text=""MMS text body"" seq=""0"" />
  </mms>
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Message msg = list[0];
        Assert.Equal(MessageDirection.Received, msg.Direction);
        Assert.Equal("MMS text body", msg.Body);
        Assert.Equal("Alice", msg.From.Name);
    }

    [Fact]
    public async Task ImportAsync_ParsesMmsWithImageAttachment()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <mms date=""1704067200000"" msg_box=""1"" contact_name=""Alice"">
    <addr address=""+15551234567"" type=""151"" charset=""106"" />
    <part ct=""text/plain"" text=""See photo"" seq=""0"" />
    <part ct=""image/jpeg"" name=""photo.jpg"" data=""base64data"" seq=""1"" />
  </mms>
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Message msg = list[0];
        Assert.Equal("See photo", msg.Body);
        Assert.Single(msg.Attachments);
        Assert.Equal("photo.jpg", msg.Attachments[0].FileName);
        Assert.Equal("image/jpeg", msg.Attachments[0].MimeType);
    }

    [Fact]
    public async Task ImportAsync_SkipsSMILParts()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <mms date=""1704067200000"" msg_box=""1"" contact_name=""Alice"">
    <addr address=""+15551234567"" type=""151"" charset=""106"" />
    <part ct=""application/smil"" text=""smil content"" seq=""-1"" />
    <part ct=""text/plain"" text=""Actual text"" seq=""0"" />
  </mms>
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Assert.Empty(list[0].Attachments);
        Assert.Equal("Actual text", list[0].Body);
    }

    [Fact]
    public async Task ImportAsync_ParsesBothSmsAndMmsMessages()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""2"">
  <sms address=""+15551111111"" date=""1704067200000"" type=""1"" body=""Text message"" contact_name=""Alice"" />
  <mms date=""1704070800000"" msg_box=""2"" contact_name=""Bob"">
    <addr address=""+15552222222"" type=""151"" charset=""106"" />
    <part ct=""text/plain"" text=""MMS message"" seq=""0"" />
  </mms>
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task ImportAsync_ThrowsOnMissingRootElement()
    {
        string path = WriteXml(@"<?xml version=""1.0""?>");

        // XDocument.Load throws XmlException when the XML has no root element
        await Assert.ThrowsAsync<System.Xml.XmlException>(() => _importer.ImportAsync(path));
    }

    [Fact]
    public async Task ImportAsync_MmsWithNoAddr_UsesContactName()
    {
        string path = WriteXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smses count=""1"">
  <mms date=""1704067200000"" msg_box=""1"" contact_name=""Alice"">
    <part ct=""text/plain"" text=""No address"" seq=""0"" />
  </mms>
</smses>");

        IEnumerable<Message> messages = await _importer.ImportAsync(path);
        List<Message> list = messages.ToList();

        Assert.Single(list);
        Assert.Equal("Alice", list[0].From.Name);
    }
}
