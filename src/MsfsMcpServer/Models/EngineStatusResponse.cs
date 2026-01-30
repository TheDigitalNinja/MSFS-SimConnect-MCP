using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for engine status data.
/// </summary>
public sealed class EngineStatusResponse
{
    [JsonPropertyName("engine_count")]
    public int EngineCount { get; init; }

    [JsonPropertyName("engine1_rpm")]
    public double EngineOneRpm { get; init; }

    [JsonPropertyName("engine1_throttle_pct")]
    public double EngineOneThrottlePercent { get; init; }

    [JsonPropertyName("engine2_rpm")]
    public double EngineTwoRpm { get; init; }

    [JsonPropertyName("engine2_throttle_pct")]
    public double EngineTwoThrottlePercent { get; init; }

    [JsonPropertyName("fuel_flow_gph")]
    public double FuelFlowGallonsPerHour { get; init; }

    [JsonPropertyName("fuel_flow2_gph")]
    public double FuelFlowTwoGallonsPerHour { get; init; }

    [JsonPropertyName("fuel_total_gal")]
    public double FuelTotalGallons { get; init; }

    [JsonPropertyName("fuel_total_weight_lb")]
    public double FuelTotalWeightPounds { get; init; }

    [JsonPropertyName("egt_celsius")]
    public double ExhaustGasTemperatureCelsius { get; init; }

    [JsonPropertyName("egt2_celsius")]
    public double ExhaustGasTemperatureTwoCelsius { get; init; }

    [JsonPropertyName("n1_pct")]
    public double EngineOneN1Percent { get; init; }

    [JsonPropertyName("n1_2_pct")]
    public double EngineTwoN1Percent { get; init; }

    [JsonPropertyName("n2_pct")]
    public double EngineOneN2Percent { get; init; }

    [JsonPropertyName("n2_2_pct")]
    public double EngineTwoN2Percent { get; init; }

    [JsonPropertyName("torque_pct")]
    public double EngineOneTorquePercent { get; init; }

    [JsonPropertyName("torque2_pct")]
    public double EngineTwoTorquePercent { get; init; }

    [JsonPropertyName("oil_pressure_psi")]
    public double OilPressurePsi { get; init; }

    [JsonPropertyName("fuel_pressure_psi")]
    public double FuelPressurePsi { get; init; }

    [JsonPropertyName("fuel_pressure2_psi")]
    public double FuelPressureTwoPsi { get; init; }

    [JsonPropertyName("oil_temp_celsius")]
    public double OilTemperatureCelsius { get; init; }

    [JsonPropertyName("apu_pct_rpm")]
    public double ApuPercentRpm { get; init; }

    [JsonPropertyName("apu_bleed_on")]
    public bool ApuBleedAir { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static EngineStatusResponse FromData(EngineStatusData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            EngineCount = data.EngineCount,
            EngineOneRpm = Round(data.EngineOneRpm, 0),
            EngineOneThrottlePercent = Round(data.EngineOneThrottlePercent, 1),
            FuelFlowGallonsPerHour = Round(data.FuelFlowGph, 1),
            FuelFlowTwoGallonsPerHour = Round(data.FuelFlowTwoGph, 1),
            FuelTotalGallons = Round(data.FuelTotalGallons, 1),
            FuelTotalWeightPounds = Round(data.FuelTotalWeightPounds, 1),
            ExhaustGasTemperatureCelsius = Round(data.ExhaustGasTemperatureCelsius, 0),
            ExhaustGasTemperatureTwoCelsius = Round(data.ExhaustGasTemperatureTwoCelsius, 0),
            EngineTwoRpm = Round(data.EngineTwoRpm, 0),
            EngineTwoThrottlePercent = Round(data.EngineTwoThrottlePercent, 1),
            EngineOneN1Percent = Round(data.EngineOneN1Percent, 1),
            EngineTwoN1Percent = Round(data.EngineTwoN1Percent, 1),
            EngineOneN2Percent = Round(data.EngineOneN2Percent, 1),
            EngineTwoN2Percent = Round(data.EngineTwoN2Percent, 1),
            EngineOneTorquePercent = Round(data.EngineOneTorquePercent, 1),
            EngineTwoTorquePercent = Round(data.EngineTwoTorquePercent, 1),
            OilPressurePsi = Round(data.OilPressurePsi, 1),
            FuelPressurePsi = Round(data.FuelPressurePsi, 1),
            FuelPressureTwoPsi = Round(data.FuelPressureTwoPsi, 1),
            OilTemperatureCelsius = Round(data.OilTemperatureCelsius, 0),
            ApuPercentRpm = Round(data.ApuPercentRpm, 0),
            ApuBleedAir = ToBool(data.ApuBleedAir),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static EngineStatusResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            EngineCount = 0,
            EngineOneRpm = 0,
            EngineOneThrottlePercent = 0,
            FuelFlowGallonsPerHour = 0,
            FuelFlowTwoGallonsPerHour = 0,
            FuelTotalGallons = 0,
            FuelTotalWeightPounds = 0,
            ExhaustGasTemperatureCelsius = 0,
            ExhaustGasTemperatureTwoCelsius = 0,
            EngineTwoRpm = 0,
            EngineTwoThrottlePercent = 0,
            EngineOneN1Percent = 0,
            EngineTwoN1Percent = 0,
            EngineOneN2Percent = 0,
            EngineTwoN2Percent = 0,
            EngineOneTorquePercent = 0,
            EngineTwoTorquePercent = 0,
            OilPressurePsi = 0,
            FuelPressurePsi = 0,
            FuelPressureTwoPsi = 0,
            OilTemperatureCelsius = 0,
            ApuPercentRpm = 0,
            ApuBleedAir = false,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static bool ToBool(int value) => value != 0;

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
