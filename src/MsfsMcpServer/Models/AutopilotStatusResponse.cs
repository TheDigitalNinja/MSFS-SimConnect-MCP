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

    [JsonPropertyName("approach_armed")]
    public bool ApproachArmed { get; init; }

    [JsonPropertyName("glideslope_mode")]
    public bool GlideSlopeMode { get; init; }

    [JsonPropertyName("backcourse_mode")]
    public bool BackcourseMode { get; init; }

    [JsonPropertyName("flight_director")]
    public bool FlightDirector { get; init; }

    [JsonPropertyName("yaw_damper")]
    public bool YawDamper { get; init; }

    [JsonPropertyName("bank_hold_mode")]
    public bool BankHoldMode { get; init; }

    [JsonPropertyName("bank_hold_deg")]
    public double BankHoldDegrees { get; init; }

    [JsonPropertyName("pitch_hold_mode")]
    public bool PitchHoldMode { get; init; }

    [JsonPropertyName("pitch_hold_deg")]
    public double PitchHoldDegrees { get; init; }

    [JsonPropertyName("altitude_arm")]
    public bool AltitudeArm { get; init; }

    [JsonPropertyName("vnav_armed")]
    public bool VnavArmed { get; init; }

    [JsonPropertyName("vnav_active")]
    public bool VnavActive { get; init; }

    [JsonPropertyName("autothrottle_armed")]
    public bool AutothrottleArmed { get; init; }

    [JsonPropertyName("autothrottle_active")]
    public bool AutothrottleActive { get; init; }

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
            ApproachArmed = ToBool(data.ApproachArmed),
            GlideSlopeMode = ToBool(data.GlideSlopeHold),
            BackcourseMode = ToBool(data.BackcourseHold),
            FlightDirector = ToBool(data.FlightDirectorActive),
            YawDamper = ToBool(data.YawDampener),
            BankHoldMode = ToBool(data.BankHold),
            BankHoldDegrees = Round(data.BankHoldDegrees, 0),
            PitchHoldMode = ToBool(data.PitchHold),
            PitchHoldDegrees = Round(data.PitchHoldDegrees, 0),
            AltitudeArm = ToBool(data.AltitudeArmed),
            VnavArmed = ToBool(data.VnavArmed),
            VnavActive = ToBool(data.VnavActive),
            AutothrottleArmed = ToBool(data.AutothrottleArmed),
            AutothrottleActive = ToBool(data.AutothrottleActive),
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
            ApproachArmed = false,
            GlideSlopeMode = false,
            BackcourseMode = false,
            FlightDirector = false,
            YawDamper = false,
            BankHoldMode = false,
            BankHoldDegrees = 0,
            PitchHoldMode = false,
            PitchHoldDegrees = 0,
            AltitudeArm = false,
            VnavArmed = false,
            VnavActive = false,
            AutothrottleArmed = false,
            AutothrottleActive = false,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static bool ToBool(int value) => value != 0;

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
