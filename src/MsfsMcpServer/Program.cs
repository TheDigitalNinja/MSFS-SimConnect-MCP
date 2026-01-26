using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using MsfsMcpServer.Services;
using MsfsMcpServer.UI;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.WebHost.UseUrls("http://localhost:5000");

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

var webHostTask = app.RunAsync();

var simConnect = app.Services.GetRequiredService<ISimConnectService>();
var programLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var connected = await simConnect.ConnectAsync();
if (!connected)
{
    programLogger.LogWarning("SimConnect connection could not be established. Ensure MSFS is running with the SDK installed.");
}

Application.Run(new ApplicationContext());

simConnect.Disconnect();
await app.StopAsync();
await webHostTask;
await app.DisposeAsync();
