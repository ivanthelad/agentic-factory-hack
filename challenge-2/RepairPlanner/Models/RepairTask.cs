using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlanner.Models;

/// <summary>
/// Represents an individual task/step within a work order repair plan.
/// This is a nested object within WorkOrder, not a separate Cosmos DB document.
/// </summary>
public sealed class RepairTask
{
    /// <summary>
    /// Sequential order of this task (1, 2, 3, etc.)
    /// </summary>
    [JsonPropertyName("sequence")]
    [JsonProperty("sequence")]
    public int Sequence { get; set; }

    /// <summary>
    /// Brief title of the task
    /// </summary>
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the task and how to perform it
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Estimated duration for this task in minutes (integer)
    /// </summary>
    [JsonPropertyName("estimatedDurationMinutes")]
    [JsonProperty("estimatedDurationMinutes")]
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// Skills required to complete this task
    /// (e.g., "plc_troubleshooting", "bearing_replacement")
    /// </summary>
    [JsonPropertyName("requiredSkills")]
    [JsonProperty("requiredSkills")]
    public List<string> RequiredSkills { get; set; } = new();

    /// <summary>
    /// Safety precautions and notes for this task
    /// </summary>
    [JsonPropertyName("safetyNotes")]
    [JsonProperty("safetyNotes")]
    public string SafetyNotes { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Task completion status
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Optional: Actual time taken in minutes (filled after completion)
    /// </summary>
    [JsonPropertyName("actualDurationMinutes")]
    [JsonProperty("actualDurationMinutes")]
    public int? ActualDurationMinutes { get; set; }

    /// <summary>
    /// Optional: Notes from technician after completing the task
    /// </summary>
    [JsonPropertyName("completionNotes")]
    [JsonProperty("completionNotes")]
    public string CompletionNotes { get; set; } = string.Empty;
}
