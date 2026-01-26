using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for connection status checks.
/// </summary>
public sealed class ConnectionStatusResponse
{
    /// <summary>
    /// Indicates whether SimConnect is reachable.
    /// </summary>
    [JsonPropertyName("connected")]
    public bool Connected { get; init; }

    /// <summary>
    /// Name of the simulator when connected.
    /// </summary>
    [JsonPropertyName("simulator")]
    public string? Simulator { get; init; }

    /// <summary>
    /// User-friendly error message when not connected.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful connection response.
    /// </summary>
    public static ConnectionStatusResponse ConnectedStatus(string simulator) =>
        new()
        {
            Connected = true,
            Simulator = simulator,
            Error = null
        };

    /// <summary>
    /// Creates a disconnected response with an error reason.
    /// </summary>
    public static ConnectionStatusResponse Disconnected(string message) =>
        new()
        {
            Connected = false,
            Simulator = null,
            Error = message
        };
}
