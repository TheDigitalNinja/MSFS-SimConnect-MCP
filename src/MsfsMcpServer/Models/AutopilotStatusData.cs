using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw autopilot status data returned from SimConnect.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct AutopilotStatusData
{
    /// <summary>
    /// Autopilot master on/off.
    /// </summary>
    public int AutopilotMaster;

    /// <summary>
    /// Heading hold mode engaged.
    /// </summary>
    public int HeadingLock;

    /// <summary>
    /// Selected heading, degrees.
    /// </summary>
    public double HeadingLockDegrees;

    /// <summary>
    /// Altitude hold mode engaged.
    /// </summary>
    public int AltitudeLock;

    /// <summary>
    /// Selected altitude, feet.
    /// </summary>
    public double AltitudeLockFeet;

    /// <summary>
    /// Airspeed hold mode engaged.
    /// </summary>
    public int AirspeedHold;

    /// <summary>
    /// Selected airspeed, knots.
    /// </summary>
    public double AirspeedHoldKnots;

    /// <summary>
    /// Vertical speed hold mode engaged.
    /// </summary>
    public int VerticalHold;

    /// <summary>
    /// Selected vertical speed, feet per minute.
    /// </summary>
    public double VerticalHoldFpm;

    /// <summary>
    /// NAV mode engaged.
    /// </summary>
    public int NavLock;

    /// <summary>
    /// Approach mode engaged.
    /// </summary>
    public int ApproachHold;
}
