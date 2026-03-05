using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Search;

namespace SMSXmlToCsv.Tests.Services;

public class MessageSearchServiceTests
{
    private static Message MakeMsg(string body, string fromName = "Alice",
        MessageDirection dir = MessageDirection.Received,
        DateTimeOffset? ts = null)
    {
        Contact from = Contact.FromName(fromName);
        Contact to = Contact.FromName("Me");
        return Message.CreateTextMessage("App", from, to,
            ts ?? DateTimeOffset.UtcNow, body, dir);
    }

    private static MessageSearchService BuildService(IEnumerable<Message> messages)
    {
        MessageSearchService service = new MessageSearchService();
        service.LoadMessages(messages);
        return service;
    }

    [Fact]
    public void Search_FindsExactMatch_CaseInsensitive()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("Hello world"),
            MakeMsg("Goodbye"),
        });

        List<SearchResult> results = service.Search("hello");

        Assert.Single(results);
        Assert.Equal("Hello world", results[0].Message.Body);
    }

    [Fact]
    public void Search_FindsExactMatch_CaseSensitive()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("Hello world"),
            MakeMsg("hello world"),
        });

        List<SearchResult> results = service.Search("Hello", caseSensitive: true);

        Assert.Single(results);
        Assert.Equal("Hello world", results[0].Message.Body);
    }

    [Fact]
    public void Search_ReturnsNoResults_WhenNoMatch()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("Hello world"),
        });

        List<SearchResult> results = service.Search("xyz");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_ReturnsMultipleResults()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("I love cats"),
            MakeMsg("cats are cool"),
            MakeMsg("I prefer dogs"),
        });

        List<SearchResult> results = service.Search("cats");

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_SetsCorrectMatchPosition()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("Hello world"),
        });

        List<SearchResult> results = service.Search("world");

        Assert.Single(results);
        Assert.Equal(6, results[0].MatchPosition);
    }

    [Fact]
    public void Search_IncludesContextMessages()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Before 1"),
            MakeMsg("Before 2"),
            MakeMsg("Target message"),
            MakeMsg("After 1"),
            MakeMsg("After 2"),
        };
        MessageSearchService service = BuildService(messages);

        List<SearchResult> results = service.Search("Target", contextMessages: 2);

        Assert.Single(results);
        Assert.Equal(2, results[0].ContextBefore.Count);
        Assert.Equal(2, results[0].ContextAfter.Count);
    }

    [Fact]
    public void Search_LimitsContextToAvailableMessages()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Target message"),
            MakeMsg("After only"),
        };
        MessageSearchService service = BuildService(messages);

        List<SearchResult> results = service.Search("Target", contextMessages: 3);

        Assert.Single(results);
        Assert.Empty(results[0].ContextBefore);
        Assert.Single(results[0].ContextAfter);
    }

    [Fact]
    public void Search_ReturnsEmpty_WhenNoMessagesLoaded()
    {
        MessageSearchService service = BuildService(new List<Message>());

        List<SearchResult> results = service.Search("hello");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchByContact_FiltersToSpecificContact()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("Hello from Alice", "Alice"),
            MakeMsg("Hello from Bob", "Bob"),
        });

        List<SearchResult> results = service.SearchByContact("Hello", "Alice");

        Assert.Single(results);
        Assert.Equal("Alice", results[0].Message.From.Name);
    }

    [Fact]
    public void SearchByContact_ReturnsEmpty_WhenContactHasNoMatches()
    {
        MessageSearchService service = BuildService(new[]
        {
            MakeMsg("Goodbye", "Alice"),
        });

        List<SearchResult> results = service.SearchByContact("Hello", "Alice");

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExportResultsAsync_CreatesJsonFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            string outputPath = Path.Combine(tempDir, "results.json");
            MessageSearchService service = BuildService(new[] { MakeMsg("Hello world") });
            List<SearchResult> results = service.Search("hello");

            await service.ExportResultsAsync(results, outputPath);

            Assert.True(File.Exists(outputPath));
            string content = File.ReadAllText(outputPath);
            Assert.Contains("Hello world", content);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
