using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;

namespace MsfsMcpServer.Tools;

/// <summary>
/// MCP tool that retrieves navigation source, OBS, and CDI/GSI state.
/// </summary>
[McpServerToolType]
public sealed class NavigationStatusTool
{
    private const string ToolName = "get_navigation_status";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<NavigationStatusTool> _logger;
    private readonly IToolCallLogger _callLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationStatusTool"/> class.
    /// </summary>
    public NavigationStatusTool(ISimConnectService simConnect, ILogger<NavigationStatusTool> logger, IToolCallLogger callLogger)
    {
        _simConnect = simConnect;
        _logger = logger;
        _callLogger = callLogger;
    }

    /// <summary>
    /// Returns navigation source selection and CDI/GSI information.
    /// </summary>
    [McpServerTool(Name = ToolName), Description("Gets nav source (GPS/VLOC) and CDI/GSI state.")]
    public async Task<NavigationStatusResponse> GetNavigationStatus(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        NavigationStatusResponse response = NavigationStatusResponse.ErrorResponse("An unexpected error occurred.");

        try
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Navigation status request canceled before execution.");
                response = NavigationStatusResponse.ErrorResponse("Request canceled.");
                return response;
            }

            var isConnected = _simConnect.IsConnected;
            if (!isConnected)
            {
                isConnected = await _simConnect.ConnectAsync(ct).ConfigureAwait(false);
            }

            if (!isConnected)
            {
                response = NavigationStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?");
                return response;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(RequestTimeout);

            var data = await _simConnect.RequestDataAsync<NavigationStatusData>(timeoutCts.Token).ConfigureAwait(false);

            if (data == null)
            {
                response = NavigationStatusResponse.ErrorResponse("Unable to retrieve navigation status. Ensure a flight is loaded.");
                return response;
            }

            response = NavigationStatusResponse.FromData(data.Value);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting navigation status.");
            response = NavigationStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Navigation status request canceled.");
            response = NavigationStatusResponse.ErrorResponse("Request canceled.");
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Navigation status request canceled due to timeout.");
            response = NavigationStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting navigation status.");
            response = NavigationStatusResponse.ErrorResponse("An unexpected error occurred.");
            return response;
        }
        finally
        {
            stopwatch.Stop();
            Log(response, stopwatch.Elapsed);
        }

        void Log(NavigationStatusResponse result, TimeSpan elapsed)
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
