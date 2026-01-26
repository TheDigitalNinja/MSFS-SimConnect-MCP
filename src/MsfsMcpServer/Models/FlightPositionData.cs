using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw flight position data returned from SimConnect.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct FlightPositionData
{
    /// <summary>
    /// Latitude in decimal degrees.
    /// </summary>
    public double Latitude;

    /// <summary>
    /// Longitude in decimal degrees.
    /// </summary>
    public double Longitude;

    /// <summary>
    /// Altitude above mean sea level, feet.
    /// </summary>
    public double AltitudeMslFeet;

    /// <summary>
    /// True heading, degrees.
    /// </summary>
    public double HeadingTrue;

    /// <summary>
    /// Ground speed, knots.
    /// </summary>
    public double GroundSpeedKnots;

    /// <summary>
    /// Vertical speed, feet per minute.
    /// </summary>
    public double VerticalSpeedFpm;

    /// <summary>
    /// Pitch attitude, degrees.
    /// </summary>
    public double PitchDegrees;

    /// <summary>
    /// Bank attitude, degrees.
    /// </summary>
    public double BankDegrees;
}
