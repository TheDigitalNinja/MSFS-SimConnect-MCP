using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for approach status.
/// </summary>
public sealed class ApproachStatusResponse
{
    [JsonPropertyName("approach_loaded")]
    public bool ApproachLoaded { get; init; }

    [JsonPropertyName("approach_active")]
    public bool ApproachActive { get; init; }

    [JsonPropertyName("approach_wp_index")]
    public int ApproachWaypointIndex { get; init; }

    [JsonPropertyName("approach_wp_count")]
    public int ApproachWaypointCount { get; init; }

    [JsonPropertyName("is_final_segment")]
    public bool IsFinalApproachSegment { get; init; }

    [JsonPropertyName("is_missed_segment")]
    public bool IsMissedApproachSegment { get; init; }

    [JsonPropertyName("gps_has_glidepath")]
    public bool GpsHasGlidepath { get; init; }

    [JsonPropertyName("glide_slope_error_dots")]
    public double GlideSlopeErrorDots { get; init; }

    [JsonPropertyName("gps_gsi_dots")]
    public double GpsGsiDots { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static ApproachStatusResponse FromData(ApproachStatusData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            ApproachLoaded = data.ApproachLoaded > 0,
            ApproachActive = data.ApproachActive > 0,
            ApproachWaypointIndex = data.ApproachWaypointIndex,
            ApproachWaypointCount = data.ApproachWaypointCount,
            IsFinalApproachSegment = data.IsFinalApproachSegment > 0,
            IsMissedApproachSegment = data.IsMissedApproachSegment > 0,
            GpsHasGlidepath = data.GpsHasGlidepath > 0,
            GlideSlopeErrorDots = Round(data.GlideSlopeErrorDots, 2),
            GpsGsiDots = Round(data.GpsGsiNeedleDots, 2),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static ApproachStatusResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            ApproachLoaded = false,
            ApproachActive = false,
            ApproachWaypointIndex = 0,
            ApproachWaypointCount = 0,
            IsFinalApproachSegment = false,
            IsMissedApproachSegment = false,
            GpsHasGlidepath = false,
            GlideSlopeErrorDots = 0,
            GpsGsiDots = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
