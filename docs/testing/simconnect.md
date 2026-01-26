# SimConnect Manual Test Checklist

> Run these with MSFS 2024 and the SDK SimConnect libraries available (environment variable `MSFS2024_SDK` set). The app must be built and launched so the hidden message window can receive SimConnect callbacks.

## Scenarios

1) **MSFS not running**
   - Start the app without MSFS open.
   - Expected: `get_connection_status` reports disconnected with message “SimConnect not available. Is MSFS running?”.
   - Expected: `get_flight_position` returns error with same message; no crash.

2) **MSFS main menu**
   - Launch MSFS to main menu, then start the app.
   - Expected: connection succeeds.
   - Expected: `get_flight_position` times out after ~2s with “Request timed out. MSFS may be loading or on main menu.”.

3) **Active flight (stable)**
   - Start a flight and wait until aircraft is loaded.
   - Expected: `get_connection_status` returns connected and simulator name.
   - Expected: `get_flight_position` returns non-zero values; timestamp present; values roughly match in-sim location/speed/altitude.

4) **Loading screen / heavy load**
   - Trigger a long load (e.g., move to new airport or switch weather preset).
   - Expected: some requests may time out with friendly message; app should not hang or crash; subsequent requests recover once sim resumes.

5) **Aircraft swap**
   - Change aircraft mid-session.
   - Expected: connection stays alive; subsequent `get_flight_position` returns updated data for new aircraft.

6) **MSFS shutdown while connected**
   - With the app running and connected, close MSFS.
   - Expected: service logs quit; pending requests fail gracefully; `get_connection_status` flips to disconnected on next call; further requests return friendly error until reconnection succeeds.

7) **Reconnect after MSFS restart**
   - Restart MSFS and call `get_connection_status` / `get_flight_position`.
   - Expected: automatic reconnect succeeds or manual reconnect via tool call succeeds without app restart.

## Notes for logging

- Capture timestamps for each scenario.
- Note any unhandled exceptions or repeated timeouts.
- Record the approximate latency of successful `get_flight_position` calls (goal: under 2 seconds).

## Known issues (update as found)

- _None logged yet._
