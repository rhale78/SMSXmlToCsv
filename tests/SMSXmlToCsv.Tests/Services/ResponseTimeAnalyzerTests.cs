using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Analysis;

namespace SMSXmlToCsv.Tests.Services;

public class ResponseTimeAnalyzerTests
{
    private static Message MakeMsg(MessageDirection dir, DateTimeOffset ts, string fromName = "Alice")
    {
        Contact from = dir == MessageDirection.Sent ? Contact.FromName("Me") : Contact.FromName(fromName);
        Contact to = dir == MessageDirection.Sent ? Contact.FromName(fromName) : Contact.FromName("Me");
        return Message.CreateTextMessage("App", from, to, ts, "Body", dir);
    }

    [Fact]
    public void AnalyzeResponseTimes_ReturnsZero_ForNoMessages()
    {
        ResponseTimeAnalyzer analyzer = new ResponseTimeAnalyzer();

        ResponseTimeReport report = analyzer.AnalyzeResponseTimes(new List<Message>());

        Assert.Equal(0, report.TotalResponses);
    }

    [Fact]
    public void AnalyzeResponseTimes_DetectsResponsePair()
    {
        DateTimeOffset t1 = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = t1.AddMinutes(5);

        List<Message> messages = new List<Message>
        {
            MakeMsg(MessageDirection.Sent, t1),
            MakeMsg(MessageDirection.Received, t2),
        };

        ResponseTimeAnalyzer analyzer = new ResponseTimeAnalyzer();
        ResponseTimeReport report = analyzer.AnalyzeResponseTimes(messages);

        Assert.Equal(1, report.TotalResponses);
        Assert.Equal(TimeSpan.FromMinutes(5), report.AverageResponseTime);
    }

    [Fact]
    public void AnalyzeResponseTimes_ExcludesResponsesOver24Hours()
    {
        DateTimeOffset t1 = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = t1.AddHours(25); // More than 24 hours

        List<Message> messages = new List<Message>
        {
            MakeMsg(MessageDirection.Sent, t1),
            MakeMsg(MessageDirection.Received, t2),
        };

        ResponseTimeAnalyzer analyzer = new ResponseTimeAnalyzer();
        ResponseTimeReport report = analyzer.AnalyzeResponseTimes(messages);

        Assert.Equal(0, report.TotalResponses);
    }

    [Fact]
    public void AnalyzeResponseTimes_DoesNotCountSameDirection()
    {
        DateTimeOffset t1 = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = t1.AddMinutes(5);

        List<Message> messages = new List<Message>
        {
            MakeMsg(MessageDirection.Sent, t1),
            MakeMsg(MessageDirection.Sent, t2), // Same direction — not a response pair
        };

        ResponseTimeAnalyzer analyzer = new ResponseTimeAnalyzer();
        ResponseTimeReport report = analyzer.AnalyzeResponseTimes(messages);

        Assert.Equal(0, report.TotalResponses);
    }

    [Fact]
    public void AnalyzeResponseTimes_CalculatesMinAndMax()
    {
        DateTimeOffset base_ = new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero);

        List<Message> messages = new List<Message>
        {
            MakeMsg(MessageDirection.Sent, base_),
            MakeMsg(MessageDirection.Received, base_.AddMinutes(2)),
            MakeMsg(MessageDirection.Sent, base_.AddMinutes(10)),
            MakeMsg(MessageDirection.Received, base_.AddMinutes(20)), // 10 minute response
        };

        ResponseTimeAnalyzer analyzer = new ResponseTimeAnalyzer();
        ResponseTimeReport report = analyzer.AnalyzeResponseTimes(messages);

        Assert.Equal(TimeSpan.FromMinutes(2), report.MinResponseTime);
        Assert.Equal(TimeSpan.FromMinutes(10), report.MaxResponseTime);
    }
}
