using SMSXmlToCsv.Models;
using SMSXmlToCsv.Services.Filtering;

namespace SMSXmlToCsv.Tests.Services;

public class DateRangeFilterTests
{
    private static Message MakeMsg(DateTimeOffset timestamp)
    {
        Contact from = Contact.FromName("Alice");
        Contact to = Contact.FromName("Me");
        return Message.CreateTextMessage("App", from, to, timestamp, "Body", MessageDirection.Received);
    }

    [Fact]
    public void Filter_ReturnsAllMessages_WhenDisabled()
    {
        DateRangeFilter filter = new DateRangeFilter(false, null, null);
        List<Message> messages = new List<Message>
        {
            MakeMsg(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            MakeMsg(new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.Zero)),
            MakeMsg(new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero)),
        };

        IEnumerable<Message> result = filter.Filter(messages);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public void Filter_AppliesStartDate()
    {
        DateTime start = new DateTime(2023, 1, 1);
        DateRangeFilter filter = new DateRangeFilter(true, start, null);
        List<Message> messages = new List<Message>
        {
            MakeMsg(new DateTimeOffset(2022, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            MakeMsg(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            MakeMsg(new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero)),
        };

        IEnumerable<Message> result = filter.Filter(messages);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Filter_AppliesEndDate_IncludesEntireDay()
    {
        DateTime end = new DateTime(2023, 12, 31);
        DateRangeFilter filter = new DateRangeFilter(true, null, end);
        List<Message> messages = new List<Message>
        {
            MakeMsg(new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero)), // same day, late
            MakeMsg(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),      // next day
        };

        IEnumerable<Message> result = filter.Filter(messages);

        Assert.Single(result);
    }

    [Fact]
    public void Filter_AppliesBothStartAndEndDate()
    {
        DateTime start = new DateTime(2023, 1, 1);
        DateTime end = new DateTime(2023, 12, 31);
        DateRangeFilter filter = new DateRangeFilter(true, start, end);
        List<Message> messages = new List<Message>
        {
            MakeMsg(new DateTimeOffset(2022, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            MakeMsg(new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.Zero)),
            MakeMsg(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        };

        IEnumerable<Message> result = filter.Filter(messages);

        Assert.Single(result);
    }

    [Fact]
    public void IsInRange_ReturnsTrueWhenDisabled()
    {
        DateRangeFilter filter = new DateRangeFilter(false, null, null);
        Message msg = MakeMsg(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.True(filter.IsInRange(msg));
    }

    [Fact]
    public void IsInRange_ReturnsFalse_WhenBeforeStart()
    {
        DateTime start = new DateTime(2023, 1, 1);
        DateRangeFilter filter = new DateRangeFilter(true, start, null);
        Message msg = MakeMsg(new DateTimeOffset(2022, 12, 31, 0, 0, 0, TimeSpan.Zero));

        Assert.False(filter.IsInRange(msg));
    }

    [Fact]
    public void IsInRange_ReturnsTrue_WhenOnStartDate()
    {
        DateTime start = new DateTime(2023, 1, 1);
        DateRangeFilter filter = new DateRangeFilter(true, start, null);
        Message msg = MakeMsg(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.True(filter.IsInRange(msg));
    }

    [Fact]
    public void IsInRange_ReturnsFalse_WhenAfterEnd()
    {
        DateTime end = new DateTime(2023, 12, 31);
        DateRangeFilter filter = new DateRangeFilter(true, null, end);
        Message msg = MakeMsg(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.False(filter.IsInRange(msg));
    }

    [Fact]
    public void IsEnabled_IsTrue_WhenCreatedWithEnabled()
    {
        DateRangeFilter filter = new DateRangeFilter(true, null, null);

        Assert.True(filter.IsEnabled);
    }

    [Fact]
    public void IsEnabled_IsFalse_WhenCreatedWithDisabled()
    {
        DateRangeFilter filter = new DateRangeFilter(false, null, null);

        Assert.False(filter.IsEnabled);
    }
}
