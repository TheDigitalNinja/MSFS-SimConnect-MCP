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

    [JsonPropertyName("ground_speed_kts")]
    public double GroundSpeedKnots { get; init; }

    [JsonPropertyName("vertical_speed_fpm")]
    public double VerticalSpeedFpm { get; init; }

    [JsonPropertyName("pitch_deg")]
    public double PitchDegrees { get; init; }

    [JsonPropertyName("bank_deg")]
    public double BankDegrees { get; init; }

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
            GroundSpeedKnots = Round(data.GroundSpeedKnots, 0),
            VerticalSpeedFpm = Round(data.VerticalSpeedFpm, 0),
            PitchDegrees = Round(data.PitchDegrees, 1),
            BankDegrees = Round(data.BankDegrees, 1),
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
            GroundSpeedKnots = 0,
            VerticalSpeedFpm = 0,
            PitchDegrees = 0,
            BankDegrees = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
