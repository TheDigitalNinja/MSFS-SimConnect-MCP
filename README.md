# MSFS SimConnect MCP

Model Context Protocol (MCP) server for Microsoft Flight Simulator 2024. Exposes real-time flight data to AI agents via SimConnect for use cases like AI-powered flight instruction.

## What It Does

This Windows application sits in your system tray and connects to a running instance of Microsoft Flight Simulator. It exposes flight data through the Model Context Protocol, allowing AI agents (like Claude) to query your current flight state in real-time.

**Example use case:** You're flying an approach and ask Claude "Am I configured correctly for landing?" Claude calls the MCP tools to check your altitude, airspeed, flaps, and autopilot settings, then gives you contextual feedback.

## Available Tools

| Tool | Description |
|------|-------------|
| `get_connection_status` | Check if MSFS is running and connected |
| `get_flight_position` | Position + magnetic heading, GS/VS, pitch/bank, AGL/radio alt, winds, AoA, slip, on-ground |
| `get_flight_instruments` | Indicated altitude, airspeed (IAS/TAS/Mach), heading indicator, altimeter setting |
| `get_engine_status` | RPM/throttle, fuel flow/quantity/weight, temps, N1/N2/torque, fuel pressure, APU |
| `get_autopilot_status` | AP/FD, HDG/ALT/VS/IAS modes, APP/GS/BC, VNAV arm/active, bank/pitch hold, yaw damper, autothrottle |
| `get_aircraft_info` | Aircraft type, tail number, weights |
| `get_flight_plan_leg` | Active GPS plan state, next waypoint bearing/distance/ETE/ETA, XTK, destination ETE/ETA |
| `get_flight_plan_waypoint` | Active leg next/previous waypoint details (ID/lat/lon/alt, bearing, distance) |
| `get_navigation_status` | Nav source (GPS/VLOC), OBS/course, CDI/GSI, LOC/GS availability, NAV1/2 freqs, DME, to/from, magvar |
| `get_approach_status` | Approach loaded/active, segment flags, glidepath/GS deviation |
| `get_aircraft_configuration` | Gear, flaps, spoilers, autobrake, parking brake, trims, exterior lights |

All tools are **read-only**. This server cannot control your aircraft.

## Requirements

- Windows 10/11
- Microsoft Flight Simulator 2024 (may also work with MSFS 2020 - untested)
- MSFS SDK installed (for SimConnect.dll)

## Installation

TODO

## Configuration

### Claude Desktop (Connector UI)

1. Run the server: `dotnet run --project src/MsfsMcpServer` (listens on `http://localhost:5000`).
2. Configure Claude with this JSON (e.g., in `claude_desktop_config.json`):
   ```json
   {
     "mcpServers": {
       "msfs": {
         "command": "npx",
         "args": [
           "mcp-remote",
           "http://localhost:5000/mcp/sse"
         ]
       }
     }
   }
   ```
3. Keep the server running while you use it.

### Other MCP Clients

The server runs on `http://localhost:5000` with SSE transport. Use the SSE endpoint: `http://localhost:5000/mcp/sse`. (Some clients may also accept `http://localhost:5000/mcp`.)

### Quick Start

1. Ensure MSFS is running (ideally in-flight for real data).
2. Start the server: `dotnet run --project src/MsfsMcpServer`
3. Connect your MCP client to `http://localhost:5000/mcp/sse`.
4. Call `get_connection_status` and `get_flight_position`.

## Usage

TODO

## Web Dashboard

The server includes a web dashboard at `http://localhost:5000` for monitoring connection status and viewing live flight data.

TODO - screenshot

## Troubleshooting

**"Not connected to MSFS"**
- Ensure Microsoft Flight Simulator is running
- Check that you're in a flight (not on the main menu)

**"Request timed out"**
- MSFS may be loading or in a menu
- Try again once you're in an active flight

**Server won't start**
- Check if port 5000 is already in use
- Ensure SimConnect.dll is in the same folder as the exe

## License

This project is released under the [Unlicense](LICENSE) - public domain.

## Links

- [SimConnect SDK Documentation](https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimConnect/SimConnect_SDK.htm)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
