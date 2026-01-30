using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw approach state data returned from SimConnect GPS SimVars.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ApproachStatusData
{
    /// <summary>
    /// Approach is loaded.
    /// </summary>
    public int ApproachLoaded;

    /// <summary>
    /// Approach is active.
    /// </summary>
    public int ApproachActive;

    /// <summary>
    /// Current approach waypoint index.
    /// </summary>
    public int ApproachWaypointIndex;

    /// <summary>
    /// Approach waypoint count.
    /// </summary>
    public int ApproachWaypointCount;

    /// <summary>
    /// Approach is in final segment.
    /// </summary>
    public int IsFinalApproachSegment;

    /// <summary>
    /// Approach is in missed segment.
    /// </summary>
    public int IsMissedApproachSegment;

    /// <summary>
    /// GPS has glidepath available.
    /// </summary>
    public int GpsHasGlidepath;

    /// <summary>
    /// Glide slope deviation (dots).
    /// </summary>
    public double GlideSlopeErrorDots;

    /// <summary>
    /// GPS GSI needle deflection (dots).
    /// </summary>
    public double GpsGsiNeedleDots;
}
