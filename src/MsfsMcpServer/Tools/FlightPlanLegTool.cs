using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves active flight plan leg/next waypoint data.
/// </summary>
[McpServerToolType]
public sealed class FlightPlanLegTool
{
    private const string ToolName = "get_flight_plan_leg";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightPlanLegTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightPlanLegTool"/> class.
    /// </summary>
    public FlightPlanLegTool(ISimConnectService simConnect, ILogger<FlightPlanLegTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns the active GPS flight plan leg and next waypoint details.
    /// </summary>
    [McpServerTool(Name = ToolName), Description("Gets active GPS flight plan leg with next waypoint details.")]
    public async Task<FlightPlanLegResponse> GetFlightPlanLeg(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        FlightPlanLegResponse response = FlightPlanLegResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Flight plan leg request was canceled before execution.");
                response = FlightPlanLegResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = FlightPlanLegResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<FlightPlanLegData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = FlightPlanLegResponse.ErrorResponse("Unable to retrieve flight plan data. Ensure a flight plan is loaded and active.");
                return response;
            }

            response = FlightPlanLegResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight plan leg.");
            response = FlightPlanLegResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight plan leg request canceled.");
            response = FlightPlanLegResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Flight plan leg request canceled due to timeout.");
            response = FlightPlanLegResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight plan leg.");
            response = FlightPlanLegResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(FlightPlanLegResponse result, TimeSpan elapsed)
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
