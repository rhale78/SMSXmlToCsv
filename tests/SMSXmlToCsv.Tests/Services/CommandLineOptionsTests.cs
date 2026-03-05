using SMSXmlToCsv.Services.CLI;

namespace SMSXmlToCsv.Tests.Services;

public class CommandLineOptionsTests
{
    [Fact]
    public void Parse_NoArgs_ReturnsInteractiveMode()
    {
        CommandLineOptions options = CommandLineOptions.Parse(Array.Empty<string>());

        Assert.True(options.Interactive);
    }

    [Fact]
    public void Parse_HelpFlag_SetsShowHelp()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--help" });

        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void Parse_ShortHelpFlag_SetsShowHelp()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "-h" });

        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void Parse_VersionFlag_SetsShowVersion()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--version" });

        Assert.True(options.ShowVersion);
    }

    [Fact]
    public void Parse_InputFlag_SetsInputFile()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--input", "backup.xml" });

        Assert.Equal("backup.xml", options.InputFile);
    }

    [Fact]
    public void Parse_ShortInputFlag_SetsInputFile()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "-i", "backup.xml" });

        Assert.Equal("backup.xml", options.InputFile);
    }

    [Fact]
    public void Parse_OutputFlag_SetsOutputDirectory()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--output", "./exports" });

        Assert.Equal("./exports", options.OutputDirectory);
    }

    [Fact]
    public void Parse_FormatsFlag_SetsExportFormats()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--formats", "csv,html,json" });

        Assert.Equal(3, options.ExportFormats.Count);
        Assert.Contains("csv", options.ExportFormats);
        Assert.Contains("html", options.ExportFormats);
        Assert.Contains("json", options.ExportFormats);
    }

    [Fact]
    public void Parse_StartDateFlag_SetsStartDate()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--start-date", "2024-01-01" });

        Assert.NotNull(options.StartDate);
        Assert.Equal(2024, options.StartDate!.Value.Year);
        Assert.Equal(1, options.StartDate.Value.Month);
        Assert.Equal(1, options.StartDate.Value.Day);
    }

    [Fact]
    public void Parse_EndDateFlag_SetsEndDate()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--end-date", "2024-12-31" });

        Assert.NotNull(options.EndDate);
        Assert.Equal(2024, options.EndDate!.Value.Year);
        Assert.Equal(12, options.EndDate.Value.Month);
        Assert.Equal(31, options.EndDate.Value.Day);
    }

    [Fact]
    public void Parse_ContactsFlag_SetsSelectedContacts()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--contacts", "Alice,Bob,Charlie" });

        Assert.Equal(3, options.SelectedContacts.Count);
        Assert.Contains("Alice", options.SelectedContacts);
        Assert.Contains("Bob", options.SelectedContacts);
        Assert.Contains("Charlie", options.SelectedContacts);
    }

    [Fact]
    public void Parse_ContinueOnErrorFlag_SetsContinueOnError()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--continue-on-error" });

        Assert.True(options.ContinueOnError);
    }

    [Fact]
    public void Parse_ThreadAnalysisFlag_SetsThreadAnalysis()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--thread-analysis" });

        Assert.True(options.EnableThreadAnalysis);
    }

    [Fact]
    public void Parse_StatsFlag_SetsEnableStatistics()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--stats" });

        Assert.True(options.EnableStatistics);
    }

    [Fact]
    public void Parse_InteractiveFlag_SetsInteractive()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--interactive" });

        Assert.True(options.Interactive);
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNonInteractiveAndNoInput()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--output", "./exports", "--formats", "csv" });

        bool valid = options.Validate(out string? error);

        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNoOutputDirectory()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--input", "backup.xml", "--formats", "csv" });

        bool valid = options.Validate(out string? error);

        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNoFormats()
    {
        CommandLineOptions options = CommandLineOptions.Parse(new[] { "--input", "backup.xml", "--output", "./exports" });

        bool valid = options.Validate(out string? error);

        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenInvalidFormat()
    {
        CommandLineOptions options = CommandLineOptions.Parse(
            new[] { "--input", "backup.xml", "--output", "./exports", "--formats", "invalid_format" });

        bool valid = options.Validate(out string? error);

        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ReturnsTrue_WithValidNonInteractiveOptions()
    {
        CommandLineOptions options = CommandLineOptions.Parse(
            new[] { "--input", "backup.xml", "--output", "./exports", "--formats", "csv" });

        bool valid = options.Validate(out string? error);

        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_ReturnsTrue_ForInteractiveMode()
    {
        CommandLineOptions options = CommandLineOptions.Parse(Array.Empty<string>());

        bool valid = options.Validate(out string? error);

        Assert.True(valid);
        Assert.Null(error);
    }
}
