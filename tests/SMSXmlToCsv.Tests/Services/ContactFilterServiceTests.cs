using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services;

namespace SMSXmlToCsv.Tests.Services;

public class ContactFilterServiceTests
{
    private readonly ContactFilterService _service = new ContactFilterService();

    private static Message MakeMsg(string fromName, string toName,
        MessageDirection dir = MessageDirection.Received)
    {
        Contact from = Contact.FromName(fromName);
        Contact to = Contact.FromName(toName);
        return Message.CreateTextMessage("App", from, to, DateTimeOffset.UtcNow, "Body", dir);
    }

    [Fact]
    public void FilterUnknownContacts_RemovesMessagesWithUnknownFrom()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Unknown", "Bob"),
            MakeMsg("Alice", "Bob"),
        };

        IEnumerable<Message> result = _service.FilterUnknownContacts(messages);

        Assert.Single(result);
        Assert.Equal("Alice", result.First().From.Name);
    }

    [Fact]
    public void FilterUnknownContacts_RemovesMessagesWithUnknownTo()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Unknown"),
            MakeMsg("Alice", "Bob"),
        };

        IEnumerable<Message> result = _service.FilterUnknownContacts(messages);

        Assert.Single(result);
    }

    [Fact]
    public void FilterUnknownContacts_KeepsAllMessages_WhenNoUnknownContacts()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Bob"),
            MakeMsg("Charlie", "Dave"),
        };

        IEnumerable<Message> result = _service.FilterUnknownContacts(messages);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void FilterUnknownContacts_ReturnsEmpty_WhenAllUnknown()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Unknown", "Bob"),
            MakeMsg("Alice", "Unknown"),
        };

        IEnumerable<Message> result = _service.FilterUnknownContacts(messages);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterUnknownContacts_RemovesNoneVariant()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("none", "Bob"),
            MakeMsg("Alice", "n/a"),
        };

        IEnumerable<Message> result = _service.FilterUnknownContacts(messages);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterUnknownContacts_ReturnsEmpty_ForEmptyInput()
    {
        IEnumerable<Message> result = _service.FilterUnknownContacts(new List<Message>());

        Assert.Empty(result);
    }

    [Fact]
    public void FilterByContacts_ReturnsOnlyMatchingContacts()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me"),
            MakeMsg("Bob", "Me"),
            MakeMsg("Charlie", "Me"),
        };
        HashSet<string> filter = new HashSet<string> { "Alice", "Charlie" };

        IEnumerable<Message> result = _service.FilterByContacts(messages, filter);

        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.NotEqual("Bob", m.From.Name));
    }

    [Fact]
    public void FilterByContacts_IsCaseInsensitive()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me"),
            MakeMsg("Bob", "Me"),
        };
        HashSet<string> filter = new HashSet<string> { "alice" };

        IEnumerable<Message> result = _service.FilterByContacts(messages, filter);

        Assert.Single(result);
    }

    [Fact]
    public void FilterByContacts_MatchesRecipientToo()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Me", "Alice", MessageDirection.Sent),
            MakeMsg("Me", "Bob", MessageDirection.Sent),
        };
        HashSet<string> filter = new HashSet<string> { "Alice" };

        IEnumerable<Message> result = _service.FilterByContacts(messages, filter);

        Assert.Single(result);
    }

    [Fact]
    public void FilterByContacts_ReturnsEmpty_WhenNoMatch()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", "Me"),
        };
        HashSet<string> filter = new HashSet<string> { "Nobody" };

        IEnumerable<Message> result = _service.FilterByContacts(messages, filter);

        Assert.Empty(result);
    }
}
