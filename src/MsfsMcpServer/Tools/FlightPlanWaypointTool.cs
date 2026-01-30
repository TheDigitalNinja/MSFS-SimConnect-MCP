using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that exposes prev/next waypoint details relative to the active GPS leg.
/// </summary>
[McpServerToolType]
public sealed class FlightPlanWaypointTool
{
    private const string ToolName = "get_flight_plan_waypoint";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightPlanWaypointTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightPlanWaypointTool"/> class.
    /// </summary>
    public FlightPlanWaypointTool(ISimConnectService simConnect, ILogger<FlightPlanWaypointTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns waypoint details for the active leg's next or previous waypoint.
    /// Only the active leg prev/next can be read via SimVars.
    /// </summary>
    /// <param name="index">Waypoint index to read (must be active index or active index - 1).</param>
    [McpServerTool(Name = ToolName), Description("Gets waypoint details for the active leg (prev/next only).")]
    public async Task<FlightPlanWaypointResponse> GetFlightPlanWaypoint(int index, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        FlightPlanWaypointResponse response = FlightPlanWaypointResponse.ErrorResponse(index, "An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Flight plan waypoint request canceled before execution.");
                response = FlightPlanWaypointResponse.ErrorResponse(index, "Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = FlightPlanWaypointResponse.ErrorResponse(index, "SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<FlightPlanWaypointData>(timeoutCts.Token).ConfigureAwait(false);
            if (data == null)
            {
                response = FlightPlanWaypointResponse.ErrorResponse(index, "Unable to retrieve waypoint data. Ensure a flight plan is active.");
                return response;
            }

            var activeIndex = data.Value.ActiveWaypointIndex;
            if (index == activeIndex)
            {
                response = FlightPlanWaypointResponse.FromNext(index, data.Value);
                return response;
            }

            if (index == activeIndex - 1)
            {
                response = FlightPlanWaypointResponse.FromPrevious(index, data.Value);
                return response;
            }

            response = FlightPlanWaypointResponse.ErrorResponse(
                index,
                "Only the active leg waypoints (active and previous) are exposed by GPS SimVars.");
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight plan waypoint.");
            response = FlightPlanWaypointResponse.ErrorResponse(index, "Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight plan waypoint request canceled.");
            response = FlightPlanWaypointResponse.ErrorResponse(index, "Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Flight plan waypoint request canceled due to timeout.");
            response = FlightPlanWaypointResponse.ErrorResponse(index, "Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight plan waypoint for index {Index}.", index);
            response = FlightPlanWaypointResponse.ErrorResponse(index, "An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(FlightPlanWaypointResponse result, TimeSpan elapsed)
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
