using System.Threading;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.UI;

ApplicationConfiguration.Initialize();

const string ServerUrl = "http://localhost:5000";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.WebHost.UseUrls(ServerUrl);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://127.0.0.1:5000",
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddSingleton<SimConnectMessageWindow>();
builder.Services.AddSingleton<ISimConnectService, SimConnectService>();
builder.Services.AddSingleton<IToolCallLogger, ToolCallLogger>();

var app = builder.Build();

app.UseCors("LocalhostCors");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapMcp(pattern: "/mcp");

app.MapGet("/api/status", async (ISimConnectService simConnect, CancellationToken ct) =>
{
    if (!simConnect.IsConnected && !await simConnect.ConnectAsync(ct).ConfigureAwait(false))
    {
        return Results.Json(ConnectionStatusResponse.Disconnected("SimConnect not available. Is MSFS running?"));
    }

    return Results.Json(ConnectionStatusResponse.ConnectedStatus("Microsoft Flight Simulator 2024"));
});

app.MapGet("/api/aircraft", async (ISimConnectService simConnect, CancellationToken ct) =>
{
    if (!simConnect.IsConnected && !await simConnect.ConnectAsync(ct).ConfigureAwait(false))
    {
        return Results.Json(AircraftInfoResponse.ErrorResponse("SimConnect not available. Is MSFS running?"));
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        var data = await simConnect.RequestDataAsync<AircraftInfoData>(timeoutCts.Token).ConfigureAwait(false);
        if (data == null)
        {
            return Results.Json(AircraftInfoResponse.ErrorResponse("Unable to retrieve aircraft info. Ensure you are in an active flight."));
        }

        return Results.Json(AircraftInfoResponse.FromData(data.Value));
    }
    catch (TimeoutException)
    {
        return Results.Json(AircraftInfoResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        return Results.Json(AircraftInfoResponse.ErrorResponse("Request canceled."));
    }
    catch (OperationCanceledException)
    {
        return Results.Json(AircraftInfoResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch
    {
        return Results.Json(AircraftInfoResponse.ErrorResponse("An unexpected error occurred."));
    }
});

app.MapGet("/api/position", async (ISimConnectService simConnect, CancellationToken ct) =>
{
    if (!simConnect.IsConnected && !await simConnect.ConnectAsync(ct).ConfigureAwait(false))
    {
        return Results.Json(FlightPositionResponse.ErrorResponse("SimConnect not available. Is MSFS running?"));
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        var data = await simConnect.RequestDataAsync<FlightPositionData>(timeoutCts.Token).ConfigureAwait(false);
        if (data == null)
        {
            return Results.Json(FlightPositionResponse.ErrorResponse("Unable to retrieve flight data. Ensure you are in an active flight."));
        }

        return Results.Json(FlightPositionResponse.FromData(data.Value));
    }
    catch (TimeoutException)
    {
        return Results.Json(FlightPositionResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        return Results.Json(FlightPositionResponse.ErrorResponse("Request canceled."));
    }
    catch (OperationCanceledException)
    {
        return Results.Json(FlightPositionResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch
    {
        return Results.Json(FlightPositionResponse.ErrorResponse("An unexpected error occurred."));
    }
});

app.MapGet("/api/instruments", async (ISimConnectService simConnect, CancellationToken ct) =>
{
    if (!simConnect.IsConnected && !await simConnect.ConnectAsync(ct).ConfigureAwait(false))
    {
        return Results.Json(FlightInstrumentsResponse.ErrorResponse("SimConnect not available. Is MSFS running?"));
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        var data = await simConnect.RequestDataAsync<FlightInstrumentsData>(timeoutCts.Token).ConfigureAwait(false);
        if (data == null)
        {
            return Results.Json(FlightInstrumentsResponse.ErrorResponse("Unable to retrieve flight data. Ensure you are in an active flight."));
        }

        return Results.Json(FlightInstrumentsResponse.FromData(data.Value));
    }
    catch (TimeoutException)
    {
        return Results.Json(FlightInstrumentsResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        return Results.Json(FlightInstrumentsResponse.ErrorResponse("Request canceled."));
    }
    catch (OperationCanceledException)
    {
        return Results.Json(FlightInstrumentsResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch
    {
        return Results.Json(FlightInstrumentsResponse.ErrorResponse("An unexpected error occurred."));
    }
});

app.MapGet("/api/autopilot", async (ISimConnectService simConnect, CancellationToken ct) =>
{
    if (!simConnect.IsConnected && !await simConnect.ConnectAsync(ct).ConfigureAwait(false))
    {
        return Results.Json(AutopilotStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?"));
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        var data = await simConnect.RequestDataAsync<AutopilotStatusData>(timeoutCts.Token).ConfigureAwait(false);
        if (data == null)
        {
            return Results.Json(AutopilotStatusResponse.ErrorResponse("Unable to retrieve autopilot data. Ensure you are in an active flight."));
        }

        return Results.Json(AutopilotStatusResponse.FromData(data.Value));
    }
    catch (TimeoutException)
    {
        return Results.Json(AutopilotStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        return Results.Json(AutopilotStatusResponse.ErrorResponse("Request canceled."));
    }
    catch (OperationCanceledException)
    {
        return Results.Json(AutopilotStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch
    {
        return Results.Json(AutopilotStatusResponse.ErrorResponse("An unexpected error occurred."));
    }
});

app.MapGet("/api/engine", async (ISimConnectService simConnect, CancellationToken ct) =>
{
    if (!simConnect.IsConnected && !await simConnect.ConnectAsync(ct).ConfigureAwait(false))
    {
        return Results.Json(EngineStatusResponse.ErrorResponse("SimConnect not available. Is MSFS running?"));
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        var data = await simConnect.RequestDataAsync<EngineStatusData>(timeoutCts.Token).ConfigureAwait(false);
        if (data == null)
        {
            return Results.Json(EngineStatusResponse.ErrorResponse("Unable to retrieve engine data. Ensure you are in an active flight."));
        }

        return Results.Json(EngineStatusResponse.FromData(data.Value));
    }
    catch (TimeoutException)
    {
        return Results.Json(EngineStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        return Results.Json(EngineStatusResponse.ErrorResponse("Request canceled."));
    }
    catch (OperationCanceledException)
    {
        return Results.Json(EngineStatusResponse.ErrorResponse("Request timed out. MSFS may be loading or on main menu."));
    }
    catch
    {
        return Results.Json(EngineStatusResponse.ErrorResponse("An unexpected error occurred."));
    }
});

app.MapGet("/api/logs", (IToolCallLogger callLogger) =>
{
    var entries = callLogger.GetRecent(20);
    return Results.Json(entries);
});

using var shutdownCts = new CancellationTokenSource();
var webHostTask = app.RunAsync(shutdownCts.Token);

var serviceProvider = app.Services;
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var programLogger = loggerFactory.CreateLogger("Program");
var simConnect = serviceProvider.GetRequiredService<ISimConnectService>();

var connected = await simConnect.ConnectAsync();
if (!connected)
{
    programLogger.LogWarning("SimConnect connection could not be established. Ensure MSFS is running with the SDK installed.");
}

var shutdownOnce = 0;
async Task ShutdownAsync()
{
    if (Interlocked.Exchange(ref shutdownOnce, 1) == 1)
    {
        return;
    }

    try
    {
        simConnect.Disconnect();
        shutdownCts.Cancel();
        await app.StopAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        programLogger.LogError(ex, "Error during shutdown.");
    }
}

using (var trayContext = new TrayApplicationContext(
           simConnect,
           loggerFactory.CreateLogger<TrayApplicationContext>(),
           new Uri(ServerUrl),
           ShutdownAsync))
{
    Application.Run(trayContext);
}

await ShutdownAsync();

try
{
    await webHostTask.ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Expected when shutting down.
}

await app.DisposeAsync();
