using System.ComponentModel;
using System.Diagnostics;
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
    private const string ToolName = "get_autopilot_status";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<AutopilotStatusTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutopilotStatusTool"/> class.
    /// </summary>
    public AutopilotStatusTool(ISimConnectService simConnect, ILogger<AutopilotStatusTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns autopilot mode states and target values.
    /// </summary>
    [McpServerTool(Name = "get_autopilot_status"), Description("Gets autopilot modes and selected targets.")]
    public async Task<AutopilotStatusResponse> GetAutopilotStatus(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        AutopilotStatusResponse response = AutopilotStatusResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Autopilot status request was canceled before execution.");
                response = AutopilotStatusResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = AutopilotStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<AutopilotStatusData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = AutopilotStatusResponse.ErrorResponse("Unable to retrieve autopilot data. Ensure you are in an active flight.");
                return response;
            }

            response = AutopilotStatusResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting autopilot status.");
            response = AutopilotStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Autopilot status request canceled.");
            response = AutopilotStatusResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Autopilot status request canceled due to timeout.");
            response = AutopilotStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autopilot status.");
            response = AutopilotStatusResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(AutopilotStatusResponse result, TimeSpan elapsed)
        {
            if (result.Error is null)
            {
                _callLogger.LogSuccess(ToolName, elapsed);
            }
            else
            {
                _callLogger.LogFailure(ToolName, elapsed, result.Error);
            }
        }
    }
}
