using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services;

namespace SMSXmlToCsv.Tests.Services;

public class ContactMergeServiceTests
{
    private readonly ContactMergeService _service = new ContactMergeService();

    [Fact]
    public void FindDuplicates_ReturnsEmpty_WhenNoContacts()
    {
        List<ContactMergeCandidate> result = _service.FindDuplicates(new List<Contact>());

        Assert.Empty(result);
    }

    [Fact]
    public void FindDuplicates_ReturnsEmpty_WhenNoDuplicates()
    {
        List<Contact> contacts = new List<Contact>
        {
            Contact.FromPhoneNumber("Alice", "+15551111111"),
            Contact.FromPhoneNumber("Bob", "+15552222222"),
            Contact.FromPhoneNumber("Charlie", "+15553333333"),
        };

        List<ContactMergeCandidate> result = _service.FindDuplicates(contacts);

        Assert.Empty(result);
    }

    [Fact]
    public void FindDuplicates_DetectsSameNormalizedName()
    {
        List<Contact> contacts = new List<Contact>
        {
            Contact.FromPhoneNumber("Alice Smith", "+15551111111"),
            Contact.FromPhoneNumber("Alice Smith", "+15552222222"),
        };

        List<ContactMergeCandidate> result = _service.FindDuplicates(contacts);

        Assert.True(result.Count > 0);
    }

    [Fact]
    public void FindDuplicates_DetectsSamePhoneNumber()
    {
        List<Contact> contacts = new List<Contact>
        {
            Contact.FromPhoneNumber("Alice", "+15551111111"),
            Contact.FromPhoneNumber("Alice S.", "+15551111111"),
        };

        List<ContactMergeCandidate> result = _service.FindDuplicates(contacts);

        Assert.True(result.Count > 0);
    }

    [Fact]
    public void ApplyMergeDecisions_ReturnsMessages_WhenNoDecisions()
    {
        Contact from = Contact.FromName("Alice");
        Contact to = Contact.FromName("Me");
        List<Message> messages = new List<Message>
        {
            Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Hello", MessageDirection.Received),
        };

        List<Message> result = _service.ApplyMergeDecisions(messages, new Dictionary<string, string>()).ToList();

        Assert.Single(result);
        Assert.Equal("Alice", result[0].From.Name);
    }

    [Fact]
    public void ApplyMergeDecisions_RenamesFromContact()
    {
        Contact from = Contact.FromName("Alice S.");
        Contact to = Contact.FromName("Me");
        List<Message> messages = new List<Message>
        {
            Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Hello", MessageDirection.Received),
        };
        Dictionary<string, string> decisions = new Dictionary<string, string>
        {
            { "Alice S.", "Alice Smith" }
        };

        List<Message> result = _service.ApplyMergeDecisions(messages, decisions).ToList();

        Assert.Single(result);
        Assert.Equal("Alice Smith", result[0].From.Name);
    }

    [Fact]
    public void ApplyMergeDecisions_RenamesRecipient()
    {
        Contact from = Contact.FromName("Me");
        Contact to = Contact.FromName("Bob J.");
        List<Message> messages = new List<Message>
        {
            Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Hello", MessageDirection.Sent),
        };
        Dictionary<string, string> decisions = new Dictionary<string, string>
        {
            { "Bob J.", "Bob Jones" }
        };

        List<Message> result = _service.ApplyMergeDecisions(messages, decisions).ToList();

        Assert.Equal("Bob Jones", result[0].To.Name);
    }

    [Fact]
    public void ApplyMergeDecisions_PreservesPhoneNumbers()
    {
        Contact from = Contact.FromPhoneNumber("Alice S.", "+15551111111");
        Contact to = Contact.FromName("Me");
        List<Message> messages = new List<Message>
        {
            Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Hello", MessageDirection.Received),
        };
        Dictionary<string, string> decisions = new Dictionary<string, string>
        {
            { "Alice S.", "Alice Smith" }
        };

        List<Message> result = _service.ApplyMergeDecisions(messages, decisions).ToList();

        Assert.Contains("+15551111111", result[0].From.PhoneNumbers);
    }
}
