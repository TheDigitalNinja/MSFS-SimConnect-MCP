using System.Threading;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
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

var app = builder.Build();

app.UseCors("LocalhostCors");
app.MapMcp(pattern: "/mcp");

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
