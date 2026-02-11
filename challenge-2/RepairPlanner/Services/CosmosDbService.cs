using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using RepairPlanner.Models;

namespace RepairPlanner.Services;

/// <summary>
/// Service for interacting with Azure Cosmos DB for tire manufacturing maintenance data.
/// Provides CRUD operations for Technicians, Parts Inventory, and Work Orders.
/// Uses primary constructor pattern - parameters become fields automatically.
/// </summary>
public sealed class CosmosDbService(
    CosmosClient cosmosClient,
    string databaseName,
    ILogger<CosmosDbService> logger)
{
    // Container names - must match the containers in your Cosmos DB database
    private const string TechniciansContainerName = "Technicians";
    private const string PartsInventoryContainerName = "PartsInventory";
    private const string WorkOrdersContainerName = "WorkOrders";

    // Lazy initialization of containers (created on first access)
    // ?? = means "assign if null" (like Python's: x = x or default_value)
    private Container? _techniciansContainer;
    private Container? _partsInventoryContainer;
    private Container? _workOrdersContainer;

    /// <summary>
    /// Gets the Technicians container (partition key: department)
    /// </summary>
    private Container TechniciansContainer => _techniciansContainer ??= cosmosClient
        .GetDatabase(databaseName)
        .GetContainer(TechniciansContainerName);

    /// <summary>
    /// Gets the PartsInventory container (partition key: category)
    /// </summary>
    private Container PartsInventoryContainer => _partsInventoryContainer ??= cosmosClient
        .GetDatabase(databaseName)
        .GetContainer(PartsInventoryContainerName);

    /// <summary>
    /// Gets the WorkOrders container (partition key: status)
    /// </summary>
    private Container WorkOrdersContainer => _workOrdersContainer ??= cosmosClient
        .GetDatabase(databaseName)
        .GetContainer(WorkOrdersContainerName);

    /// <summary>
    /// Queries available technicians who have ANY of the required skills.
    /// Returns technicians with availability = "available" and at least one matching skill.
    /// </summary>
    /// <param name="requiredSkills">List of skills needed for the repair (e.g., ["plc_troubleshooting", "bearing_replacement"])</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>List of available technicians with matching skills, ordered by experience (descending)</returns>
    public async Task<List<Technician>> GetAvailableTechniciansWithSkillsAsync(
        IReadOnlyList<string> requiredSkills,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Querying available technicians with skills: {Skills}",
                string.Join(", ", requiredSkills));

            // If no skills required, return all available technicians
            if (requiredSkills.Count == 0)
            {
                logger.LogWarning("No required skills specified, returning all available technicians");
                return await GetAllAvailableTechniciansAsync(cancellationToken);
            }

            // Use LINQ to query Cosmos DB
            // GetItemLinqQueryable creates an IQueryable that translates to Cosmos DB SQL
            var queryable = TechniciansContainer
                .GetItemLinqQueryable<Technician>(requestOptions: new QueryRequestOptions
                {
                    // Cross-partition query since we're searching across all departments
                    MaxItemCount = 100
                });

            // Build LINQ query: filter for available technicians with ANY of the required skills
            var query = queryable
                .Where(t => t.Availability == "available")
                .Where(t => t.Skills.Any(skill => requiredSkills.Contains(skill)))
                .OrderByDescending(t => t.ExperienceYears) // Most experienced first
                .ToFeedIterator(); // Convert to iterator for efficient paging

            var technicians = new List<Technician>();

            // Iterate through pages of results
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync(cancellationToken);
                technicians.AddRange(response);

                logger.LogDebug(
                    "Retrieved {Count} technicians (RU cost: {RU})",
                    response.Count,
                    response.RequestCharge);
            }

            logger.LogInformation(
                "Found {Count} available technicians with matching skills",
                technicians.Count);

            return technicians;
        }
        catch (CosmosException ex)
        {
            logger.LogError(
                ex,
                "Cosmos DB error querying technicians: Status={Status}, Message={Message}",
                ex.StatusCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error querying technicians");
            throw;
        }
    }

    /// <summary>
    /// Helper method to get all available technicians (used when no specific skills required)
    /// </summary>
    private async Task<List<Technician>> GetAllAvailableTechniciansAsync(
        CancellationToken cancellationToken)
    {
        var queryable = TechniciansContainer
            .GetItemLinqQueryable<Technician>(requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 100
            });

        var query = queryable
            .Where(t => t.Availability == "available")
            .OrderByDescending(t => t.ExperienceYears)
            .ToFeedIterator();

        var technicians = new List<Technician>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync(cancellationToken);
            technicians.AddRange(response);
        }

        return technicians;
    }

    /// <summary>
    /// Fetches parts from inventory by their part numbers.
    /// Returns only parts that exist in the inventory.
    /// </summary>
    /// <param name="partNumbers">List of part numbers to fetch (e.g., ["TCP-HTR-4KW", "GEN-TS-K400"])</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>List of parts found in inventory</returns>
    public async Task<List<Part>> GetPartsInventoryAsync(
        IReadOnlyList<string> partNumbers,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Fetching parts inventory for: {PartNumbers}",
                string.Join(", ", partNumbers));

            // If no parts requested, return empty list
            if (partNumbers.Count == 0)
            {
                logger.LogInformation("No part numbers specified, returning empty list");
                return new List<Part>();
            }

            // Use LINQ to query Cosmos DB for parts matching the part numbers
            var queryable = PartsInventoryContainer
                .GetItemLinqQueryable<Part>(requestOptions: new QueryRequestOptions
                {
                    // Cross-partition query since parts can be in different categories
                    MaxItemCount = 100
                });

            // Filter parts where partNumber is in the list of requested part numbers
            var query = queryable
                .Where(p => partNumbers.Contains(p.PartNumber))
                .ToFeedIterator();

            var parts = new List<Part>();

            // Iterate through pages of results
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync(cancellationToken);
                parts.AddRange(response);

                logger.LogDebug(
                    "Retrieved {Count} parts (RU cost: {RU})",
                    response.Count,
                    response.RequestCharge);
            }

            logger.LogInformation(
                "Found {Count} parts in inventory out of {Requested} requested",
                parts.Count,
                partNumbers.Count);

            // Log any missing parts
            var foundPartNumbers = parts.Select(p => p.PartNumber).ToHashSet();
            var missingParts = partNumbers.Where(pn => !foundPartNumbers.Contains(pn)).ToList();
            if (missingParts.Count > 0)
            {
                logger.LogWarning(
                    "Parts not found in inventory: {MissingParts}",
                    string.Join(", ", missingParts));
            }

            return parts;
        }
        catch (CosmosException ex)
        {
            logger.LogError(
                ex,
                "Cosmos DB error fetching parts inventory: Status={Status}, Message={Message}",
                ex.StatusCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching parts inventory");
            throw;
        }
    }

    /// <summary>
    /// Creates a new work order in Cosmos DB.
    /// Sets timestamps and validates required fields.
    /// </summary>
    /// <param name="workOrder">Work order to create</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The created work order with generated ID and timestamps</returns>
    public async Task<WorkOrder> CreateWorkOrderAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(workOrder.MachineId))
            {
                throw new ArgumentException("MachineId is required", nameof(workOrder));
            }

            if (string.IsNullOrWhiteSpace(workOrder.Status))
            {
                throw new ArgumentException("Status is required (partition key)", nameof(workOrder));
            }

            // Generate ID if not provided
            // ?? = means "if null, use this instead" (like Python's "or")
            workOrder.Id = string.IsNullOrWhiteSpace(workOrder.Id)
                ? Guid.NewGuid().ToString()
                : workOrder.Id;

            // Set timestamps
            var now = DateTime.UtcNow;
            workOrder.CreatedAt = now;
            workOrder.UpdatedAt = now;

            logger.LogInformation(
                "Creating work order {WorkOrderNumber} for machine {MachineId} with status {Status}",
                workOrder.WorkOrderNumber,
                workOrder.MachineId,
                workOrder.Status);

            // Create the item in Cosmos DB
            // PartitionKey must match the partition key path defined in the container (/status)
            var response = await WorkOrdersContainer.CreateItemAsync(
                item: workOrder,
                partitionKey: new PartitionKey(workOrder.Status),
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Successfully created work order {Id} (RU cost: {RU})",
                response.Resource.Id,
                response.RequestCharge);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // 409 Conflict means item with this ID already exists
            logger.LogError(
                ex,
                "Work order with ID {Id} already exists",
                workOrder.Id);
            throw new InvalidOperationException(
                $"Work order with ID {workOrder.Id} already exists",
                ex);
        }
        catch (CosmosException ex)
        {
            logger.LogError(
                ex,
                "Cosmos DB error creating work order: Status={Status}, Message={Message}",
                ex.StatusCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating work order");
            throw;
        }
    }

    /// <summary>
    /// Optional: Get a work order by ID and status (partition key)
    /// Useful for retrieving work orders after creation or for updates
    /// </summary>
    /// <param name="id">Work order ID</param>
    /// <param name="status">Work order status (partition key)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The work order if found, null otherwise</returns>
    public async Task<WorkOrder?> GetWorkOrderByIdAsync(
        string id,
        string status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await WorkOrdersContainer.ReadItemAsync<WorkOrder>(
                id: id,
                partitionKey: new PartitionKey(status),
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Retrieved work order {Id} (RU cost: {RU})",
                id,
                response.RequestCharge);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Work order {Id} with status {Status} not found", id, status);
            return null;
        }
        catch (CosmosException ex)
        {
            logger.LogError(
                ex,
                "Cosmos DB error retrieving work order: Status={Status}, Message={Message}",
                ex.StatusCode,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Optional: Get a technician by ID
    /// </summary>
    public async Task<Technician?> GetTechnicianByIdAsync(
        string id,
        string department,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await TechniciansContainer.ReadItemAsync<Technician>(
                id: id,
                partitionKey: new PartitionKey(department),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Technician {Id} in department {Department} not found", id, department);
            return null;
        }
    }
}
