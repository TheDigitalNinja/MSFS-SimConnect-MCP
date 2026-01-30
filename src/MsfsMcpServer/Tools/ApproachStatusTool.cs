using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves GPS approach state and glidepath availability.
/// </summary>
[McpServerToolType]
public sealed class ApproachStatusTool
{
    private const string ToolName = "get_approach_status";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<ApproachStatusTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproachStatusTool"/> class.
    /// </summary>
    public ApproachStatusTool(ISimConnectService simConnect, ILogger<ApproachStatusTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns approach load/active state and glidepath/glideslope indicators.
    /// </summary>
    [McpServerTool(Name = ToolName), Description("Gets approach load/active state and glidepath/glideslope status.")]
    public async Task<ApproachStatusResponse> GetApproachStatus(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        ApproachStatusResponse response = ApproachStatusResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Approach status request canceled before execution.");
                response = ApproachStatusResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = ApproachStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<ApproachStatusData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = ApproachStatusResponse.ErrorResponse("Unable to retrieve approach data. Ensure a flight is loaded.");
                return response;
            }

            response = ApproachStatusResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting approach status.");
            response = ApproachStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Approach status request canceled.");
            response = ApproachStatusResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Approach status request canceled due to timeout.");
            response = ApproachStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approach status.");
            response = ApproachStatusResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(ApproachStatusResponse result, TimeSpan elapsed)
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
