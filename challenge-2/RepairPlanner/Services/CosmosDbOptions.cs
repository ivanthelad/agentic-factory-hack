namespace RepairPlanner.Services;

/// <summary>
/// Configuration options for Cosmos DB connection settings.
/// Used to store connection details loaded from environment variables or configuration.
/// </summary>
public sealed class CosmosDbOptions
{
    /// <summary>
    /// Cosmos DB account endpoint URI (e.g., "https://myaccount.documents.azure.com:443/")
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Cosmos DB account key for authentication
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Database name to connect to (e.g., "tire-factory-db")
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}
