using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves engine status information.
/// </summary>
[McpServerToolType]
public sealed class EngineStatusTool
{
    private const string ToolName = "get_engine_status";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<EngineStatusTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineStatusTool"/> class.
    /// </summary>
    public EngineStatusTool(ISimConnectService simConnect, ILogger<EngineStatusTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns engine RPM, throttle, fuel, and temperature metrics.
    /// </summary>
    [McpServerTool(Name = "get_engine_status"), Description("Gets engine RPM, throttle, fuel, and temperatures.")]
    public async Task<EngineStatusResponse> GetEngineStatus(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        EngineStatusResponse response = EngineStatusResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Engine status request was canceled before execution.");
                response = EngineStatusResponse.ErrorResponse("Request canceled.");
                return response;
            }

            if (!_simConnect.IsConnected)
            {
                response = EngineStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<EngineStatusData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = EngineStatusResponse.ErrorResponse("Unable to retrieve engine data. Ensure you are in an active flight.");
                return response;
            }

            response = EngineStatusResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting engine status.");
            response = EngineStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Engine status request canceled.");
            response = EngineStatusResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Engine status request canceled due to timeout.");
            response = EngineStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engine status.");
            response = EngineStatusResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(EngineStatusResponse result, TimeSpan elapsed)
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
