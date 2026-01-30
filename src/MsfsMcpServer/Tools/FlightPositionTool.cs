using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves current flight position and attitude.
/// </summary>
[McpServerToolType]
public sealed class FlightPositionTool
{
    private const string ToolName = "get_flight_position";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightPositionTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightPositionTool"/> class.
    /// </summary>
    public FlightPositionTool(ISimConnectService simConnect, ILogger<FlightPositionTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns the current aircraft position, speed, and attitude.
    /// </summary>
    [McpServerTool(Name = "get_flight_position"), Description("Gets current aircraft position, altitude, heading, and speed.")]
    public async Task<FlightPositionResponse> GetFlightPosition(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        FlightPositionResponse response = FlightPositionResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Flight position request was canceled before execution.");
                response = FlightPositionResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = FlightPositionResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<FlightPositionData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = FlightPositionResponse.ErrorResponse("Unable to retrieve flight data. Ensure you are in an active flight.");
                return response;
            }

            response = FlightPositionResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight position.");
            response = FlightPositionResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight position request canceled.");
            response = FlightPositionResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Flight position request canceled due to timeout.");
            response = FlightPositionResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight position.");
            response = FlightPositionResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(FlightPositionResponse result, TimeSpan elapsed)
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
