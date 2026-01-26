using System.ComponentModel;
using System.Diagnostics;
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
    private const string ToolName = "get_connection_status";
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<ConnectionStatusTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStatusTool"/> class.
    /// </summary>
    public ConnectionStatusTool(ISimConnectService simConnect, ILogger<ConnectionStatusTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns whether SimConnect is reachable.
    /// </summary>
    [McpServerTool(Name = "get_connection_status"), Description("Checks if SimConnect is connected and responsive.")]
    public Task<ConnectionStatusResponse> GetConnectionStatus(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Connection status request was canceled before execution.");
            var canceled = ConnectionStatusResponse.Disconnected("Request canceled.");
            _callLogger.LogFailure(ToolName, stopwatch.Elapsed, canceled.Error ?? "Request canceled.");
            return Task.FromResult(canceled);
        }

        try
        {
            if (!_simConnect.IsConnected)
            {
                var disconnected = ConnectionStatusResponse.Disconnected("SimConnect not available. Is MSFS running?");
                _callLogger.LogFailure(ToolName, stopwatch.Elapsed, disconnected.Error ?? "SimConnect not available.");
                return Task.FromResult(disconnected);
            }

            var connected = ConnectionStatusResponse.ConnectedStatus("Microsoft Flight Simulator 2024");
            _callLogger.LogSuccess(ToolName, stopwatch.Elapsed);
            return Task.FromResult(connected);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Connection status request canceled.");
            var canceled = ConnectionStatusResponse.Disconnected("Request canceled.");
            _callLogger.LogFailure(ToolName, stopwatch.Elapsed, canceled.Error ?? "Request canceled.");
            return Task.FromResult(canceled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection status.");
            var failure = ConnectionStatusResponse.Disconnected("An unexpected error occurred.");
            _callLogger.LogFailure(ToolName, stopwatch.Elapsed, failure.Error ?? "An unexpected error occurred.");
            return Task.FromResult(failure);
        }
    }
}
