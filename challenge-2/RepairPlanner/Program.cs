using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepairPlanner;
using RepairPlanner.Models;
using RepairPlanner.Services;

// ============================================================================
// Environment Variables Setup
// ============================================================================
// ?? operator means "if null, use this instead" (like Python's "or")
var azureAiProjectEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT environment variable is required");

var modelDeploymentName = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")
    ?? throw new InvalidOperationException("MODEL_DEPLOYMENT_NAME environment variable is required");

var cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")
    ?? throw new InvalidOperationException("COSMOS_ENDPOINT environment variable is required");

var cosmosKey = Environment.GetEnvironmentVariable("COSMOS_KEY")
    ?? throw new InvalidOperationException("COSMOS_KEY environment variable is required");

var cosmosDatabaseName = Environment.GetEnvironmentVariable("COSMOS_DATABASE_NAME")
    ?? throw new InvalidOperationException("COSMOS_DATABASE_NAME environment variable is required");

Console.WriteLine("=== Repair Planner Agent - Initialization ===");
Console.WriteLine($"Azure AI Project Endpoint: {azureAiProjectEndpoint}");
Console.WriteLine($"Model Deployment: {modelDeploymentName}");
Console.WriteLine($"Cosmos DB Endpoint: {cosmosEndpoint}");
Console.WriteLine($"Cosmos DB Database: {cosmosDatabaseName}");
Console.WriteLine();

// ============================================================================
// Dependency Injection Setup
// ============================================================================
// ServiceCollection is like Python's dependency injection container
var services = new ServiceCollection();

// Add console logging with Information level
// AddLogging returns the same collection (fluent API, like Python's method chaining)
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register AIProjectClient as singleton
// new Uri() converts string to URI type (C# is strongly typed)
// DefaultAzureCredential automatically handles Azure authentication
services.AddSingleton(sp => new AIProjectClient(
    new Uri(azureAiProjectEndpoint),
    new DefaultAzureCredential()));

// Register CosmosClient as singleton
// CosmosClient is thread-safe and should be reused
services.AddSingleton(sp => new CosmosClient(
    cosmosEndpoint,
    cosmosKey));

// Register CosmosDbService as singleton
// The lambda (sp => ...) is like Python's lambda, but for dependency resolution
services.AddSingleton<CosmosDbService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosDbService>>();
    return new CosmosDbService(cosmosClient, cosmosDatabaseName, logger);
});

// Register FaultMappingService as IFaultMappingService (interface-based registration)
// This allows for easy mocking and testing
services.AddSingleton<IFaultMappingService, FaultMappingService>();

// Register RepairPlannerAgent as singleton
// This uses the new primary constructor pattern
services.AddSingleton<RepairPlannerAgent>(sp =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    var cosmosDb = sp.GetRequiredService<CosmosDbService>();
    var faultMapping = sp.GetRequiredService<IFaultMappingService>();
    var logger = sp.GetRequiredService<ILogger<RepairPlannerAgent>>();
    return new RepairPlannerAgent(projectClient, cosmosDb, faultMapping, modelDeploymentName, logger);
});

// ============================================================================
// Build Service Provider and Run Workflow
// ============================================================================
// await using is like Python's "async with" - ensures proper cleanup
await using var serviceProvider = services.BuildServiceProvider();

try
{
    Console.WriteLine("=== Starting Repair Planning Workflow ===");
    Console.WriteLine();

    // Get the RepairPlannerAgent from DI container
    // GetRequiredService throws if service not found (like strict mode)
    var repairPlanner = serviceProvider.GetRequiredService<RepairPlannerAgent>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Step 1: Ensure agent is registered in Azure AI Foundry
    Console.WriteLine("Step 1: Registering agent with Azure AI Foundry...");
    await repairPlanner.EnsureAgentVersionAsync();
    Console.WriteLine("✓ Agent registered successfully");
    Console.WriteLine();

    // Step 2: Create a sample diagnosed fault
    // This would typically come from the Fault Diagnosis Agent (Challenge 1)
    Console.WriteLine("Step 2: Creating sample diagnosed fault...");
    var sampleFault = new DiagnosedFault
    {
        Id = Guid.NewGuid().ToString(),
        MachineId = "machine-001",
        FaultType = "curing_temperature_excessive",
        Severity = "critical",
        RecommendedAction = "Replace heating element controller and temperature sensor. Verify proper calibration before resuming operations.",
        DetectedAt = DateTime.UtcNow.AddHours(-1), // Detected 1 hour ago
        Confidence = 0.95,
        PartitionKey = "critical" // Partition key matches severity
    };

    Console.WriteLine($"  Fault Type: {sampleFault.FaultType}");
    Console.WriteLine($"  Machine ID: {sampleFault.MachineId}");
    Console.WriteLine($"  Severity: {sampleFault.Severity}");
    Console.WriteLine($"  Recommended Action: {sampleFault.RecommendedAction}");
    Console.WriteLine();

    // Step 3: Generate repair plan and create work order
    Console.WriteLine("Step 3: Generating repair plan and creating work order...");
    Console.WriteLine("(This may take 15-30 seconds as the agent analyzes the fault and plans repairs)");
    Console.WriteLine();

    var workOrder = await repairPlanner.PlanAndCreateWorkOrderAsync(sampleFault);

    // Step 4: Display results
    Console.WriteLine("=== Work Order Created Successfully ===");
    Console.WriteLine();
    Console.WriteLine($"Work Order Number: {workOrder.WorkOrderNumber}");
    Console.WriteLine($"Status: {workOrder.Status}");
    Console.WriteLine($"Priority: {workOrder.Priority}");
    Console.WriteLine($"Type: {workOrder.Type}");
    Console.WriteLine($"Machine ID: {workOrder.MachineId}");
    Console.WriteLine($"Assigned To: {workOrder.AssignedTo ?? "Unassigned"}");
    Console.WriteLine($"Estimated Duration: {workOrder.EstimatedDuration} minutes");
    Console.WriteLine($"Number of Tasks: {workOrder.Tasks?.Count ?? 0}");
    Console.WriteLine($"Parts Required: {workOrder.PartsUsed?.Count ?? 0}");
    Console.WriteLine();

    // Display tasks summary
    if (workOrder.Tasks?.Count > 0)
    {
        Console.WriteLine("--- Tasks Summary ---");
        foreach (var task in workOrder.Tasks.OrderBy(t => t.Sequence))
        {
            Console.WriteLine($"  {task.Sequence}. {task.Title} ({task.EstimatedDurationMinutes} min)");
            if (task.RequiredSkills?.Count > 0)
            {
                Console.WriteLine($"     Skills: {string.Join(", ", task.RequiredSkills)}");
            }
        }
        Console.WriteLine();
    }

    // Display parts summary
    if (workOrder.PartsUsed?.Count > 0)
    {
        Console.WriteLine("--- Parts Required ---");
        foreach (var part in workOrder.PartsUsed)
        {
            Console.WriteLine($"  • {part.PartNumber} (Qty: {part.Quantity})");
        }
        Console.WriteLine();
    }

    // Step 5: Serialize and print complete work order as formatted JSON
    Console.WriteLine("=== Complete Work Order (JSON) ===");
    // JsonSerializerOptions configures how the JSON is formatted
    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true, // Pretty-print with indentation
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Use camelCase for property names
    };
    var jsonOutput = JsonSerializer.Serialize(workOrder, jsonOptions);
    Console.WriteLine(jsonOutput);
    Console.WriteLine();

    Console.WriteLine("=== Repair Planning Workflow Completed Successfully ===");
    logger.LogInformation("Repair planning workflow completed for machine {MachineId}", sampleFault.MachineId);
}
catch (Exception ex)
{
    // Exception handling - catch any errors and display helpful message
    Console.Error.WriteLine($"❌ Error during repair planning workflow: {ex.Message}");
    Console.Error.WriteLine($"Exception Type: {ex.GetType().Name}");
    
    // InnerException provides more details about nested errors
    if (ex.InnerException != null)
    {
        Console.Error.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
    
    // Stack trace helps with debugging (like Python's traceback)
    Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
    
    // Exit with error code (non-zero indicates failure)
    Environment.Exit(1);
}
