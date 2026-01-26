using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves autopilot modes and targets.
/// </summary>
[McpServerToolType]
public sealed class AutopilotStatusTool
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<AutopilotStatusTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutopilotStatusTool"/> class.
    /// </summary>
    public AutopilotStatusTool(ISimConnectService simConnect, ILogger<AutopilotStatusTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    /// <summary>
    /// Returns autopilot mode states and target values.
    /// </summary>
    [McpServerTool(Name = "get_autopilot_status"), Description("Gets autopilot modes and selected targets.")]
    public async Task<AutopilotStatusResponse> GetAutopilotStatus(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Autopilot status request was canceled before execution.");
            return AutopilotStatusResponse.ErrorResponse("Request canceled.");
        }

        if (!_simConnect.IsConnected)
        {
            return AutopilotStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            var data = await _simConnect.RequestDataAsync<AutopilotStatusData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                return AutopilotStatusResponse.ErrorResponse("Unable to retrieve autopilot data. Ensure you are in an active flight.");
            }

            return AutopilotStatusResponse.FromData(data.Value);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting autopilot status.");
            return AutopilotStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Autopilot status request canceled.");
            return AutopilotStatusResponse.ErrorResponse("Request canceled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Autopilot status request canceled due to timeout.");
            return AutopilotStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autopilot status.");
            return AutopilotStatusResponse.ErrorResponse("An unexpected error occurred.");
        }
    }
}
