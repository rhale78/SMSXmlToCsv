using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Analysis;

namespace SMSXmlToCsv.Tests.Services;

public class ThreadAnalyzerTests
{
    private static Message MakeMsg(string fromName, DateTimeOffset ts,
        MessageDirection dir = MessageDirection.Received)
    {
        Contact from = Contact.FromName(fromName);
        Contact to = Contact.FromName("Me");
        return Message.CreateTextMessage("App", from, to, ts, "Body", dir);
    }

    [Fact]
    public void DetectThreads_ReturnsEmpty_ForNoMessages()
    {
        ThreadAnalyzer analyzer = new ThreadAnalyzer();

        List<ConversationThread> threads = analyzer.DetectThreads(new List<Message>());

        Assert.Empty(threads);
    }

    [Fact]
    public void DetectThreads_GroupsConsecutiveMessages_WithinTimeout()
    {
        DateTimeOffset start = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        ThreadAnalyzer analyzer = new ThreadAnalyzer(threadTimeoutMinutes: 30, minimumThreadLength: 2);

        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", start),
            MakeMsg("Alice", start.AddMinutes(10)),
            MakeMsg("Alice", start.AddMinutes(20)),
        };

        List<ConversationThread> threads = analyzer.DetectThreads(messages);

        Assert.Single(threads);
        Assert.Equal(3, threads[0].MessageCount);
    }

    [Fact]
    public void DetectThreads_SplitsOnTimeout()
    {
        DateTimeOffset start = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        ThreadAnalyzer analyzer = new ThreadAnalyzer(threadTimeoutMinutes: 30, minimumThreadLength: 2);

        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", start),
            MakeMsg("Alice", start.AddMinutes(10)),
            // Gap of 2 hours — starts a new thread
            MakeMsg("Alice", start.AddHours(2)),
            MakeMsg("Alice", start.AddHours(2).AddMinutes(10)),
        };

        List<ConversationThread> threads = analyzer.DetectThreads(messages);

        Assert.Equal(2, threads.Count);
    }

    [Fact]
    public void DetectThreads_ExcludesShortThreadsBelowMinimum()
    {
        DateTimeOffset start = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        ThreadAnalyzer analyzer = new ThreadAnalyzer(threadTimeoutMinutes: 30, minimumThreadLength: 3);

        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", start),
            MakeMsg("Alice", start.AddMinutes(5)), // Only 2 messages in this thread
        };

        List<ConversationThread> threads = analyzer.DetectThreads(messages);

        Assert.Empty(threads);
    }

    [Fact]
    public void DetectThreads_SeparatesContactGroups()
    {
        DateTimeOffset start = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        ThreadAnalyzer analyzer = new ThreadAnalyzer(threadTimeoutMinutes: 30, minimumThreadLength: 2);

        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", start),
            MakeMsg("Alice", start.AddMinutes(5)),
            MakeMsg("Bob", start.AddMinutes(1)),
            MakeMsg("Bob", start.AddMinutes(6)),
        };

        List<ConversationThread> threads = analyzer.DetectThreads(messages);

        Assert.Equal(2, threads.Count);
        Assert.Contains(threads, t => t.ContactName == "Alice");
        Assert.Contains(threads, t => t.ContactName == "Bob");
    }

    [Fact]
    public void CalculateStatistics_ReturnsEmptyStats_ForNoThreads()
    {
        ThreadAnalyzer analyzer = new ThreadAnalyzer();

        ThreadStatistics stats = analyzer.CalculateStatistics(new List<ConversationThread>());

        Assert.Equal(0, stats.TotalThreads);
    }

    [Fact]
    public void CalculateStatistics_ComputesCorrectTotals()
    {
        DateTimeOffset start = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        ThreadAnalyzer analyzer = new ThreadAnalyzer(threadTimeoutMinutes: 30, minimumThreadLength: 2);

        List<Message> messages = new List<Message>
        {
            MakeMsg("Alice", start),
            MakeMsg("Alice", start.AddMinutes(5)),
            MakeMsg("Alice", start.AddMinutes(10)),
        };

        List<ConversationThread> threads = analyzer.DetectThreads(messages);
        ThreadStatistics stats = analyzer.CalculateStatistics(threads);

        Assert.Equal(1, stats.TotalThreads);
        Assert.Equal(3, stats.TotalMessages);
    }
}
