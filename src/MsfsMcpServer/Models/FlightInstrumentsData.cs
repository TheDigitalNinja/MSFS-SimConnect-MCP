using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw flight instrument data returned from SimConnect.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct FlightInstrumentsData
{
    /// <summary>
    /// Indicated altitude, feet.
    /// </summary>
    public double IndicatedAltitudeFeet;

    /// <summary>
    /// Indicated airspeed, knots.
    /// </summary>
    public double AirspeedIndicatedKnots;

    /// <summary>
    /// True airspeed, knots.
    /// </summary>
    public double AirspeedTrueKnots;

    /// <summary>
    /// Mach number.
    /// </summary>
    public double Mach;

    /// <summary>
    /// Heading indicator / HSI, degrees.
    /// </summary>
    public double HeadingIndicatorDegrees;

    /// <summary>
    /// Altimeter setting, inHg.
    /// </summary>
    public double AltimeterSettingInHg;

    /// <summary>
    /// Attitude indicator pitch, degrees.
    /// </summary>
    public double AttitudePitchDegrees;

    /// <summary>
    /// Attitude indicator bank, degrees.
    /// </summary>
    public double AttitudeBankDegrees;
}
