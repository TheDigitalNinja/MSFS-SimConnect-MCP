using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw aircraft information data returned from SimConnect.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct AircraftInfoData
{
    /// <summary>
    /// Aircraft title/name.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Title;

    /// <summary>
    /// Tail number / ATC ID.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string AtcId;

    /// <summary>
    /// Airline / ATC airline.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string AtcAirline;

    /// <summary>
    /// Current aircraft weight, pounds.
    /// </summary>
    public double TotalWeightPounds;

    /// <summary>
    /// Maximum gross weight, pounds.
    /// </summary>
    public double MaxGrossWeightPounds;

    /// <summary>
    /// Empty weight, pounds.
    /// </summary>
    public double EmptyWeightPounds;
}
