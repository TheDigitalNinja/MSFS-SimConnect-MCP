using System.ComponentModel;
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
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightPositionTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightPositionTool"/> class.
    /// </summary>
    public FlightPositionTool(ISimConnectService simConnect, ILogger<FlightPositionTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current aircraft position, speed, and attitude.
    /// </summary>
    [McpServerTool(Name = "get_flight_position"), Description("Gets current aircraft position, altitude, heading, and speed.")]
    public async Task<FlightPositionResponse> GetFlightPosition(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight position request was canceled before execution.");
            return FlightPositionResponse.ErrorResponse("Request canceled.");
        }

        if (!_simConnect.IsConnected)
        {
            return FlightPositionResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            var data = await _simConnect.RequestDataAsync<FlightPositionData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                return FlightPositionResponse.ErrorResponse("Unable to retrieve flight data. Ensure you are in an active flight.");
            }

            return FlightPositionResponse.FromData(data.Value);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight position.");
            return FlightPositionResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight position request canceled.");
            return FlightPositionResponse.ErrorResponse("Request canceled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Flight position request canceled due to timeout.");
            return FlightPositionResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight position.");
            return FlightPositionResponse.ErrorResponse("An unexpected error occurred.");
        }
    }
}
