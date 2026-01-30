using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw flight plan leg data returned from SimConnect GPS SimVars.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct FlightPlanLegData
{
    /// <summary>
    /// Indicates if a GPS flight plan is active.
    /// </summary>
    public int IsFlightPlanActive;

    /// <summary>
    /// Total waypoint count in the active flight plan.
    /// </summary>
    public int WaypointCount;

    /// <summary>
    /// Index of the active waypoint.
    /// </summary>
    public int ActiveWaypointIndex;

    /// <summary>
    /// Next waypoint identifier.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string NextWaypointId;

    /// <summary>
    /// Next waypoint latitude in decimal degrees.
    /// </summary>
    public double NextWaypointLatitude;

    /// <summary>
    /// Next waypoint longitude in decimal degrees.
    /// </summary>
    public double NextWaypointLongitude;

    /// <summary>
    /// Next waypoint altitude, feet.
    /// </summary>
    public double NextWaypointAltitudeFeet;

    /// <summary>
    /// Bearing to next waypoint, degrees.
    /// </summary>
    public double NextWaypointBearingDegrees;

    /// <summary>
    /// Desired track to next waypoint, degrees.
    /// </summary>
    public double DesiredTrackDegrees;

    /// <summary>
    /// Distance to next waypoint, nautical miles.
    /// </summary>
    public double DistanceToNextNauticalMiles;

    /// <summary>
    /// Estimated time enroute to next waypoint, seconds.
    /// </summary>
    public double EteToNextSeconds;

    /// <summary>
    /// Estimated time of arrival at next waypoint (seconds since midnight).
    /// </summary>
    public double EtaToNextSeconds;

    /// <summary>
    /// Cross-track error, nautical miles (positive right of course).
    /// </summary>
    public double CrossTrackErrorNauticalMiles;

    /// <summary>
    /// Distance to destination, nautical miles.
    /// </summary>
    public double DistanceToDestinationNauticalMiles;

    /// <summary>
    /// Estimated time enroute to destination, seconds.
    /// </summary>
    public double EteToDestinationSeconds;

    /// <summary>
    /// Estimated time of arrival to destination (seconds since midnight).
    /// </summary>
    public double EtaToDestinationSeconds;
}
