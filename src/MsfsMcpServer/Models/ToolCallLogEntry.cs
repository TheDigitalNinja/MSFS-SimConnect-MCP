using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Represents a single MCP tool invocation for dashboard diagnostics.
/// </summary>
public sealed class ToolCallLogEntry
{
    [JsonPropertyName("tool")]
    public string Tool { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("duration_ms")]
    public double DurationMilliseconds { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
