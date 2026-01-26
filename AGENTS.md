# AGENTS.md - MSFS MCP Server

## Project Summary

A Windows system tray app that connects to Microsoft Flight Simulator 2024 via SimConnect and exposes flight data through MCP (Model Context Protocol). AI agents can query real-time flight info for flight instruction use cases. **Read-only** - no aircraft control.

## Tech Stack

- **.NET 8.0** (net8.0-windows)
- **Windows Forms** - System tray host and Windows message loop
- **ASP.NET Core / Kestrel** - HTTP/SSE server for MCP
- **ModelContextProtocol NuGet** - Official C# MCP SDK
- **SimConnect** - MSFS SDK for flight data
- **xUnit + Moq + FluentAssertions** - Testing

## Folder Structure

```
MsfsMcpServer/
├── src/MsfsMcpServer/
│   ├── Program.cs              # Entry point, wires everything together
│   ├── Services/
│   │   ├── ISimConnectService.cs   # Interface for testability
│   │   └── SimConnectService.cs    # Real implementation
│   ├── Tools/                  # MCP tools (one class per tool)
│   │   ├── ConnectionStatusTool.cs
│   │   ├── FlightPositionTool.cs
│   │   └── ...
│   ├── Models/                 # Data structs for SimConnect
│   │   ├── FlightPositionData.cs
│   │   └── ...
│   └── UI/
│       └── TrayApplicationContext.cs
├── tests/MsfsMcpServer.Tests/
│   └── Tools/                  # Mirror structure of src
└── wwwroot/                    # Web dashboard static files
```

## Key Patterns

### MCP Tool Structure

Every tool follows this pattern:

```csharp
[McpServerToolType]
public class FlightPositionTool
{
    private readonly ISimConnectService _simConnect;
    private readonly ILogger<FlightPositionTool> _logger;

    public FlightPositionTool(ISimConnectService simConnect, ILogger<FlightPositionTool> logger)
    {
        _simConnect = simConnect;
        _logger = logger;
    }

    [McpServerTool, Description("Gets current aircraft position, altitude, heading, and speed")]
    public async Task<FlightPositionResponse> GetFlightPosition(CancellationToken ct)
    {
        try
        {
            if (!_simConnect.IsConnected)
            {
                return FlightPositionResponse.Error("Not connected to MSFS");
            }

            var data = await _simConnect.RequestDataAsync<FlightPositionData>(ct);
            
            if (data == null)
            {
                return FlightPositionResponse.Error("Unable to retrieve flight data. Ensure you are in an active flight.");
            }

            return FlightPositionResponse.FromData(data.Value);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout requesting flight position");
            return FlightPositionResponse.Error("Request timed out. MSFS may be loading or on main menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight position");
            return FlightPositionResponse.Error("An unexpected error occurred.");
        }
    }
}
```

### Error Handling

- Always return a response object, never throw from tools
- Use `Response.Error("user-friendly message")` pattern
- Log technical details, return friendly messages
- Handle: disconnected, timeout, null data, unexpected exceptions

### Data Fetching

**On-demand only, no caching.** Each tool call fetches fresh data from SimConnect with a 2-second timeout. This ensures agents never receive stale data from a previous flight/menu state.

### SimConnect Data Structs

```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct FlightPositionData
{
    public double Latitude;
    public double Longitude;
    // ... must match order of AddToDataDefinition calls
}
```

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run app
dotnet run --project src/MsfsMcpServer

# Publish release
dotnet publish src/MsfsMcpServer -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Definition of Done

A task is complete when:

1. Code compiles without warnings
2. Unit tests pass with >80% coverage on new code
3. Manual testing completed (where applicable)
4. Follows established patterns (see Key Patterns above)
5. XML documentation on public members
6. No TODO comments left unresolved
7. Error cases return helpful messages

## Guardrails

**Don't:**
- Write operations - This is read-only. No setting autopilot, no triggering events.
- Caching - Always fetch fresh data on each tool call.
- Blocking the UI thread - SimConnect callbacks are on UI thread; keep handlers fast.
- Swallow exceptions silently - Log everything, return friendly errors.
- Use emojis in documentation - Keep README, code comments, and docs professional.

**Do:**
- Use the ISimConnectService interface - Enables testing without MSFS.
- Include timestamps - Responses should include when data was fetched.
- Handle timeout explicitly - 2 second timeout, clear message when it fires.

## Testing Without MSFS

Use `MockSimConnectService` in tests:

```csharp
var mock = new Mock<ISimConnectService>();
mock.Setup(x => x.IsConnected).Returns(true);
mock.Setup(x => x.RequestDataAsync<FlightPositionData>(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new FlightPositionData { Latitude = 39.85, Longitude = -104.67, ... });

var tool = new FlightPositionTool(mock.Object, NullLogger<FlightPositionTool>.Instance);
```

## Reference Docs

- **Full project spec**: See `PROJECT_SPEC.md` in this repo
- **SimConnect SDK**: https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimConnect/SimConnect_SDK.htm
- **SimVars list**: https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Simulation_Variables.htm  
- **MCP C# SDK**: https://github.com/modelcontextprotocol/csharp-sdk
- **MCP Server Guide**: https://modelcontextprotocol.io/docs/develop/build-server
