using SMSXmlToCsv.Services.ErrorHandling;

namespace SMSXmlToCsv.Tests.Services;

public class ErrorCollectorTests
{
    [Fact]
    public void ErrorCount_IsZero_Initially()
    {
        ErrorCollector collector = new ErrorCollector();

        Assert.Equal(0, collector.ErrorCount);
    }

    [Fact]
    public void AddError_IncrementsCount()
    {
        ErrorCollector collector = new ErrorCollector(continueOnError: true);

        collector.AddError(new InvalidOperationException("test"), "TestOperation");

        Assert.Equal(1, collector.ErrorCount);
    }

    [Fact]
    public void AddError_StoresErrorDetails()
    {
        ErrorCollector collector = new ErrorCollector(continueOnError: true);
        InvalidOperationException ex = new InvalidOperationException("bad thing happened");

        collector.AddError(ex, "MyOperation", "context info");

        ErrorEntry entry = collector.Errors[0];
        Assert.Equal("MyOperation", entry.Operation);
        Assert.Equal("bad thing happened", entry.Message);
        Assert.Equal("context info", entry.Context);
        Assert.Equal("InvalidOperationException", entry.ExceptionType);
    }

    [Fact]
    public void TryExecute_ReturnsTrueOnSuccess()
    {
        ErrorCollector collector = new ErrorCollector();
        bool executed = false;

        bool result = collector.TryExecute(() => { executed = true; }, "TestOp");

        Assert.True(result);
        Assert.True(executed);
        Assert.Equal(0, collector.ErrorCount);
    }

    [Fact]
    public void TryExecute_ReturnsFalse_WhenContinueOnErrorAndThrows()
    {
        ErrorCollector collector = new ErrorCollector(continueOnError: true);

        bool result = collector.TryExecute(() => throw new Exception("boom"), "TestOp");

        Assert.False(result);
        Assert.Equal(1, collector.ErrorCount);
    }

    [Fact]
    public void TryExecute_Rethrows_WhenContinueOnErrorIsFalse()
    {
        ErrorCollector collector = new ErrorCollector(continueOnError: false);

        Assert.Throws<Exception>(() =>
            collector.TryExecute(() => throw new Exception("boom"), "TestOp"));
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsTrueOnSuccess()
    {
        ErrorCollector collector = new ErrorCollector();

        bool result = await collector.TryExecuteAsync(() => Task.CompletedTask, "TestOp");

        Assert.True(result);
        Assert.Equal(0, collector.ErrorCount);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsFalse_WhenContinueOnError()
    {
        ErrorCollector collector = new ErrorCollector(continueOnError: true);

        bool result = await collector.TryExecuteAsync(
            () => throw new Exception("async boom"),
            "TestOp");

        Assert.False(result);
        Assert.Equal(1, collector.ErrorCount);
    }

    [Fact]
    public void Clear_RemovesAllErrors()
    {
        ErrorCollector collector = new ErrorCollector(continueOnError: true);
        collector.AddError(new Exception("one"), "Op1");
        collector.AddError(new Exception("two"), "Op2");

        collector.Clear();

        Assert.Equal(0, collector.ErrorCount);
    }

    [Fact]
    public void SaveErrorReport_WritesFile()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"error_report_{Guid.NewGuid():N}.txt");
        try
        {
            ErrorCollector collector = new ErrorCollector(continueOnError: true, saveErrorReport: true, errorReportPath: tempPath);
            collector.AddError(new InvalidOperationException("test error"), "TestOp");

            collector.SaveErrorReport();

            Assert.True(File.Exists(tempPath));
            string content = File.ReadAllText(tempPath);
            Assert.Contains("test error", content);
            Assert.Contains("TestOp", content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void SaveErrorReport_ThrowsIfNoPathSpecified()
    {
        ErrorCollector collector = new ErrorCollector();

        Assert.Throws<InvalidOperationException>(() => collector.SaveErrorReport());
    }
}
