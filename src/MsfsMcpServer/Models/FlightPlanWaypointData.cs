using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw waypoint context (prev/next of active leg).
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct FlightPlanWaypointData
{
    /// <summary>
    /// Active waypoint index.
    /// </summary>
    public int ActiveWaypointIndex;

    /// <summary>
    /// Next waypoint identifier.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string NextWaypointId;

    public double NextWaypointLatitude;
    public double NextWaypointLongitude;
    public double NextWaypointAltitudeFeet;
    public double NextWaypointBearingDegrees;
    public double NextWaypointDistanceNauticalMiles;

    /// <summary>
    /// Previous waypoint identifier.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string PreviousWaypointId;

    public double PreviousWaypointLatitude;
    public double PreviousWaypointLongitude;
    public double PreviousWaypointAltitudeFeet;
    public double PreviousWaypointBearingDegrees;
    public double PreviousWaypointDistanceNauticalMiles;
}
