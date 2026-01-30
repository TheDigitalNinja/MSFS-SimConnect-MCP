using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw aircraft configuration and lighting data from SimConnect.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct AircraftConfigurationData
{
    public double GearHandlePosition;
    public double GearTotalPercent;
    public int FlapsHandleIndex;
    public double FlapsLeadingEdgePercent;
    public double FlapsTrailingEdgePercent;
    public double SpoilersHandlePercent;
    public int AutobrakeLevel;
    public int ParkingBrakeEngaged;
    public int ParkingBrakeIndicator;
    public double RudderTrimPercent;
    public double ElevatorTrimPercent;
    public double AileronTrimPercent;
    public int BeaconLightOn;
    public int StrobeLightOn;
    public int LandingLightOn;
    public int NavLightOn;
    public int TaxiLightOn;
}
