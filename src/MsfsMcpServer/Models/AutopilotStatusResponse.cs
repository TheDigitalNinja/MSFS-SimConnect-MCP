using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for autopilot status data.
/// </summary>
public sealed class AutopilotStatusResponse
{
    [JsonPropertyName("autopilot_master")]
    public bool AutopilotMaster { get; init; }

    [JsonPropertyName("heading_mode")]
    public bool HeadingMode { get; init; }

    [JsonPropertyName("heading_select_deg")]
    public double HeadingSelectDegrees { get; init; }

    [JsonPropertyName("altitude_hold_mode")]
    public bool AltitudeHoldMode { get; init; }

    [JsonPropertyName("altitude_select_ft")]
    public double AltitudeSelectFeet { get; init; }

    [JsonPropertyName("speed_hold_mode")]
    public bool SpeedHoldMode { get; init; }

    [JsonPropertyName("speed_select_kts")]
    public double SpeedSelectKnots { get; init; }

    [JsonPropertyName("vertical_speed_mode")]
    public bool VerticalSpeedMode { get; init; }

    [JsonPropertyName("vertical_speed_fpm")]
    public double VerticalSpeedFpm { get; init; }

    [JsonPropertyName("nav_mode")]
    public bool NavMode { get; init; }

    [JsonPropertyName("approach_mode")]
    public bool ApproachMode { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static AutopilotStatusResponse FromData(AutopilotStatusData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            AutopilotMaster = ToBool(data.AutopilotMaster),
            HeadingMode = ToBool(data.HeadingLock),
            HeadingSelectDegrees = Round(data.HeadingLockDegrees, 0),
            AltitudeHoldMode = ToBool(data.AltitudeLock),
            AltitudeSelectFeet = Round(data.AltitudeLockFeet, 0),
            SpeedHoldMode = ToBool(data.AirspeedHold),
            SpeedSelectKnots = Round(data.AirspeedHoldKnots, 0),
            VerticalSpeedMode = ToBool(data.VerticalHold),
            VerticalSpeedFpm = Round(data.VerticalHoldFpm, 0),
            NavMode = ToBool(data.NavLock),
            ApproachMode = ToBool(data.ApproachHold),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static AutopilotStatusResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            AutopilotMaster = false,
            HeadingMode = false,
            HeadingSelectDegrees = 0,
            AltitudeHoldMode = false,
            AltitudeSelectFeet = 0,
            SpeedHoldMode = false,
            SpeedSelectKnots = 0,
            VerticalSpeedMode = false,
            VerticalSpeedFpm = 0,
            NavMode = false,
            ApproachMode = false,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static bool ToBool(int value) => value != 0;

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
