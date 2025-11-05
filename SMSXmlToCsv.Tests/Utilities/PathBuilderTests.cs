using SMSXmlToCsv.Core.Models;
using SMSXmlToCsv.Core.Utilities;

namespace SMSXmlToCsv.Tests.Utilities;

public class PathBuilderTests
{
    [Fact]
    public void BuildPath_WithDatePlaceholder_ReplacesWithCurrentDate()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "exports/{date}/messages";
        var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Act
        var result = pathBuilder.BuildPath(template);

        // Assert
        Assert.Contains(expectedDate, result);
        Assert.Equal($"exports/{expectedDate}/messages", result);
    }

    [Fact]
    public void BuildPath_WithTimePlaceholder_ReplacesWithCurrentTime()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "exports/{time}";
        
        // Act
        var result = pathBuilder.BuildPath(template);

        // Assert
        Assert.StartsWith("exports/", result);
        Assert.Matches(@"exports/\d{2}-\d{2}-\d{2}", result);
    }

    [Fact]
    public void BuildPath_WithDateTimePlaceholder_ReplacesWithCurrentDateTime()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "exports/{datetime}";
        
        // Act
        var result = pathBuilder.BuildPath(template);

        // Assert
        Assert.StartsWith("exports/", result);
        Assert.Matches(@"exports/\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}", result);
    }

    [Fact]
    public void BuildPath_WithYearMonthDayPlaceholders_ReplacesIndividually()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "{year}/{month}/{day}";
        var now = DateTime.Now;
        var expectedYear = now.ToString("yyyy");
        var expectedMonth = now.ToString("MM");
        var expectedDay = now.ToString("dd");

        // Act
        var result = pathBuilder.BuildPath(template);

        // Assert
        Assert.Equal($"{expectedYear}/{expectedMonth}/{expectedDay}", result);
    }

    [Fact]
    public void BuildPath_WithContactNamePlaceholder_ReplacesWithContactName()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "contacts/{contact_name}/messages";
        var contact = new Contact { Name = "John Doe" };

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Equal("contacts/John Doe/messages", result);
    }

    [Fact]
    public void BuildPath_WithContactIdPlaceholder_ReplacesWithContactId()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "contacts/{contact_id}";
        var contact = new Contact { Id = "12345" };

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Equal("contacts/12345", result);
    }

    [Fact]
    public void BuildPath_WithPhoneNumberPlaceholder_ReplacesWithPhoneNumber()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "contacts/{phone_number}";
        var contact = new Contact { PhoneNumber = "+1234567890" };

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Equal("contacts/+1234567890", result);
    }

    [Fact]
    public void BuildPath_WithInvalidCharactersInContactName_SanitizesName()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "contacts/{contact_name}";
        var contact = new Contact { Name = "John/Doe:Test*" };

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Equal("contacts/John_Doe_Test_", result);
        Assert.DoesNotContain("/", result.Replace("contacts/", ""));
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("*", result);
    }

    [Fact]
    public void BuildPath_WithMultiplePlaceholders_ReplacesAll()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "exports/{date}/{contact_name}/{time}";
        var contact = new Contact { Name = "Jane Smith" };
        var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Contains(expectedDate, result);
        Assert.Contains("Jane Smith", result);
        Assert.StartsWith($"exports/{expectedDate}/Jane Smith/", result);
    }

    [Fact]
    public void BuildPath_WithNullContact_OnlyReplacesDateTimePlaceholders()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "exports/{date}/{contact_name}";
        var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Act
        var result = pathBuilder.BuildPath(template, null);

        // Assert
        Assert.Equal($"exports/{expectedDate}/{{contact_name}}", result);
    }

    [Fact]
    public void BuildPath_WithEmptyTemplate_ThrowsArgumentException()
    {
        // Arrange
        var pathBuilder = new PathBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pathBuilder.BuildPath(""));
    }

    [Fact]
    public void BuildPath_WithNullTemplate_ThrowsArgumentException()
    {
        // Arrange
        var pathBuilder = new PathBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pathBuilder.BuildPath(null!));
    }

    [Fact]
    public void BuildPath_WithSpecificDateTime_UsesProvidedDateTime()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "{date}_{time}";
        var specificDate = new DateTime(2023, 12, 25, 14, 30, 45);

        // Act
        var result = pathBuilder.BuildPath(template, specificDate);

        // Assert
        Assert.Equal("2023-12-25_14-30-45", result);
    }

    [Fact]
    public void BuildPath_WithSpecificDateTimeAndContact_ReplacesAllPlaceholders()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "{year}/{month}/{contact_name}_{datetime}";
        var specificDate = new DateTime(2023, 12, 25, 14, 30, 45);
        var contact = new Contact { Name = "Alice" };

        // Act
        var result = pathBuilder.BuildPath(template, specificDate, contact);

        // Assert
        Assert.Equal("2023/12/Alice_2023-12-25_14-30-45", result);
    }

    [Fact]
    public void BuildPath_WithEmptyContactName_UsesUnknown()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "contacts/{contact_name}";
        var contact = new Contact { Name = "" };

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Equal("contacts/Unknown", result);
    }

    [Fact]
    public void BuildPath_WithWhitespaceContactName_TrimsAndUsesUnknown()
    {
        // Arrange
        var pathBuilder = new PathBuilder();
        var template = "contacts/{contact_name}";
        var contact = new Contact { Name = "   " };

        // Act
        var result = pathBuilder.BuildPath(template, contact);

        // Assert
        Assert.Equal("contacts/Unknown", result);
    }
}
