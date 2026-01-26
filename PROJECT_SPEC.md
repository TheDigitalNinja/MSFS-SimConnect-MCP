# MSFS MCP Server - Project Specification

## Project Overview

A Windows system tray application that connects to Microsoft Flight Simulator 2024 via SimConnect and exposes flight data through the Model Context Protocol (MCP). This allows AI agents (Claude, etc.) to query real-time flight information for use cases like flight instruction, situational awareness, and learning.

**Key Use Case**: Flight instruction - an AI agent can see your current flight state and provide contextual guidance without controlling the aircraft.

---

## Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────┐
│                    AI Client (Claude, etc.)             │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTP/SSE (localhost:5000)
┌─────────────────────▼───────────────────────────────────┐
│              MCP Server (ASP.NET Core / Kestrel)        │
│  ┌────────────────────────────────────────────────────┐ │
│  │  MCP Tools: get_position, get_autopilot, etc.      │ │
│  └─────────────────────┬──────────────────────────────┘ │
│                        │ C# method calls                │
│  ┌─────────────────────▼──────────────────────────────┐ │
│  │           SimConnect Service                        │ │
│  │  - Manages connection to MSFS                       │ │
│  │  - On-demand data fetching                          │ │
│  │  - Request/response with timeout                    │ │
│  └─────────────────────┬──────────────────────────────┘ │
│                        │                                │
│  ┌─────────────────────▼──────────────────────────────┐ │
│  │           Web Dashboard (static HTML)               │ │
│  │  - Connection status                                │ │
│  │  - Live flight data display                         │ │
│  │  - MCP call logging                                 │ │
│  └────────────────────────────────────────────────────┘ │
│                                                         │
│  ┌────────────────────────────────────────────────────┐ │
│  │           System Tray Host (Windows Forms)          │ │
│  │  - Windows message loop (required for SimConnect)   │ │
│  │  - Tray icon with status and menu                   │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                      │ Windows Messages
┌─────────────────────▼───────────────────────────────────┐
│              Microsoft Flight Simulator 2024            │
└─────────────────────────────────────────────────────────┘
```

### Key Architecture Decisions

#### 1. On-Demand Data Fetching (Not Cached)

**Decision**: Fetch data from SimConnect only when an MCP tool is called, with a timeout.

**Reasoning**: Cached data can become contextually invalid without any signal:
- User exits to main menu - cache shows mid-flight data
- User loads a different flight - cache shows wrong aircraft/position
- User swaps aircraft - cache shows old fuel/weight/callsign
- MSFS crashes - cache shows healthy flight

With on-demand fetching:
- If MSFS is in-flight: Response in ~50ms
- If MSFS is on menu/loading: Timeout returns clear "unavailable" state
- If MSFS is not running: Connection error is explicit

The agent gets truth or a clear signal that truth isn't available. This is critical for flight instruction where wrong data leads to wrong advice.

#### 2. HTTP/SSE Transport (Not STDIO)

**Decision**: Run an embedded Kestrel web server for MCP communication.

**Reasoning**:
- App is long-running (system tray) - not spawned per-request
- Enables the web dashboard on the same port
- Testable with browser/curl during development
- Multiple clients can connect simultaneously
- Claude Desktop supports HTTP MCP servers

#### 3. Granular Tools (Not One Big Blob)

**Decision**: Separate tools for each data category rather than one `get_all_data` tool.

**Tools**:
- `get_connection_status` - Is MSFS running? Is a flight active?
- `get_flight_position` - Lat, lon, altitude, heading, ground speed, vertical speed, pitch, bank
- `get_flight_instruments` - Indicated altitude, IAS, TAS, Mach, heading indicator, altimeter setting
- `get_engine_status` - RPM, throttle, fuel flow, fuel quantity, temps, pressures
- `get_autopilot_status` - AP master, modes engaged, target values
- `get_aircraft_info` - Aircraft type, callsign, weights

**Reasoning**:
- Agent fetches only what's needed for the question
- Smaller, focused responses use fewer tokens
- Better for instruction: "check your airspeed" only needs instruments
- Each tool has a single responsibility, easier to test

#### 4. Read-Only (No Control)

**Decision**: V1 is strictly read-only. No setting autopilot, tuning radios, or triggering events.

**Reasoning**:
- This is for flight instruction, not AI copilot
- Write operations need extensive validation and safety checks
- Simpler scope, faster to ship
- Can add write tools in V2 if needed

#### 5. System Tray with Web Dashboard

**System Tray UI**:
- Right-click menu:
  - Connection status indicator
  - MCP server status with port
  - "Open Dashboard" link
  - "Settings..." (future)
  - "Exit"
- Double-click: Opens web dashboard in browser
- Tooltip on hover: Quick status (e.g., "Connected - N172SP @ 8,500ft")

**Web Dashboard** (served at same port as MCP):
- Real-time connection status
- Current aircraft info
- Live position/speed/altitude
- Log of recent MCP tool calls (for debugging)

---

## Technical References

### SimConnect SDK

**Documentation**: https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimConnect/SimConnect_SDK.htm

**Key Pages**:
- Simulation Variables: https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Simulation_Variables.htm
- API Reference: Search for `SimConnect_AddToDataDefinition`, `SimConnect_RequestDataOnSimObject`

**SDK Location**: The MSFS 2024 SDK installs to a path stored in environment variable `MSFS2024_SDK`. The managed DLL is at:
```
$(MSFS2024_SDK)\SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll
```

The native DLL (must be distributed with app):
```
$(MSFS2024_SDK)\SimConnect SDK\lib\SimConnect.dll
```

**SimConnect Pattern**:
1. Create `SimConnect` instance with a window handle (for message loop)
2. Call `AddToDataDefinition()` to register which SimVars you want
3. Call `RegisterDataDefineStruct<T>()` to map to a C# struct
4. Call `RequestDataOnSimObject()` to request data
5. Data arrives asynchronously via `OnRecvSimobjectData` callback
6. Must call `ReceiveMessage()` in the Windows message loop to pump messages

### MCP C# SDK

**NuGet Package**: `ModelContextProtocol` (prerelease)
```
dotnet add package ModelContextProtocol --prerelease
dotnet add package ModelContextProtocol.AspNetCore --prerelease
dotnet add package Microsoft.Extensions.Hosting
```

**GitHub**: https://github.com/modelcontextprotocol/csharp-sdk

**Official Docs**: https://modelcontextprotocol.io/docs/develop/build-server

**Key Pattern** - Defining a tool:
```csharp
[McpServerToolType]
public class MyTools
{
    [McpServerTool, Description("Description for the AI")]
    public static string MyTool([Description("Param description")] string param)
    {
        return "result";
    }
}
```

**HTTP/SSE Setup** requires `ModelContextProtocol.AspNetCore` package and configuring Kestrel endpoints.

### Windows Forms System Tray

**Key Classes**:
- `NotifyIcon` - The tray icon
- `ContextMenuStrip` - Right-click menu
- `ApplicationContext` - For running without a visible form

**Pattern**: Create a class inheriting `ApplicationContext`, set up `NotifyIcon` in constructor, run with `Application.Run(context)`.

---

## SimVar Definitions by Tool

### get_connection_status
No SimVars - just checks if SimConnect connection is alive and responsive.

### get_flight_position
| SimVar | Unit | Description |
|--------|------|-------------|
| PLANE LATITUDE | degrees | Current latitude |
| PLANE LONGITUDE | degrees | Current longitude |
| PLANE ALTITUDE | feet | Altitude above mean sea level |
| PLANE HEADING DEGREES TRUE | degrees | True heading |
| GROUND VELOCITY | knots | Ground speed |
| VERTICAL SPEED | feet per minute | Rate of climb/descent |
| PLANE PITCH DEGREES | degrees | Pitch attitude |
| PLANE BANK DEGREES | degrees | Bank angle |

### get_flight_instruments
| SimVar | Unit | Description |
|--------|------|-------------|
| INDICATED ALTITUDE | feet | Altimeter reading |
| AIRSPEED INDICATED | knots | IAS |
| AIRSPEED TRUE | knots | TAS |
| AIRSPEED MACH | mach | Mach number |
| HEADING INDICATOR | degrees | Heading indicator/HSI |
| KOHLSMAN SETTING HG | inHg | Altimeter setting |
| ATTITUDE INDICATOR PITCH DEGREES | degrees | AI pitch |
| ATTITUDE INDICATOR BANK DEGREES | degrees | AI bank |

### get_engine_status
| SimVar | Unit | Description |
|--------|------|-------------|
| NUMBER OF ENGINES | number | Engine count |
| GENERAL ENG RPM:1 | rpm | Engine 1 RPM |
| GENERAL ENG THROTTLE LEVER POSITION:1 | percent | Throttle position |
| ENG FUEL FLOW GPH:1 | gallons per hour | Fuel flow |
| FUEL TOTAL QUANTITY | gallons | Total fuel remaining |
| ENG EXHAUST GAS TEMPERATURE:1 | celsius | EGT |
| ENG OIL PRESSURE:1 | psi | Oil pressure |
| ENG OIL TEMPERATURE:1 | celsius | Oil temp |

### get_autopilot_status
| SimVar | Unit | Description |
|--------|------|-------------|
| AUTOPILOT MASTER | bool | AP on/off |
| AUTOPILOT HEADING LOCK | bool | Heading mode |
| AUTOPILOT HEADING LOCK DIR | degrees | Target heading |
| AUTOPILOT ALTITUDE LOCK | bool | Altitude hold mode |
| AUTOPILOT ALTITUDE LOCK VAR | feet | Target altitude |
| AUTOPILOT AIRSPEED HOLD | bool | Speed hold mode |
| AUTOPILOT AIRSPEED HOLD VAR | knots | Target speed |
| AUTOPILOT VERTICAL HOLD | bool | VS mode |
| AUTOPILOT VERTICAL HOLD VAR | feet per minute | Target VS |
| AUTOPILOT NAV1 LOCK | bool | NAV mode |
| AUTOPILOT APPROACH HOLD | bool | Approach mode |

### get_aircraft_info
| SimVar | Unit | Description |
|--------|------|-------------|
| TITLE | string | Aircraft name |
| ATC ID | string | Tail number |
| ATC AIRLINE | string | Airline name |
| TOTAL WEIGHT | pounds | Current weight |
| MAX GROSS WEIGHT | pounds | Max weight |
| EMPTY WEIGHT | pounds | Empty weight |

---

## Distribution

**Format**: ZIP file containing:
- `MsfsMcpServer.exe` - Main application (self-contained .NET 8)
- `SimConnect.dll` - Native SimConnect library

**Target Framework**: .NET 8.0 Windows (net8.0-windows)

**Build Command**:
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Then zip the exe with SimConnect.dll.

---

## Task List

---

### Phase 0: Project Setup

**Goal**: Get a building solution with all dependencies configured. Nothing runs yet, but the foundation is solid.

**Prerequisites**: None - this is the starting phase.

**Key Context**: 
- Target framework is `net8.0-windows` (Windows Forms for system tray)
- SimConnect DLL comes from MSFS SDK at path in `MSFS2024_SDK` environment variable
- MCP packages are prerelease, use `--prerelease` flag

---

#### Task 0.1: Create Solution Structure
Create the Visual Studio solution with proper project structure:
```
MsfsMcpServer/
├── MsfsMcpServer.sln
├── src/
│   └── MsfsMcpServer/
│       ├── MsfsMcpServer.csproj
│       ├── Program.cs
│       ├── Services/
│       ├── Tools/
│       ├── Models/
│       └── UI/
└── tests/
    └── MsfsMcpServer.Tests/
        └── MsfsMcpServer.Tests.csproj
```

**Acceptance Criteria**:
- Solution opens in Visual Studio / Rider
- Projects build successfully
- Test project references main project

#### Task 0.2: Configure NuGet Dependencies
Add required packages to the main project:
- `ModelContextProtocol` (prerelease)
- `ModelContextProtocol.AspNetCore` (prerelease)
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Logging`

Add test packages:
- `xunit`
- `xunit.runner.visualstudio`
- `Moq`
- `FluentAssertions`

**Acceptance Criteria**:
- `dotnet restore` succeeds
- No package version conflicts

#### Task 0.3: Configure SimConnect Reference
Set up the reference to the managed SimConnect DLL:
- Reference `Microsoft.FlightSimulator.SimConnect.dll` from SDK
- Add post-build step to copy `SimConnect.dll` to output
- Document the `MSFS2024_SDK` environment variable requirement

**Acceptance Criteria**:
- Project builds with SimConnect reference
- SimConnect.dll appears in output directory
- Clear error message if SDK path not found

---

---

### Phase 1: Foundation - SimConnect Service Interface

**Goal**: Create a testable abstraction over SimConnect before implementing the real connection. This lets us build and test tools without MSFS running.

**Prerequisites**: Phase 0 complete (solution builds)

**Key Context**:
- `ISimConnectService` is the interface all tools will depend on
- Data structs must use `[StructLayout(LayoutKind.Sequential)]` for SimConnect marshalling
- Mock implementation enables unit testing without MSFS

---

#### Task 1.1: Define ISimConnectService Interface
Create an interface that abstracts SimConnect operations:
```csharp
public interface ISimConnectService
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(CancellationToken ct = default);
    void Disconnect();
    Task<T?> RequestDataAsync<T>(CancellationToken ct = default) where T : struct;
}
```

**Acceptance Criteria**:
- Interface defined in `Services/` folder
- Supports async operations with cancellation
- Generic method for requesting typed data structs

#### Task 1.2: Define Data Models
Create C# structs/records for each data category that map to SimConnect data definitions:
- `FlightPositionData`
- `FlightInstrumentsData`
- `EngineData`
- `AutopilotData`
- `AircraftInfo`
- `ConnectionStatus`

Use `[StructLayout(LayoutKind.Sequential)]` for SimConnect compatibility.

**Acceptance Criteria**:
- All models defined in `Models/` folder
- Structs have proper layout for SimConnect marshalling
- Properties have XML documentation

#### Task 1.3: Create Mock SimConnect Service
Implement `MockSimConnectService : ISimConnectService` for testing:
- Returns configurable fake data
- Can simulate connection failures
- Can simulate timeouts

**Acceptance Criteria**:
- Mock in test project
- Configurable responses
- Can inject into tools for testing

---

---

### Phase 2: Template Tool Implementation

**Goal**: Build two tools perfectly with full error handling, logging, and tests. These become the template for all other tools. **This is the most important phase** - get it right here and the rest is copy-paste.

**Prerequisites**: Phase 1 complete (ISimConnectService interface and models defined, mock available)

**Key Context**:
- Tools use `[McpServerToolType]` on class and `[McpServerTool]` on methods
- Inject `ISimConnectService` via constructor DI
- Always return response objects, never throw exceptions
- Handle: not connected, timeout, null data, unexpected errors
- See AGENTS.md for the exact code pattern to follow

**Template Tools**:
1. `get_connection_status` - Simplest possible tool, tests infrastructure
2. `get_flight_position` - First real data tool, establishes async request pattern

---

#### Task 2.1: Implement get_connection_status Tool
The simplest tool - checks if SimConnect is connected and responsive.

**Returns**:
```json
{
  "connected": true,
  "simulator": "Microsoft Flight Simulator 2024",
  "error": null
}
```

Or on failure:
```json
{
  "connected": false,
  "simulator": null,
  "error": "SimConnect not available. Is MSFS running?"
}
```

**Implementation Requirements**:
- Use `[McpServerToolType]` and `[McpServerTool]` attributes
- Inject `ISimConnectService` via DI
- Full XML documentation on class and method
- Proper error handling with user-friendly messages
- Logging at appropriate levels

**Acceptance Criteria**:
- Tool discoverable by MCP client
- Returns correct status when connected
- Returns helpful error when disconnected
- Unit tests cover success, failure, and timeout cases

#### Task 2.2: Implement get_flight_position Tool
The first "real" data tool - fetches position data from SimConnect.

**Returns**:
```json
{
  "latitude": 39.8561,
  "longitude": -104.6737,
  "altitude_msl_ft": 8500,
  "heading_true": 270.5,
  "ground_speed_kts": 120,
  "vertical_speed_fpm": 500,
  "pitch_deg": 5.2,
  "bank_deg": -2.1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

Or on failure:
```json
{
  "error": "Unable to retrieve flight data. Ensure you are in an active flight."
}
```

**Implementation Requirements**:
- Same patterns as Task 2.1
- Request data from SimConnect with timeout (2 seconds)
- Handle timeout gracefully with clear error message
- Include timestamp in response
- Round numeric values appropriately (e.g., 1 decimal for degrees)

**Acceptance Criteria**:
- Returns accurate position data when in flight
- Times out gracefully when on main menu
- Unit tests with mock service
- Integration test with actual SimConnect (manual)

#### Task 2.3: Write Comprehensive Tests for Template Tools
Create thorough test coverage:
- Unit tests with mocked SimConnect
- Test success cases
- Test timeout handling
- Test disconnection handling
- Test malformed data handling
- Test cancellation token respect

**Acceptance Criteria**:
- >90% code coverage on template tools
- Tests run in CI without MSFS
- Clear test names describing scenarios

#### Task 2.4: Document Tool Implementation Pattern
Create `TOOL_IMPLEMENTATION_GUIDE.md`:
- Step-by-step guide for adding new tools
- Code snippets and patterns
- Error handling checklist
- Testing checklist

**Acceptance Criteria**:
- Another developer (or AI) can follow guide to add tools
- Includes all necessary boilerplate
- References the template tools as examples

---

---

### Phase 3: SimConnect Implementation

**Goal**: Implement the real SimConnect service that talks to MSFS. Replace the mock with actual flight sim data.

**Prerequisites**: Phase 2 complete (template tools working with mock)

**Key Context**:
- SimConnect requires a Windows message loop (HWND) to receive callbacks
- Data flow: `RequestDataOnSimObject()` → callback via Windows message → `TaskCompletionSource` completes
- Must call `ReceiveMessage()` in message pump to process SimConnect messages
- 2-second timeout on all data requests
- Connection can drop if MSFS exits - handle gracefully

**SimConnect Pattern**:
1. `SimConnect.Open()` with window handle
2. `AddToDataDefinition()` for each SimVar
3. `RegisterDataDefineStruct<T>()` to map to C# struct  
4. `RequestDataOnSimObject()` to request (returns immediately)
5. `OnRecvSimobjectData` callback fires with data
6. Complete the waiting `TaskCompletionSource`

---

#### Task 3.1: Implement Windows Message Loop Host
Create the Windows Forms infrastructure:
- Hidden form or `NativeWindow` to receive SimConnect messages
- Message pump integration
- Thread-safe access from async code

**Acceptance Criteria**:
- SimConnect messages are received
- No UI is shown for this component
- Works alongside the system tray

#### Task 3.2: Implement SimConnectService
Full implementation of `ISimConnectService`:
- Connection management with retry logic
- Data definition registration for all data types
- Async request/response with `TaskCompletionSource`
- Timeout handling (2 second default)
- Proper resource cleanup on disconnect

**Acceptance Criteria**:
- Can connect to running MSFS instance
- Requests return data within expected time
- Timeouts are handled gracefully
- No resource leaks on connect/disconnect cycles

#### Task 3.3: Manual Integration Testing
Test against real MSFS:
- Test connection when MSFS running
- Test connection when MSFS not running
- Test data retrieval in flight
- Test behavior on main menu
- Test behavior during loading screens
- Test aircraft swap
- Test MSFS shutdown while connected

**Acceptance Criteria**:
- All scenarios documented with results
- Known issues logged
- Error messages are helpful for each scenario

---

---

### Phase 4: Remaining Tools

**Goal**: Implement all remaining MCP tools following the established pattern from Phase 2.

**Prerequisites**: Phase 3 complete (real SimConnect working), Phase 2 template tools as reference

**Key Context**:
- Copy the pattern exactly from `FlightPositionTool`
- Each tool gets its own file in `Tools/` folder
- Each tool has a corresponding response class in `Models/`
- Each tool needs unit tests in `Tests/Tools/`
- Refer to PROJECT_SPEC.md "SimVar Definitions by Tool" section for which SimVars each tool needs

**Tools to implement**:
- `get_flight_instruments` - Altimeter, airspeed, attitude indicator readings
- `get_engine_status` - RPM, throttle, fuel, temperatures
- `get_autopilot_status` - AP modes and target values
- `get_aircraft_info` - Aircraft type, callsign, weights

---

#### Task 4.1: Implement get_flight_instruments Tool
Follow pattern from Task 2.2. Returns altimeter, airspeed, attitude data.

#### Task 4.2: Implement get_engine_status Tool
Follow pattern from Task 2.2. Returns engine RPM, fuel, temps.

#### Task 4.3: Implement get_autopilot_status Tool
Follow pattern from Task 2.2. Returns AP modes and targets.

#### Task 4.4: Implement get_aircraft_info Tool
Follow pattern from Task 2.2. Returns aircraft type, callsign, weights.

**Acceptance Criteria for all**:
- Follows established patterns exactly
- Unit tests match template tool coverage
- Manual verification against MSFS

---

---

### Phase 5: MCP Server Setup

**Goal**: Configure the MCP server with HTTP/SSE transport so external clients (Claude Desktop, etc.) can connect.

**Prerequisites**: Phase 4 complete (all tools implemented)

**Key Context**:
- Using `ModelContextProtocol.AspNetCore` package for HTTP transport
- Server runs on `localhost:5000` by default
- SSE (Server-Sent Events) transport for streaming
- Tools are discovered via assembly scanning with `WithToolsFromAssembly()`
- Same Kestrel instance will later serve the web dashboard

**MCP C# SDK Docs**: https://github.com/modelcontextprotocol/csharp-sdk

---

#### Task 5.1: Configure Kestrel and MCP Server
Set up ASP.NET Core hosting:
- Configure Kestrel to listen on `localhost:5000`
- Register MCP server with SSE transport
- Register all tools via assembly scanning
- Configure logging

**Acceptance Criteria**:
- Server starts and listens on configured port
- MCP inspector can connect and see tools
- Tools are callable via MCP protocol

#### Task 5.2: Add CORS Configuration
Configure CORS for local development/testing:
- Allow localhost origins
- Allow required MCP headers

**Acceptance Criteria**:
- Browser-based clients can connect
- No CORS errors in console

#### Task 5.3: Test with Claude Desktop
Configure Claude Desktop to use the MCP server:
```json
{
  "mcpServers": {
    "msfs": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

**Acceptance Criteria**:
- Claude Desktop discovers all tools
- Tools return correct data
- Errors display appropriately

---

---

### Phase 6: System Tray Application

**Goal**: Create the Windows system tray host that ties everything together. App runs minimized to tray with status and controls.

**Prerequisites**: Phase 5 complete (MCP server working)

**Key Context**:
- Use `ApplicationContext` subclass for tray-only app (no main form)
- `NotifyIcon` for tray icon, `ContextMenuStrip` for right-click menu
- Must maintain Windows message loop for SimConnect
- Double-click opens web dashboard in default browser
- Icon should reflect connection state (gray=disconnected, color=connected)

**Windows Forms Classes**:
- `NotifyIcon` - The tray icon itself
- `ContextMenuStrip` - Right-click menu
- `ToolStripMenuItem` - Menu items
- `ApplicationContext` - Run app without visible form

---

#### Task 6.1: Create System Tray ApplicationContext
Implement the tray icon and menu:
- Tray icon with airplane graphic
- Right-click context menu:
  - Status line (connected/disconnected)
  - MCP server status with port
  - Separator
  - "Open Dashboard" menu item
  - Separator
  - "Exit" menu item
- Double-click opens dashboard
- Tooltip shows quick status

**Acceptance Criteria**:
- App starts minimized to tray
- Menu items work correctly
- Icon reflects connection status
- Tooltip updates with flight info

#### Task 6.2: Integrate All Components
Wire together in `Program.cs`:
- Start system tray
- Start MCP server on background thread
- Start SimConnect service
- Handle graceful shutdown

**Acceptance Criteria**:
- Single exe starts everything
- Exit menu item shuts down cleanly
- No orphan processes

#### Task 6.3: Connection Status Updates
Update tray icon and tooltip based on state:
- Gray icon: Not connected
- Colored icon: Connected
- Tooltip: "MSFS MCP Server - Connected - N172SP @ 8,500ft"

**Acceptance Criteria**:
- Visual feedback matches actual state
- Updates within 2 seconds of state change

---

---

### Phase 7: Web Dashboard

**Goal**: Create a simple web dashboard for monitoring status and debugging MCP interactions. Served from the same Kestrel instance as MCP.

**Prerequisites**: Phase 6 complete (system tray working)

**Key Context**:
- Static HTML + vanilla JavaScript (no build step, no framework)
- Served from `wwwroot/` folder via Kestrel static files
- Dashboard at `http://localhost:5000/`
- Can call the same endpoints MCP uses, or add simple REST endpoints
- Auto-refresh data every 2 seconds
- Shows recent MCP tool calls for debugging agent interactions

---

#### Task 7.1: Create Dashboard HTML Page
Single-page dashboard showing:
- Connection status (green/red indicator)
- Current aircraft info
- Live position/speed/altitude (auto-refresh)
- Recent MCP tool call log

**Implementation**:
- Static HTML + vanilla JS
- Fetch from MCP endpoints or add simple REST endpoints
- Auto-refresh every 2 seconds

**Acceptance Criteria**:
- Accessible at `http://localhost:5000/`
- Shows live data when connected
- Shows clear offline state when disconnected

#### Task 7.2: Add MCP Call Logging
Log recent MCP tool invocations:
- Tool name
- Timestamp
- Success/failure
- Response time

Display last 20 calls in dashboard.

**Acceptance Criteria**:
- Calls appear in log within 1 second
- Log doesn't grow unbounded
- Helps debug agent interactions

---

---

### Phase 8: Polish and Distribution

**Goal**: Final polish, error handling review, and create distributable package.

**Prerequisites**: Phase 7 complete (full app working end-to-end)

**Key Context**:
- Distribution is a ZIP containing exe + SimConnect.dll
- Use `dotnet publish` with self-contained and single-file options
- README should enable a new user to get running without other docs
- Test the full flow: extract zip → run exe → connect Claude Desktop → query flight data

**Build Command**:
```bash
dotnet publish src/MsfsMcpServer -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

#### Task 8.1: Add App Icon
Create/source an appropriate icon:
- Airplane or flight-related
- Looks good at 16x16, 32x32, 256x256
- Set as application icon and tray icon

**Acceptance Criteria**:
- Icon visible in tray
- Icon visible in taskbar/alt-tab
- Icon in exe properties

#### Task 8.2: Error Handling Review
Review all error paths:
- Startup errors (port in use, SimConnect not found)
- Runtime errors (MSFS crash, network issues)
- User-friendly messages for all

**Acceptance Criteria**:
- No unhandled exceptions
- All errors logged
- User sees helpful messages

#### Task 8.3: Create Build Script
PowerShell script to:
- Build release version
- Publish self-contained exe
- Copy SimConnect.dll
- Create zip file with version number

**Acceptance Criteria**:
- Single script produces distributable zip
- Zip contains only needed files
- Versioned filename (e.g., `MsfsMcpServer-1.0.0.zip`)

#### Task 8.4: Write README
Documentation covering:
- What this is
- Requirements (MSFS 2024, Windows)
- Installation (extract zip, run exe)
- Configuration (Claude Desktop setup)
- Troubleshooting common issues
- Available tools and what they return

**Acceptance Criteria**:
- New user can get running from README alone
- Troubleshooting covers common issues
- Tool documentation is accurate

---

## Definition of Done

A task is complete when:
1. Code is implemented and compiles without warnings
2. Unit tests pass with >80% coverage on new code
3. Manual testing completed where applicable
4. Code follows established patterns
5. XML documentation on public members
6. No TODO comments left unresolved

---

## Future Considerations (V2)

Not in scope for V1, but worth noting:
- Write operations (set autopilot, tune radios)
- Nearby airport/navaid queries
- Flight plan information
- Weather data
- Multiple simultaneous MSFS connections
- Settings UI for port configuration
- Auto-start with Windows option
- Installer (MSI/MSIX)
