using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves primary flight instrument readings.
/// </summary>
[McpServerToolType]
public sealed class FlightInstrumentsTool
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightInstrumentsTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightInstrumentsTool"/> class.
    /// </summary>
    public FlightInstrumentsTool(ISimConnectService simConnect, ILogger<FlightInstrumentsTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    /// <summary>
    /// Returns altimeter, airspeed, and attitude indicator readings.
    /// </summary>
    [McpServerTool(Name = "get_flight_instruments"), Description("Gets primary flight instruments: altimeter, airspeed, and attitude.")]
    public async Task<FlightInstrumentsResponse> GetFlightInstruments(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight instruments request was canceled before execution.");
            return FlightInstrumentsResponse.ErrorResponse("Request canceled.");
        }

        if (!_simConnect.IsConnected)
        {
            return FlightInstrumentsResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            var data = await _simConnect.RequestDataAsync<FlightInstrumentsData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                return FlightInstrumentsResponse.ErrorResponse("Unable to retrieve flight data. Ensure you are in an active flight.");
            }

            return FlightInstrumentsResponse.FromData(data.Value);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight instruments.");
            return FlightInstrumentsResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight instruments request canceled.");
            return FlightInstrumentsResponse.ErrorResponse("Request canceled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Flight instruments request canceled due to timeout.");
            return FlightInstrumentsResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight instruments.");
            return FlightInstrumentsResponse.ErrorResponse("An unexpected error occurred.");
        }
    }
}
