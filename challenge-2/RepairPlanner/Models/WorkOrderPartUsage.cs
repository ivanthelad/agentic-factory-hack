using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlanner.Models;

/// <summary>
/// Represents a part required for a work order.
/// This is a nested object within WorkOrder, not a separate Cosmos DB document.
/// </summary>
public sealed class WorkOrderPartUsage
{
    /// <summary>
    /// Reference to the Part document id
    /// </summary>
    [JsonPropertyName("partId")]
    [JsonProperty("partId")]
    public string PartId { get; set; } = string.Empty;

    /// <summary>
    /// Part number for easy identification
    /// </summary>
    [JsonPropertyName("partNumber")]
    [JsonProperty("partNumber")]
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of this part needed for the repair
    /// </summary>
    [JsonPropertyName("quantity")]
    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Optional: Part name for display purposes
    /// </summary>
    [JsonPropertyName("partName")]
    [JsonProperty("partName")]
    public string PartName { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Unit cost at time of work order creation (for cost tracking)
    /// </summary>
    [JsonPropertyName("unitCost")]
    [JsonProperty("unitCost")]
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Optional: Whether this part has been pulled from inventory
    /// </summary>
    [JsonPropertyName("isPulled")]
    [JsonProperty("isPulled")]
    public bool IsPulled { get; set; }
}
