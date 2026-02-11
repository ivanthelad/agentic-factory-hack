using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlanner.Models;

/// <summary>
/// Represents a complete work order with repair plan, tasks, and resource allocation.
/// This is the primary output of the Repair Planner Agent.
/// </summary>
public sealed class WorkOrder
{
    /// <summary>
    /// Unique identifier for the work order (Cosmos DB document id)
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable work order number (e.g., "WO-2024-001234")
    /// </summary>
    [JsonPropertyName("workOrderNumber")]
    [JsonProperty("workOrderNumber")]
    public string WorkOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Machine identifier for the equipment being repaired
    /// </summary>
    [JsonPropertyName("machineId")]
    [JsonProperty("machineId")]
    public string MachineId { get; set; } = string.Empty;

    /// <summary>
    /// Brief title/summary of the work order
    /// </summary>
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the repair work
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of work order: "corrective", "preventive", "emergency"
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Priority level: "critical", "high", "medium", "low"
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonProperty("priority")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Current status: "pending", "scheduled", "in_progress", "completed", "cancelled"
    /// This is also the partition key for Cosmos DB
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Technician id assigned to this work order (null if not yet assigned)
    /// </summary>
    [JsonPropertyName("assignedTo")]
    [JsonProperty("assignedTo")]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Estimated total duration in minutes (integer)
    /// </summary>
    [JsonPropertyName("estimatedDuration")]
    [JsonProperty("estimatedDuration")]
    public int EstimatedDuration { get; set; }

    /// <summary>
    /// Scheduled start time for the work (null if not yet scheduled)
    /// </summary>
    [JsonPropertyName("scheduledStartTime")]
    [JsonProperty("scheduledStartTime")]
    public DateTime? ScheduledStartTime { get; set; }

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    [JsonPropertyName("notes")]
    [JsonProperty("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// List of parts required for this repair
    /// </summary>
    [JsonPropertyName("partsUsed")]
    [JsonProperty("partsUsed")]
    public List<WorkOrderPartUsage> PartsUsed { get; set; } = new();

    /// <summary>
    /// Ordered list of repair tasks/steps
    /// </summary>
    [JsonPropertyName("tasks")]
    [JsonProperty("tasks")]
    public List<RepairTask> Tasks { get; set; } = new();

    /// <summary>
    /// Timestamp when the work order was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the work order was last updated
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: Reference to the diagnosed fault that triggered this work order
    /// </summary>
    [JsonPropertyName("faultId")]
    [JsonProperty("faultId")]
    public string FaultId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Actual completion time
    /// </summary>
    [JsonPropertyName("completedAt")]
    [JsonProperty("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Optional: Total estimated cost (parts + labor)
    /// </summary>
    [JsonPropertyName("estimatedCost")]
    [JsonProperty("estimatedCost")]
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Optional: Actual cost after completion
    /// </summary>
    [JsonPropertyName("actualCost")]
    [JsonProperty("actualCost")]
    public decimal? ActualCost { get; set; }
}
