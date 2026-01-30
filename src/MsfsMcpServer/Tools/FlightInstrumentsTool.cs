using System.ComponentModel;
using System.Diagnostics;
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
    private const string ToolName = "get_flight_instruments";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightInstrumentsTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightInstrumentsTool"/> class.
    /// </summary>
    public FlightInstrumentsTool(ISimConnectService simConnect, ILogger<FlightInstrumentsTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns altimeter, airspeed, and attitude indicator readings.
    /// </summary>
    [McpServerTool(Name = "get_flight_instruments"), Description("Gets primary flight instruments: altimeter, airspeed, and attitude.")]
    public async Task<FlightInstrumentsResponse> GetFlightInstruments(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        FlightInstrumentsResponse response = FlightInstrumentsResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Flight instruments request was canceled before execution.");
                response = FlightInstrumentsResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = FlightInstrumentsResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<FlightInstrumentsData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = FlightInstrumentsResponse.ErrorResponse("Unable to retrieve flight data. Ensure you are in an active flight.");
                return response;
            }

            response = FlightInstrumentsResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight instruments.");
            response = FlightInstrumentsResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Flight instruments request canceled.");
            response = FlightInstrumentsResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Flight instruments request canceled due to timeout.");
            response = FlightInstrumentsResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight instruments.");
            response = FlightInstrumentsResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(FlightInstrumentsResponse result, TimeSpan elapsed)
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
