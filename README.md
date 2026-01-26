# MSFS SimConnect MCP

Model Context Protocol (MCP) server for Microsoft Flight Simulator 2024. Exposes real-time flight data to AI agents via SimConnect for use cases like AI-powered flight instruction.

## What It Does

This Windows application sits in your system tray and connects to a running instance of Microsoft Flight Simulator. It exposes flight data through the Model Context Protocol, allowing AI agents (like Claude) to query your current flight state in real-time.

**Example use case:** You're flying an approach and ask Claude "Am I configured correctly for landing?" Claude calls the MCP tools to check your altitude, airspeed, flaps, and autopilot settings, then gives you contextual feedback.

## Available Tools

| Tool | Description |
|------|-------------|
| `get_connection_status` | Check if MSFS is running and connected |
| `get_flight_position` | Latitude, longitude, altitude, heading, ground speed, vertical speed |
| `get_flight_instruments` | Indicated altitude, airspeed (IAS/TAS/Mach), heading indicator, altimeter setting |
| `get_engine_status` | RPM, throttle position, fuel flow, fuel quantity, temperatures |
| `get_autopilot_status` | AP master, heading/altitude/speed modes and targets |
| `get_aircraft_info` | Aircraft type, tail number, weights |

All tools are **read-only**. This server cannot control your aircraft.

## Requirements

- Windows 10/11
- Microsoft Flight Simulator 2024 (may also work with MSFS 2020 - untested)
- MSFS SDK installed (for SimConnect.dll)

## Installation

TODO

## Configuration

### Claude Desktop (MCP)

Add to your Claude Desktop config (`%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "msfs": {
      "url": "http://localhost:5000/mcp/sse"
    }
  }
}
```

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
