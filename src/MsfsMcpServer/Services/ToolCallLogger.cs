using MsfsMcpServer.Models;

namespace MsfsMcpServer.Services;

/// <summary>
/// In-memory bounded log of recent MCP tool calls for dashboard display.
/// </summary>
public sealed class ToolCallLogger : IToolCallLogger
{
    private const int MaxEntries = 50;

    private readonly LinkedList<ToolCallLogEntry> _entries = new();
    private readonly object _sync = new();

    public void LogSuccess(string toolName, TimeSpan duration) =>
        Add(toolName, duration, success: true, error: null);

    public void LogFailure(string toolName, TimeSpan duration, string error) =>
        Add(toolName, duration, success: false, error: error);

    public IReadOnlyCollection<ToolCallLogEntry> GetRecent(int count)
    {
        lock (_sync)
        {
            return _entries.Take(count).ToArray();
        }
    }

    private void Add(string toolName, TimeSpan duration, bool success, string? error)
    {
        var entry = new ToolCallLogEntry
        {
            Tool = toolName,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            DurationMilliseconds = Math.Round(duration.TotalMilliseconds, 1, MidpointRounding.AwayFromZero),
            Success = success,
            Error = success ? null : error
        };

        lock (_sync)
        {
            _entries.AddFirst(entry);
            while (_entries.Count > MaxEntries)
            {
                _entries.RemoveLast();
            }
        }
    }
}
