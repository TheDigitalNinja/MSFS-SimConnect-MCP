using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for the active flight plan leg.
/// </summary>
public sealed class FlightPlanLegResponse
{
    [JsonPropertyName("flight_plan_active")]
    public bool FlightPlanActive { get; init; }

    [JsonPropertyName("waypoint_count")]
    public int WaypointCount { get; init; }

    [JsonPropertyName("active_waypoint_index")]
    public int ActiveWaypointIndex { get; init; }

    [JsonPropertyName("next_waypoint_id")]
    public string? NextWaypointId { get; init; }

    [JsonPropertyName("next_latitude")]
    public double NextLatitude { get; init; }

    [JsonPropertyName("next_longitude")]
    public double NextLongitude { get; init; }

    [JsonPropertyName("next_altitude_ft")]
    public double NextAltitudeFeet { get; init; }

    [JsonPropertyName("bearing_to_next_deg")]
    public double BearingToNextDegrees { get; init; }

    [JsonPropertyName("desired_track_deg")]
    public double DesiredTrackDegrees { get; init; }

    [JsonPropertyName("distance_to_next_nm")]
    public double DistanceToNextNauticalMiles { get; init; }

    [JsonPropertyName("ete_to_next_sec")]
    public double EteToNextSeconds { get; init; }

    [JsonPropertyName("eta_to_next_sec")]
    public double EtaToNextSeconds { get; init; }

    [JsonPropertyName("cross_track_error_nm")]
    public double CrossTrackErrorNauticalMiles { get; init; }

    [JsonPropertyName("distance_to_destination_nm")]
    public double DistanceToDestinationNauticalMiles { get; init; }

    [JsonPropertyName("ete_to_destination_sec")]
    public double EteToDestinationSeconds { get; init; }

    [JsonPropertyName("eta_to_destination_sec")]
    public double EtaToDestinationSeconds { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static FlightPlanLegResponse FromData(FlightPlanLegData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            FlightPlanActive = data.IsFlightPlanActive > 0,
            WaypointCount = data.WaypointCount,
            ActiveWaypointIndex = data.ActiveWaypointIndex,
            NextWaypointId = string.IsNullOrWhiteSpace(data.NextWaypointId) ? null : data.NextWaypointId.Trim(),
            NextLatitude = Round(data.NextWaypointLatitude, 5),
            NextLongitude = Round(data.NextWaypointLongitude, 5),
            NextAltitudeFeet = Round(data.NextWaypointAltitudeFeet, 0),
            BearingToNextDegrees = Round(data.NextWaypointBearingDegrees, 1),
            DesiredTrackDegrees = Round(data.DesiredTrackDegrees, 1),
            DistanceToNextNauticalMiles = Round(data.DistanceToNextNauticalMiles, 1),
            EteToNextSeconds = Round(data.EteToNextSeconds, 1),
            EtaToNextSeconds = Round(data.EtaToNextSeconds, 1),
            CrossTrackErrorNauticalMiles = Round(data.CrossTrackErrorNauticalMiles, 2),
            DistanceToDestinationNauticalMiles = Round(data.DistanceToDestinationNauticalMiles, 1),
            EteToDestinationSeconds = Round(data.EteToDestinationSeconds, 1),
            EtaToDestinationSeconds = Round(data.EtaToDestinationSeconds, 1),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static FlightPlanLegResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            FlightPlanActive = false,
            WaypointCount = 0,
            ActiveWaypointIndex = 0,
            NextWaypointId = null,
            NextLatitude = 0,
            NextLongitude = 0,
            NextAltitudeFeet = 0,
            BearingToNextDegrees = 0,
            DesiredTrackDegrees = 0,
            DistanceToNextNauticalMiles = 0,
            EteToNextSeconds = 0,
            EtaToNextSeconds = 0,
            CrossTrackErrorNauticalMiles = 0,
            DistanceToDestinationNauticalMiles = 0,
            EteToDestinationSeconds = 0,
            EtaToDestinationSeconds = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
