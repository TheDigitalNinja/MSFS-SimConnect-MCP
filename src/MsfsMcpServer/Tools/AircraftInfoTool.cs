using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves aircraft metadata and weights.
/// </summary>
[McpServerToolType]
public sealed class AircraftInfoTool
{
    private const string ToolName = "get_aircraft_info";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<AircraftInfoTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AircraftInfoTool"/> class.
    /// </summary>
    public AircraftInfoTool(ISimConnectService simConnect, ILogger<AircraftInfoTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns aircraft name, callsign, and weight information.
    /// </summary>
    [McpServerTool(Name = "get_aircraft_info"), Description("Gets aircraft type, callsign, and weight details.")]
    public async Task<AircraftInfoResponse> GetAircraftInfo(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        AircraftInfoResponse response = AircraftInfoResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Aircraft info request was canceled before execution.");
                response = AircraftInfoResponse.ErrorResponse("Request canceled.");
                return response;
            }

            if (!_simConnect.IsConnected)
            {
                response = AircraftInfoResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<AircraftInfoData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = AircraftInfoResponse.ErrorResponse("Unable to retrieve aircraft info. Ensure you are in an active flight.");
                return response;
            }

            response = AircraftInfoResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting aircraft info.");
            response = AircraftInfoResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Aircraft info request canceled.");
            response = AircraftInfoResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Aircraft info request canceled due to timeout.");
            response = AircraftInfoResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft info.");
            response = AircraftInfoResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(AircraftInfoResponse result, TimeSpan elapsed)
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
