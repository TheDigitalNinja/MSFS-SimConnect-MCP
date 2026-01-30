using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for navigation source and CDI/GSI status.
/// </summary>
public sealed class NavigationStatusResponse
{
    [JsonPropertyName("gps_drives_nav1")]
    public bool GpsDrivesNav1 { get; init; }

    [JsonPropertyName("gps_obs_deg")]
    public double GpsObsDegrees { get; init; }

    [JsonPropertyName("nav_obs_deg")]
    public double NavObsDegrees { get; init; }

    [JsonPropertyName("nav_cdi_dots")]
    public double NavCdiDots { get; init; }

    [JsonPropertyName("nav_gsi_dots")]
    public double NavGsiDots { get; init; }

    [JsonPropertyName("nav_has_localizer")]
    public bool NavHasLocalizer { get; init; }

    [JsonPropertyName("nav_has_glide_slope")]
    public bool NavHasGlideSlope { get; init; }

    [JsonPropertyName("nav1_active_mhz")]
    public double Nav1ActiveFrequencyMHz { get; init; }

    [JsonPropertyName("nav2_active_mhz")]
    public double Nav2ActiveFrequencyMHz { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static NavigationStatusResponse FromData(NavigationStatusData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            GpsDrivesNav1 = data.GpsDrivesNav1 > 0,
            GpsObsDegrees = Round(data.GpsObsDegrees, 1),
            NavObsDegrees = Round(data.NavObsDegrees, 1),
            NavCdiDots = Round(data.NavCdi, 2),
            NavGsiDots = Round(data.NavGsi, 2),
            NavHasLocalizer = data.NavHasLocalizer > 0,
            NavHasGlideSlope = data.NavHasGlideSlope > 0,
            Nav1ActiveFrequencyMHz = Round(data.Nav1ActiveFrequencyMHz, 3),
            Nav2ActiveFrequencyMHz = Round(data.Nav2ActiveFrequencyMHz, 3),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static NavigationStatusResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            GpsDrivesNav1 = false,
            GpsObsDegrees = 0,
            NavObsDegrees = 0,
            NavCdiDots = 0,
            NavGsiDots = 0,
            NavHasLocalizer = false,
            NavHasGlideSlope = false,
            Nav1ActiveFrequencyMHz = 0,
            Nav2ActiveFrequencyMHz = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
