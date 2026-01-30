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

    /// <summary>
    /// Engine 2 RPM.
    /// </summary>
    public double EngineTwoRpm;

    /// <summary>
    /// Engine 2 throttle position, percent.
    /// </summary>
    public double EngineTwoThrottlePercent;

    /// <summary>
    /// Engine 1 N1 percent.
    /// </summary>
    public double EngineOneN1Percent;

    /// <summary>
    /// Engine 2 N1 percent.
    /// </summary>
    public double EngineTwoN1Percent;

    /// <summary>
    /// Engine 1 N2 percent.
    /// </summary>
    public double EngineOneN2Percent;

    /// <summary>
    /// Engine 2 N2 percent.
    /// </summary>
    public double EngineTwoN2Percent;

    /// <summary>
    /// Engine 1 torque percent.
    /// </summary>
    public double EngineOneTorquePercent;

    /// <summary>
    /// Engine 2 torque percent.
    /// </summary>
    public double EngineTwoTorquePercent;

    /// <summary>
    /// Engine 2 exhaust gas temperature, Celsius.
    /// </summary>
    public double ExhaustGasTemperatureTwoCelsius;

    /// <summary>
    /// Engine 2 fuel flow, gallons per hour.
    /// </summary>
    public double FuelFlowTwoGph;

    /// <summary>
    /// Engine 1 fuel pressure, psi.
    /// </summary>
    public double FuelPressurePsi;

    /// <summary>
    /// Engine 2 fuel pressure, psi.
    /// </summary>
    public double FuelPressureTwoPsi;

    /// <summary>
    /// Total fuel weight, pounds.
    /// </summary>
    public double FuelTotalWeightPounds;

    /// <summary>
    /// APU percent RPM.
    /// </summary>
    public double ApuPercentRpm;

    /// <summary>
    /// APU bleed air available.
    /// </summary>
    public int ApuBleedAir;
}
