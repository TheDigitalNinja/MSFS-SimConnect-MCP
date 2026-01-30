using System.Runtime.InteropServices;

namespace MsfsMcpServer.Models;

/// <summary>
/// Raw navigation source and CDI/GSI status data.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct NavigationStatusData
{
    /// <summary>
    /// Indicates GPS is driving NAV1.
    /// </summary>
    public int GpsDrivesNav1;

    /// <summary>
    /// Selected OBS/CRS on GPS (degrees).
    /// </summary>
    public double GpsObsDegrees;

    /// <summary>
    /// Selected NAV1 OBS/CRS (degrees).
    /// </summary>
    public double NavObsDegrees;

    /// <summary>
    /// NAV1 CDI deflection (dots, signed).
    /// </summary>
    public double NavCdi;

    /// <summary>
    /// NAV1 GSI deflection (dots, signed).
    /// </summary>
    public double NavGsi;

    /// <summary>
    /// NAV1 has localizer available.
    /// </summary>
    public int NavHasLocalizer;

    /// <summary>
    /// NAV1 has glide slope available.
    /// </summary>
    public int NavHasGlideSlope;

    /// <summary>
    /// NAV1 active frequency MHz.
    /// </summary>
    public double Nav1ActiveFrequencyMHz;

    /// <summary>
    /// NAV2 active frequency MHz.
    /// </summary>
    public double Nav2ActiveFrequencyMHz;
}
