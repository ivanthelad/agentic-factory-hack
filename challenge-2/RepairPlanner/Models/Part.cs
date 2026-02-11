using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlanner.Models;

/// <summary>
/// Represents a spare part or inventory item for equipment repairs.
/// </summary>
public sealed class Part
{
    /// <summary>
    /// Unique identifier for the part (Cosmos DB document id)
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Part number/SKU for ordering and tracking
    /// </summary>
    [JsonPropertyName("partNumber")]
    [JsonProperty("partNumber")]
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name/description of the part
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category of the part (e.g., "heating_element", "sensor", "bearing", "hydraulic", "electrical")
    /// This is also the partition key for Cosmos DB
    /// </summary>
    [JsonPropertyName("category")]
    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Current quantity available in inventory
    /// </summary>
    [JsonPropertyName("quantityAvailable")]
    [JsonProperty("quantityAvailable")]
    public int QuantityAvailable { get; set; }

    /// <summary>
    /// Cost per unit in USD
    /// </summary>
    [JsonPropertyName("unitCost")]
    [JsonProperty("unitCost")]
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Supplier name or identifier
    /// </summary>
    [JsonPropertyName("supplier")]
    [JsonProperty("supplier")]
    public string Supplier { get; set; } = string.Empty;

    /// <summary>
    /// Minimum stock level that triggers reorder
    /// </summary>
    [JsonPropertyName("reorderLevel")]
    [JsonProperty("reorderLevel")]
    public int ReorderLevel { get; set; }

    /// <summary>
    /// Lead time in days for reordering
    /// </summary>
    [JsonPropertyName("leadTimeDays")]
    [JsonProperty("leadTimeDays")]
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// Physical location in warehouse/storage
    /// </summary>
    [JsonPropertyName("location")]
    [JsonProperty("location")]
    public string Location { get; set; } = string.Empty;
}
