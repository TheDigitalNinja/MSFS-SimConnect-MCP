using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for aircraft information queries.
/// </summary>
public sealed class AircraftInfoResponse
{
    [JsonPropertyName("aircraft_title")]
    public string AircraftTitle { get; init; } = string.Empty;

    [JsonPropertyName("tail_number")]
    public string TailNumber { get; init; } = string.Empty;

    [JsonPropertyName("airline")]
    public string Airline { get; init; } = string.Empty;

    [JsonPropertyName("total_weight_lbs")]
    public double TotalWeightPounds { get; init; }

    [JsonPropertyName("max_gross_weight_lbs")]
    public double MaxGrossWeightPounds { get; init; }

    [JsonPropertyName("empty_weight_lbs")]
    public double EmptyWeightPounds { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static AircraftInfoResponse FromData(AircraftInfoData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            AircraftTitle = data.Title?.Trim() ?? string.Empty,
            TailNumber = data.AtcId?.Trim() ?? string.Empty,
            Airline = data.AtcAirline?.Trim() ?? string.Empty,
            TotalWeightPounds = Round(data.TotalWeightPounds, 0),
            MaxGrossWeightPounds = Round(data.MaxGrossWeightPounds, 0),
            EmptyWeightPounds = Round(data.EmptyWeightPounds, 0),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static AircraftInfoResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            AircraftTitle = string.Empty,
            TailNumber = string.Empty,
            Airline = string.Empty,
            TotalWeightPounds = 0,
            MaxGrossWeightPounds = 0,
            EmptyWeightPounds = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
