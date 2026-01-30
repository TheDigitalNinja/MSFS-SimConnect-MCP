using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that returns aircraft configuration (gear/flaps/spoilers/brakes/lights/trim).
/// </summary>
[McpServerToolType]
public sealed class AircraftConfigurationTool
{
    private const string ToolName = "get_aircraft_configuration";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<AircraftConfigurationTool> _logger;
    private readonly IToolCallLogger _callLogger;

    public AircraftConfigurationTool(ISimConnectService simConnect, ILogger<AircraftConfigurationTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns current gear/flaps/spoilers/brakes/trim/lights state.
    /// </summary>
    [McpServerTool(Name = ToolName), Description("Gets gear, flaps, spoilers, brakes, trim, and exterior lights.")]
    public async Task<AircraftConfigurationResponse> GetAircraftConfiguration(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        AircraftConfigurationResponse response = AircraftConfigurationResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Aircraft configuration request was canceled before execution.");
                response = AircraftConfigurationResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = AircraftConfigurationResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<AircraftConfigurationData>(timeoutCts.Token).ConfigureAwait(false);
            if (data == null)
            {
                response = AircraftConfigurationResponse.ErrorResponse("Unable to retrieve configuration data. Ensure you are in an active flight.");
                return response;
            }

            response = AircraftConfigurationResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting aircraft configuration.");
            response = AircraftConfigurationResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Aircraft configuration request canceled.");
            response = AircraftConfigurationResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Aircraft configuration request canceled due to timeout.");
            response = AircraftConfigurationResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aircraft configuration.");
            response = AircraftConfigurationResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            if (response.Error is null)
            {
                _callLogger.LogSuccess(ToolName, stopwatch.Elapsed);
            }
            else
            {
                _callLogger.LogFailure(ToolName, stopwatch.Elapsed, response.Error);
            }
        }
    }
}
