using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for flight position queries.
/// </summary>
public sealed class FlightPositionResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("altitude_msl_ft")]
    public double AltitudeMslFeet { get; init; }

    [JsonPropertyName("heading_true")]
    public double HeadingTrue { get; init; }

    [JsonPropertyName("heading_magnetic")]
    public double HeadingMagnetic { get; init; }

    [JsonPropertyName("ground_speed_kts")]
    public double GroundSpeedKnots { get; init; }

    [JsonPropertyName("vertical_speed_fpm")]
    public double VerticalSpeedFpm { get; init; }

    [JsonPropertyName("pitch_deg")]
    public double PitchDegrees { get; init; }

    [JsonPropertyName("bank_deg")]
    public double BankDegrees { get; init; }

    [JsonPropertyName("radio_altitude_ft")]
    public double RadioAltitudeFeet { get; init; }

    [JsonPropertyName("height_agl_ft")]
    public double HeightAboveGroundFeet { get; init; }

    [JsonPropertyName("wind_speed_kts")]
    public double WindSpeedKnots { get; init; }

    [JsonPropertyName("wind_direction_true")]
    public double WindDirectionDegrees { get; init; }

    [JsonPropertyName("total_air_temp_c")]
    public double TotalAirTemperatureCelsius { get; init; }

    [JsonPropertyName("angle_of_attack_deg")]
    public double AngleOfAttackDegrees { get; init; }

    [JsonPropertyName("slip_skid")]
    public double SlipSkidBall { get; init; }

    [JsonPropertyName("on_ground")]
    public bool OnGround { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static FlightPositionResponse FromData(FlightPositionData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            Latitude = Round(data.Latitude, 4),
            Longitude = Round(data.Longitude, 4),
            AltitudeMslFeet = Round(data.AltitudeMslFeet, 0),
            HeadingTrue = Round(data.HeadingTrue, 1),
            HeadingMagnetic = Round(data.HeadingMagnetic, 1),
            GroundSpeedKnots = Round(data.GroundSpeedKnots, 0),
            VerticalSpeedFpm = Round(data.VerticalSpeedFpm, 0),
            PitchDegrees = Round(data.PitchDegrees, 1),
            BankDegrees = Round(data.BankDegrees, 1),
            RadioAltitudeFeet = Round(data.RadioAltitudeFeet, 0),
            HeightAboveGroundFeet = Round(data.HeightAboveGroundFeet, 0),
            WindSpeedKnots = Round(data.WindSpeedKnots, 0),
            WindDirectionDegrees = Round(data.WindDirectionDegrees, 0),
            TotalAirTemperatureCelsius = Round(data.TotalAirTemperatureCelsius, 1),
            AngleOfAttackDegrees = Round(RadiansToDegrees(data.AngleOfAttackRadians), 1),
            SlipSkidBall = Round(data.SlipSkidBall, 2),
            OnGround = ToBool(data.OnGround),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static FlightPositionResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            Latitude = 0,
            Longitude = 0,
            AltitudeMslFeet = 0,
            HeadingTrue = 0,
            HeadingMagnetic = 0,
            GroundSpeedKnots = 0,
            VerticalSpeedFpm = 0,
            PitchDegrees = 0,
            BankDegrees = 0,
            RadioAltitudeFeet = 0,
            HeightAboveGroundFeet = 0,
            WindSpeedKnots = 0,
            WindDirectionDegrees = 0,
            TotalAirTemperatureCelsius = 0,
            AngleOfAttackDegrees = 0,
            SlipSkidBall = 0,
            OnGround = false,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static bool ToBool(int value) => value != 0;

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;
}
