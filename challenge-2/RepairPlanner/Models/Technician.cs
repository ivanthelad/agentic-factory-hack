using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlanner.Models;

/// <summary>
/// Represents a maintenance technician with skills and availability.
/// </summary>
public sealed class Technician
{
    /// <summary>
    /// Unique identifier for the technician (Cosmos DB document id)
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the technician
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department the technician belongs to (e.g., "tire_curing", "tire_building", "general_maintenance")
    /// This is also the partition key for Cosmos DB
    /// </summary>
    [JsonPropertyName("department")]
    [JsonProperty("department")]
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// List of skills the technician possesses
    /// (e.g., "tire_curing_press", "plc_troubleshooting", "bearing_replacement")
    /// </summary>
    [JsonPropertyName("skills")]
    [JsonProperty("skills")]
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// Current availability status (e.g., "available", "busy", "on_leave")
    /// </summary>
    [JsonPropertyName("availability")]
    [JsonProperty("availability")]
    public string Availability { get; set; } = string.Empty;

    /// <summary>
    /// Contact information for the technician (phone, email, etc.)
    /// </summary>
    [JsonPropertyName("contactInfo")]
    [JsonProperty("contactInfo")]
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>
    /// Experience level in years
    /// </summary>
    [JsonPropertyName("experienceYears")]
    [JsonProperty("experienceYears")]
    public int ExperienceYears { get; set; }

    /// <summary>
    /// Current shift assignment (e.g., "day", "night", "swing")
    /// </summary>
    [JsonPropertyName("shift")]
    [JsonProperty("shift")]
    public string Shift { get; set; } = string.Empty;
}
