using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Analysis;

namespace SMSXmlToCsv.Tests.Services;

public class StatisticsAnalyzerTests
{
    private readonly StatisticsAnalyzer _analyzer = new StatisticsAnalyzer();

    private static Message MakeMsg(string body, MessageDirection dir,
        string fromName = "Alice", DateTimeOffset? ts = null,
        List<MediaAttachment>? attachments = null)
    {
        Contact from = Contact.FromName(fromName);
        Contact to = Contact.FromName("Me");
        return new Message(
            "App", from, to,
            ts ?? DateTimeOffset.UtcNow,
            body, dir,
            attachments ?? new List<MediaAttachment>());
    }

    [Fact]
    public void AnalyzeMessages_ReturnsZeroStats_ForEmpty()
    {
        MessageStatistics stats = _analyzer.AnalyzeMessages(new List<Message>());

        Assert.Equal(0, stats.TotalMessages);
        Assert.Equal(0, stats.SentMessages);
        Assert.Equal(0, stats.ReceivedMessages);
    }

    [Fact]
    public void AnalyzeMessages_CountsTotalMessages()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Hello", MessageDirection.Received),
            MakeMsg("World", MessageDirection.Sent),
            MakeMsg("Hi", MessageDirection.Received),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.Equal(3, stats.TotalMessages);
    }

    [Fact]
    public void AnalyzeMessages_CountsSentAndReceivedSeparately()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("R1", MessageDirection.Received),
            MakeMsg("S1", MessageDirection.Sent),
            MakeMsg("S2", MessageDirection.Sent),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.Equal(1, stats.ReceivedMessages);
        Assert.Equal(2, stats.SentMessages);
    }

    [Fact]
    public void AnalyzeMessages_SetsDateRange()
    {
        DateTimeOffset early = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset late = new DateTimeOffset(2023, 12, 31, 0, 0, 0, TimeSpan.Zero);

        List<Message> messages = new List<Message>
        {
            MakeMsg("First", MessageDirection.Received, ts: early),
            MakeMsg("Last", MessageDirection.Sent, ts: late),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.Equal(early.DateTime, stats.FirstMessageDate);
        Assert.Equal(late.DateTime, stats.LastMessageDate);
    }

    [Fact]
    public void AnalyzeMessages_CountsAttachments()
    {
        List<MediaAttachment> attachments = new List<MediaAttachment>
        {
            new MediaAttachment("photo.jpg", "image/jpeg")
        };

        List<Message> messages = new List<Message>
        {
            MakeMsg("With attachment", MessageDirection.Received, attachments: attachments),
            MakeMsg("Without", MessageDirection.Sent),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.Equal(1, stats.MessagesWithAttachments);
    }

    [Fact]
    public void AnalyzeMessages_CalculatesWordCount()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("one two three", MessageDirection.Received),
            MakeMsg("four five", MessageDirection.Sent),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.Equal(5, stats.TotalWords);
        Assert.Equal(2.5, stats.AverageWordsPerMessage);
    }

    [Fact]
    public void AnalyzeMessages_CalculatesMessageLengths()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Hi", MessageDirection.Received),          // 2 chars
            MakeMsg("Hello World", MessageDirection.Sent),     // 11 chars
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.Equal(2, stats.ShortestMessage);
        Assert.Equal(11, stats.LongestMessage);
    }

    [Fact]
    public void AnalyzeMessages_PopulatesMessagesByHour()
    {
        DateTimeOffset noonUtc = new DateTimeOffset(2023, 6, 15, 12, 0, 0, TimeSpan.Zero);
        List<Message> messages = new List<Message>
        {
            MakeMsg("Noon", MessageDirection.Received, ts: noonUtc),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.True(stats.MessagesByHour.ContainsKey(12));
        Assert.Equal(1, stats.MessagesByHour[12]);
    }

    [Fact]
    public void AnalyzeMessages_PopulatesContactStatistics()
    {
        List<Message> messages = new List<Message>
        {
            MakeMsg("Hello", MessageDirection.Received, "Alice"),
            MakeMsg("Hi", MessageDirection.Received, "Alice"),
            MakeMsg("Hey", MessageDirection.Received, "Bob"),
        };

        MessageStatistics stats = _analyzer.AnalyzeMessages(messages);

        Assert.True(stats.ContactStatistics.ContainsKey("Alice"));
        Assert.Equal(2, stats.ContactStatistics["Alice"].TotalMessages);
    }
}
