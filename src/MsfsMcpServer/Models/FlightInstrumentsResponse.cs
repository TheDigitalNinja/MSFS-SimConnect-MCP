using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for flight instruments data.
/// </summary>
public sealed class FlightInstrumentsResponse
{
    [JsonPropertyName("indicated_altitude_ft")]
    public double IndicatedAltitudeFeet { get; init; }

    [JsonPropertyName("airspeed_indicated_kts")]
    public double AirspeedIndicatedKnots { get; init; }

    [JsonPropertyName("airspeed_true_kts")]
    public double AirspeedTrueKnots { get; init; }

    [JsonPropertyName("mach")]
    public double Mach { get; init; }

    [JsonPropertyName("heading_indicator_deg")]
    public double HeadingIndicatorDegrees { get; init; }

    [JsonPropertyName("altimeter_setting_inhg")]
    public double AltimeterSettingInHg { get; init; }

    [JsonPropertyName("attitude_pitch_deg")]
    public double AttitudePitchDegrees { get; init; }

    [JsonPropertyName("attitude_bank_deg")]
    public double AttitudeBankDegrees { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static FlightInstrumentsResponse FromData(FlightInstrumentsData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            IndicatedAltitudeFeet = Round(data.IndicatedAltitudeFeet, 0),
            AirspeedIndicatedKnots = Round(data.AirspeedIndicatedKnots, 0),
            AirspeedTrueKnots = Round(data.AirspeedTrueKnots, 0),
            Mach = Round(data.Mach, 3),
            HeadingIndicatorDegrees = Round(data.HeadingIndicatorDegrees, 1),
            AltimeterSettingInHg = Round(data.AltimeterSettingInHg, 3),
            AttitudePitchDegrees = Round(data.AttitudePitchDegrees, 1),
            AttitudeBankDegrees = Round(data.AttitudeBankDegrees, 1),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static FlightInstrumentsResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            IndicatedAltitudeFeet = 0,
            AirspeedIndicatedKnots = 0,
            AirspeedTrueKnots = 0,
            Mach = 0,
            HeadingIndicatorDegrees = 0,
            AltimeterSettingInHg = 0,
            AttitudePitchDegrees = 0,
            AttitudeBankDegrees = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
