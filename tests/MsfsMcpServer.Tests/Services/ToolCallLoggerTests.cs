using FluentAssertions;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tests.Services;

public class ToolCallLoggerTests
{
    [Fact]
    public void LogSuccess_StoresEntry()
    {
        var logger = new ToolCallLogger();

        logger.LogSuccess("tool_a", TimeSpan.FromMilliseconds(12.3));

        var entries = logger.GetRecent(10);
        entries.Should().HaveCount(1);
        var entry = entries.First();
        entry.Tool.Should().Be("tool_a");
        entry.Success.Should().BeTrue();
        entry.Error.Should().BeNull();
        entry.DurationMilliseconds.Should().Be(12.3);
    }

    [Fact]
    public void LogFailure_StoresErrorAndDuration()
    {
        var logger = new ToolCallLogger();

        logger.LogFailure("tool_b", TimeSpan.FromMilliseconds(25.6), "boom");

        var entries = logger.GetRecent(10);
        entries.Should().HaveCount(1);
        var entry = entries.First();
        entry.Tool.Should().Be("tool_b");
        entry.Success.Should().BeFalse();
        entry.Error.Should().Be("boom");
        entry.DurationMilliseconds.Should().Be(25.6);
    }

    [Fact]
    public void GetRecent_IsMostRecentFirstAndTrimmed()
    {
        var logger = new ToolCallLogger();

        for (var i = 0; i < 55; i++)
        {
            logger.LogFailure("tool_c", TimeSpan.FromMilliseconds(i), $"err{i}");
        }

        var entries = logger.GetRecent(100);
        entries.Should().HaveCount(50);
        entries.First().Error.Should().Be("err54");
        entries.Last().Error.Should().Be("err5");
    }

    [Fact]
    public void GetRecent_RespectsRequestedCount()
    {
        var logger = new ToolCallLogger();

        logger.LogSuccess("tool_d", TimeSpan.FromMilliseconds(1));
        logger.LogSuccess("tool_d", TimeSpan.FromMilliseconds(2));
        logger.LogSuccess("tool_d", TimeSpan.FromMilliseconds(3));

        var entries = logger.GetRecent(2);
        entries.Should().HaveCount(2);
        entries.First().DurationMilliseconds.Should().Be(3);
        entries.Last().DurationMilliseconds.Should().Be(2);
    }
}
