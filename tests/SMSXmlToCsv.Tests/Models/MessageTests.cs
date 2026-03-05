using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Tests.Models;

public class MessageTests
{
    private static Contact MakeContact(string name, string? phone = null, string? email = null)
    {
        if (phone != null) return Contact.FromPhoneNumber(name, phone);
        if (email != null) return Contact.FromEmail(name, email);
        return Contact.FromName(name);
    }

    private static Message MakeMessage(
        string fromName, string toName,
        MessageDirection direction = MessageDirection.Received,
        string body = "Hello",
        string? fromPhone = null, string? toPhone = null)
    {
        Contact from = MakeContact(fromName, fromPhone);
        Contact to = MakeContact(toName, toPhone);
        return Message.CreateTextMessage("TestApp", from, to,
            DateTimeOffset.UtcNow, body, direction);
    }

    [Fact]
    public void CreateTextMessage_SetsAllProperties()
    {
        Contact from = Contact.FromPhoneNumber("Alice", "+15551234567");
        Contact to = Contact.FromName("Me");
        DateTimeOffset ts = DateTimeOffset.UtcNow;

        Message msg = Message.CreateTextMessage("TestApp", from, to, ts, "Hello", MessageDirection.Received);

        Assert.Equal("TestApp", msg.SourceApplication);
        Assert.Equal(from, msg.From);
        Assert.Equal(to, msg.To);
        Assert.Equal(ts, msg.TimestampUtc);
        Assert.Equal("Hello", msg.Body);
        Assert.Equal(MessageDirection.Received, msg.Direction);
        Assert.Empty(msg.Attachments);
    }

    [Fact]
    public void GetContactName_ReturnsSenderName_WhenDirectionReceived()
    {
        Message msg = MakeMessage("Alice", "Me", MessageDirection.Received);

        string name = msg.GetContactName(MessageDirection.Received);

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void GetContactName_ReturnsRecipientName_WhenDirectionSent()
    {
        Message msg = MakeMessage("Me", "Bob", MessageDirection.Sent);

        string name = msg.GetContactName(MessageDirection.Sent);

        Assert.Equal("Bob", name);
    }

    [Fact]
    public void GetContactName_FallsBackToEmail_WhenNameUnknown()
    {
        Contact from = Contact.FromEmail("Unknown", "alice@example.com");
        Contact to = Contact.FromName("Me");
        Message msg = Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received);

        string name = msg.GetContactName(MessageDirection.Received);

        Assert.Equal("alice@example.com", name);
    }

    [Fact]
    public void GetContactIdentifier_ReturnsPhone_ForDefaultMessage()
    {
        Contact from = Contact.FromPhoneNumber("Alice", "+15551234567");
        Contact to = Contact.FromName("Me");
        Message msg = Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received);

        string id = msg.GetContactIdentifier(MessageDirection.Received);

        Assert.Equal("+15551234567", id);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenFromIsUnknown()
    {
        Message msg = MakeMessage("Unknown", "Bob");

        Assert.False(msg.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenToIsUnknown()
    {
        Message msg = MakeMessage("Alice", "Unknown");

        Assert.False(msg.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenBothContactsKnown()
    {
        Message msg = MakeMessage("Alice", "Bob");

        Assert.True(msg.IsValid());
    }

    [Fact]
    public void GetOptimalBatchSize_Returns75_ForBaseMessage()
    {
        Message msg = MakeMessage("Alice", "Bob");

        Assert.Equal(75, msg.GetOptimalBatchSize());
    }

    [Fact]
    public void SmsMessage_GetOptimalBatchSize_Returns100()
    {
        Contact from = Contact.FromPhoneNumber("Alice", "+15551234567");
        Contact to = Contact.FromPhoneNumber("Me", "+15559999999");
        SmsMessage msg = new SmsMessage("App", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received, new List<MediaAttachment>());

        Assert.Equal(100, msg.GetOptimalBatchSize());
    }

    [Fact]
    public void EmailMessage_GetOptimalBatchSize_Returns25()
    {
        Contact from = Contact.FromEmail("Alice", "alice@example.com");
        Contact to = Contact.FromEmail("Me", "me@example.com");
        EmailMessage msg = new EmailMessage("Gmail", from, to, DateTimeOffset.UtcNow, "Body", MessageDirection.Received, new List<MediaAttachment>());

        Assert.Equal(25, msg.GetOptimalBatchSize());
    }

    [Fact]
    public void SmsMessage_IsValid_ReturnsFalse_WhenNoPhoneNumbers()
    {
        Contact from = Contact.FromName("Alice");
        Contact to = Contact.FromName("Me");
        SmsMessage msg = new SmsMessage("App", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received, new List<MediaAttachment>());

        Assert.False(msg.IsValid());
    }

    [Fact]
    public void SmsMessage_IsValid_ReturnsTrue_WhenFromHasPhone()
    {
        Contact from = Contact.FromPhoneNumber("Alice", "+15551234567");
        Contact to = Contact.FromPhoneNumber("Me", "+15559999999");
        SmsMessage msg = new SmsMessage("App", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received, new List<MediaAttachment>());

        Assert.True(msg.IsValid());
    }

    [Fact]
    public void EmailMessage_GetContactIdentifier_PrefersEmail()
    {
        Contact from = Contact.FromEmail("Alice", "alice@example.com");
        Contact to = Contact.FromEmail("Me", "me@example.com");
        EmailMessage msg = new EmailMessage("Gmail", from, to, DateTimeOffset.UtcNow, "Body", MessageDirection.Received, new List<MediaAttachment>());

        string id = msg.GetContactIdentifier(MessageDirection.Received);

        Assert.Equal("alice@example.com", id);
    }

    [Fact]
    public void SocialMediaMessage_IsValid_ReturnsFalse_WhenNameEmpty()
    {
        Contact from = Contact.FromName("Alice");
        Contact to = Contact.FromName(string.Empty);
        SocialMediaMessage msg = new SocialMediaMessage("Facebook", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received, new List<MediaAttachment>());

        Assert.False(msg.IsValid());
    }

    [Fact]
    public void SocialMediaMessage_GetOptimalBatchSize_Returns75()
    {
        Contact from = Contact.FromName("Alice");
        Contact to = Contact.FromName("Me");
        SocialMediaMessage msg = new SocialMediaMessage("Facebook", from, to, DateTimeOffset.UtcNow, "Hi", MessageDirection.Received, new List<MediaAttachment>());

        Assert.Equal(75, msg.GetOptimalBatchSize());
    }
}
