namespace MsfsMcpServer.Services;

/// <summary>
/// Abstraction over SimConnect to enable testing without the simulator.
/// </summary>
public interface ISimConnectService
{
    /// <summary>
    /// Gets a value indicating whether the service is currently connected to MSFS.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Attempts to establish a SimConnect session.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when connected; otherwise <c>false</c>.</returns>
    Task<bool> ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Closes any active SimConnect session and releases resources.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Requests typed data from SimConnect.
    /// </summary>
    /// <typeparam name="T">Struct defining the data layout.</typeparam>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The requested data or <c>null</c> when unavailable.</returns>
    Task<T?> RequestDataAsync<T>(CancellationToken ct = default) where T : struct;
}
