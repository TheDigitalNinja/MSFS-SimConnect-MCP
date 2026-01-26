using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw engine status data returned from SimConnect.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct EngineStatusData
{
    /// <summary>
    /// Number of engines.
    /// </summary>
    public int EngineCount;

    /// <summary>
    /// Engine 1 RPM.
    /// </summary>
    public double EngineOneRpm;

    /// <summary>
    /// Engine 1 throttle position, percent.
    /// </summary>
    public double EngineOneThrottlePercent;

    /// <summary>
    /// Engine 1 fuel flow, gallons per hour.
    /// </summary>
    public double FuelFlowGph;

    /// <summary>
    /// Total fuel remaining, gallons.
    /// </summary>
    public double FuelTotalGallons;

    /// <summary>
    /// Engine 1 exhaust gas temperature, Celsius.
    /// </summary>
    public double ExhaustGasTemperatureCelsius;

    /// <summary>
    /// Engine 1 oil pressure, psi.
    /// </summary>
    public double OilPressurePsi;

    /// <summary>
    /// Engine 1 oil temperature, Celsius.
    /// </summary>
    public double OilTemperatureCelsius;
}
