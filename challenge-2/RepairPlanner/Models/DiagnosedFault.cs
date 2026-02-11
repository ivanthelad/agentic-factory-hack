using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlanner.Models;

/// <summary>
/// Represents a diagnosed fault from the Fault Diagnosis Agent.
/// This is the input to the Repair Planner Agent.
/// </summary>
public sealed class DiagnosedFault
{
    /// <summary>
    /// Unique identifier for the diagnosed fault (Cosmos DB document id)
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of fault detected (e.g., "curing_temperature_excessive", "building_drum_vibration")
    /// </summary>
    [JsonPropertyName("faultType")]
    [JsonProperty("faultType")]
    public string FaultType { get; set; } = string.Empty;

    /// <summary>
    /// Machine identifier where the fault was detected
    /// </summary>
    [JsonPropertyName("machineId")]
    [JsonProperty("machineId")]
    public string MachineId { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the fault (e.g., "critical", "high", "medium", "low")
    /// </summary>
    [JsonPropertyName("severity")]
    [JsonProperty("severity")]
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score of the diagnosis (0.0 to 1.0)
    /// </summary>
    [JsonPropertyName("confidence")]
    [JsonProperty("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// Recommended action from the diagnosis agent
    /// </summary>
    [JsonPropertyName("recommendedAction")]
    [JsonProperty("recommendedAction")]
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the fault was detected
    /// </summary>
    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Partition key for Cosmos DB (typically severity for faults)
    /// </summary>
    [JsonPropertyName("partitionKey")]
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
}
