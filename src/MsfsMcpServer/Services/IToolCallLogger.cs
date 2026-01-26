using MsfsMcpServer.Models;

namespace MsfsMcpServer.Services;

/// <summary>
/// Records MCP tool invocations for dashboard diagnostics.
/// </summary>
public interface IToolCallLogger
{
    /// <summary>
    /// Records a successful tool call.
    /// </summary>
    /// <param name="toolName">Tool name.</param>
    /// <param name="duration">Execution duration.</param>
    void LogSuccess(string toolName, TimeSpan duration);

    /// <summary>
    /// Records a failed tool call.
    /// </summary>
    /// <param name="toolName">Tool name.</param>
    /// <param name="duration">Execution duration.</param>
    /// <param name="error">Error message.</param>
    void LogFailure(string toolName, TimeSpan duration, string error);

    /// <summary>
    /// Returns the most recent tool calls up to the requested count.
    /// </summary>
    /// <param name="count">Maximum number of entries.</param>
    IReadOnlyCollection<ToolCallLogEntry> GetRecent(int count);
}
