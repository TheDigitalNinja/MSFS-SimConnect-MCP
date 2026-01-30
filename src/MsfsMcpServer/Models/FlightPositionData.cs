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

    /// <summary>
    /// Magnetic heading, degrees.
    /// </summary>
    public double HeadingMagnetic;

    /// <summary>
    /// Radio altitude above terrain, feet.
    /// </summary>
    public double RadioAltitudeFeet;

    /// <summary>
    /// Height above ground level, feet.
    /// </summary>
    public double HeightAboveGroundFeet;

    /// <summary>
    /// Ambient wind speed, knots.
    /// </summary>
    public double WindSpeedKnots;

    /// <summary>
    /// Ambient wind direction, degrees true.
    /// </summary>
    public double WindDirectionDegrees;

    /// <summary>
    /// Total air temperature, Celsius.
    /// </summary>
    public double TotalAirTemperatureCelsius;

    /// <summary>
    /// Angle of attack, radians.
    /// </summary>
    public double AngleOfAttackRadians;

    /// <summary>
    /// Slip/skid ball deflection.
    /// </summary>
    public double SlipSkidBall;

    /// <summary>
    /// Indicates if the aircraft is on the ground.
    /// </summary>
    public int OnGround;
}
