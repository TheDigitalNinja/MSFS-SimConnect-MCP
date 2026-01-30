using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for a flight plan waypoint (prev/next of active leg).
/// </summary>
public sealed class FlightPlanWaypointResponse
{
    [JsonPropertyName("requested_index")]
    public int RequestedIndex { get; init; }

    [JsonPropertyName("waypoint_id")]
    public string? WaypointId { get; init; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("altitude_ft")]
    public double AltitudeFeet { get; init; }

    [JsonPropertyName("bearing_deg")]
    public double BearingDegrees { get; init; }

    [JsonPropertyName("distance_nm")]
    public double DistanceNauticalMiles { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    public static FlightPlanWaypointResponse FromNext(int requestedIndex, FlightPlanWaypointData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            RequestedIndex = requestedIndex,
            WaypointId = string.IsNullOrWhiteSpace(data.NextWaypointId) ? null : data.NextWaypointId.Trim(),
            Latitude = Round(data.NextWaypointLatitude, 5),
            Longitude = Round(data.NextWaypointLongitude, 5),
            AltitudeFeet = Round(data.NextWaypointAltitudeFeet, 0),
            BearingDegrees = Round(data.NextWaypointBearingDegrees, 1),
            DistanceNauticalMiles = Round(data.NextWaypointDistanceNauticalMiles, 1),
            Error = null,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O")
        };

    public static FlightPlanWaypointResponse FromPrevious(int requestedIndex, FlightPlanWaypointData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            RequestedIndex = requestedIndex,
            WaypointId = string.IsNullOrWhiteSpace(data.PreviousWaypointId) ? null : data.PreviousWaypointId.Trim(),
            Latitude = Round(data.PreviousWaypointLatitude, 5),
            Longitude = Round(data.PreviousWaypointLongitude, 5),
            AltitudeFeet = Round(data.PreviousWaypointAltitudeFeet, 0),
            BearingDegrees = Round(data.PreviousWaypointBearingDegrees, 1),
            DistanceNauticalMiles = Round(data.PreviousWaypointDistanceNauticalMiles, 1),
            Error = null,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O")
        };

    public static FlightPlanWaypointResponse ErrorResponse(int requestedIndex, string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            RequestedIndex = requestedIndex,
            WaypointId = null,
            Latitude = 0,
            Longitude = 0,
            AltitudeFeet = 0,
            BearingDegrees = 0,
            DistanceNauticalMiles = 0,
            Error = message,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O")
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
