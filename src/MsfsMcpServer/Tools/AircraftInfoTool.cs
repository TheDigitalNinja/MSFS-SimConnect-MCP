using System.ComponentModel;
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
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<AircraftInfoTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AircraftInfoTool"/> class.
    /// </summary>
    public AircraftInfoTool(ISimConnectService simConnect, ILogger<AircraftInfoTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    /// <summary>
    /// Returns aircraft name, callsign, and weight information.
    /// </summary>
    [McpServerTool(Name = "get_aircraft_info"), Description("Gets aircraft type, callsign, and weight details.")]
    public async Task<AircraftInfoResponse> GetAircraftInfo(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Aircraft info request was canceled before execution.");
            return AircraftInfoResponse.ErrorResponse("Request canceled.");
        }

        if (!_simConnect.IsConnected)
        {
            return AircraftInfoResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            var data = await _simConnect.RequestDataAsync<AircraftInfoData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                return AircraftInfoResponse.ErrorResponse("Unable to retrieve aircraft info. Ensure you are in an active flight.");
            }

            return AircraftInfoResponse.FromData(data.Value);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting aircraft info.");
            return AircraftInfoResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Aircraft info request canceled.");
            return AircraftInfoResponse.ErrorResponse("Request canceled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Aircraft info request canceled due to timeout.");
            return AircraftInfoResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft info.");
            return AircraftInfoResponse.ErrorResponse("An unexpected error occurred.");
        }
    }
}
