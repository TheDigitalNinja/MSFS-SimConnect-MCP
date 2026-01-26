using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that reports current SimConnect connection status.
/// </summary>
[McpServerToolType]
public sealed class ConnectionStatusTool
{
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<ConnectionStatusTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStatusTool"/> class.
    /// </summary>
    public ConnectionStatusTool(ISimConnectService simConnect, ILogger<ConnectionStatusTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    /// <summary>
    /// Returns whether SimConnect is reachable.
    /// </summary>
    [McpServerTool(Name = "get_connection_status"), Description("Checks if SimConnect is connected and responsive.")]
    public Task<ConnectionStatusResponse> GetConnectionStatus(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Connection status request was canceled before execution.");
            return Task.FromResult(ConnectionStatusResponse.Disconnected("Request canceled."));
        }

        try
        {
            if (!_simConnect.IsConnected)
            {
                return Task.FromResult(ConnectionStatusResponse.Disconnected("SimConnect not available. Is MSFS running?"));
            }

            return Task.FromResult(ConnectionStatusResponse.ConnectedStatus("Microsoft Flight Simulator 2024"));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Connection status request canceled.");
            return Task.FromResult(ConnectionStatusResponse.Disconnected("Request canceled."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection status.");
            return Task.FromResult(ConnectionStatusResponse.Disconnected("An unexpected error occurred."));
        }
    }
}
