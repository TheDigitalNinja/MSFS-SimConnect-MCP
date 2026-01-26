using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves engine status information.
/// </summary>
[McpServerToolType]
public sealed class EngineStatusTool
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<EngineStatusTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineStatusTool"/> class.
    /// </summary>
    public EngineStatusTool(ISimConnectService simConnect, ILogger<EngineStatusTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    /// <summary>
    /// Returns engine RPM, throttle, fuel, and temperature metrics.
    /// </summary>
    [McpServerTool(Name = "get_engine_status"), Description("Gets engine RPM, throttle, fuel, and temperatures.")]
    public async Task<EngineStatusResponse> GetEngineStatus(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Engine status request was canceled before execution.");
            return EngineStatusResponse.ErrorResponse("Request canceled.");
        }

        if (!_simConnect.IsConnected)
        {
            return EngineStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            var data = await _simConnect.RequestDataAsync<EngineStatusData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                return EngineStatusResponse.ErrorResponse("Unable to retrieve engine data. Ensure you are in an active flight.");
            }

            return EngineStatusResponse.FromData(data.Value);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting engine status.");
            return EngineStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Engine status request canceled.");
            return EngineStatusResponse.ErrorResponse("Request canceled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Engine status request canceled due to timeout.");
            return EngineStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engine status.");
            return EngineStatusResponse.ErrorResponse("An unexpected error occurred.");
        }
    }
}
