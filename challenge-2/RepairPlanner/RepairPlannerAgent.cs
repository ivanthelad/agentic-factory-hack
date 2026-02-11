using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Microsoft.Extensions.Logging;
using RepairPlanner.Models;
using RepairPlanner.Services;

namespace RepairPlanner;

/// <summary>
/// Repair Planner Agent that orchestrates the repair planning workflow.
/// Uses Foundry Agents SDK to generate comprehensive repair plans from diagnosed faults.
/// 
/// Workflow:
/// 1. Receives DiagnosedFault from Fault Diagnosis Agent
/// 2. Maps fault to required skills and parts using FaultMappingService
/// 3. Queries available technicians with matching skills from Cosmos DB
/// 4. Queries parts inventory from Cosmos DB
/// 5. Invokes AI agent to create work order with tasks and resource allocation
/// 6. Saves work order to Cosmos DB
/// 
/// Uses primary constructor pattern - parameters become fields automatically (like Python's __init__)
/// </summary>
public sealed class RepairPlannerAgent(
    AIProjectClient projectClient,
    CosmosDbService cosmosDb,
    IFaultMappingService faultMapping,
    string modelDeploymentName,
    ILogger<RepairPlannerAgent> logger)
{
    // Agent configuration constants
    private const string AgentName = "RepairPlannerAgent";
    
    // Multi-line string literal (raw string literal in C# 11+)
    private const string AgentInstructions = """
        You are a Repair Planner Agent for tire manufacturing equipment maintenance.
        
        Your role is to generate comprehensive repair plans that include:
        - Detailed task breakdown with step-by-step instructions
        - Timeline and duration estimates
        - Resource allocation (technicians and parts)
        - Safety considerations
        
        CRITICAL: Return the response as valid JSON matching the WorkOrder schema.
        
        Output JSON schema:
        {
          "workOrderNumber": "string (e.g., WO-2024-001234)",
          "machineId": "string (from input fault)",
          "title": "string (brief summary)",
          "description": "string (detailed repair description)",
          "type": "corrective" | "preventive" | "emergency",
          "priority": "critical" | "high" | "medium" | "low",
          "status": "string (usually pending)",
          "assignedTo": "string (technician id) or null",
          "notes": "string (additional context)",
          "estimatedDuration": integer (total minutes, e.g., 120 not "120 minutes"),
          "partsUsed": [
            {
              "partId": "string",
              "partNumber": "string",
              "quantity": integer,
              "partName": "string (optional)",
              "unitCost": decimal (optional)
            }
          ],
          "tasks": [
            {
              "sequence": integer (1, 2, 3...),
              "title": "string",
              "description": "string (detailed instructions)",
              "estimatedDurationMinutes": integer (e.g., 30 not "30 minutes"),
              "requiredSkills": ["string"],
              "safetyNotes": "string"
            }
          ]
        }
        
        IMPORTANT RULES:
        1. All duration fields MUST be integers representing minutes (e.g., 90), NOT strings like "90 minutes"
        2. Assign the most qualified available technician based on skills and experience
        3. Include only relevant parts from the provided inventory; use empty array if none needed
        4. Tasks must be ordered sequentially and actionable
        5. Each task should have clear safety notes if applicable
        6. Priority should match fault severity: critical→critical, high→high, etc.
        7. Type is typically "corrective" for diagnosed faults, "emergency" for critical severity
        8. estimatedDuration should equal the sum of all task durations
        9. Return ONLY valid JSON, no markdown code blocks or explanations
        """;

    // JSON serialization options with number handling for LLM responses
    // LLMs sometimes return numbers as strings, so we need to handle this gracefully
    // NumberHandling.AllowReadingFromString allows parsing "120" as 120
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Registers the agent version with the AI Foundry project.
    /// This should be called once at startup to ensure the agent is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task EnsureAgentVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Registering agent version: {AgentName}", AgentName);

            // Create the agent definition with model and instructions
            var definition = new PromptAgentDefinition(model: modelDeploymentName)
            {
                Instructions = AgentInstructions
            };

            // Register the agent version with the AI Foundry project
            // This makes the agent available for invocation via GetAIAgent
            await projectClient.Agents.CreateAgentVersionAsync(
                AgentName,
                new AgentVersionCreationOptions(definition),
                cancellationToken);

            logger.LogInformation("Successfully registered agent version: {AgentName}", AgentName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register agent version: {AgentName}", AgentName);
            throw;
        }
    }

    /// <summary>
    /// Main workflow method: Plans and creates a work order from a diagnosed fault.
    /// 
    /// Steps:
    /// 1. Get required skills and parts from fault mapping
    /// 2. Query available technicians with matching skills
    /// 3. Query parts inventory
    /// 4. Build context prompt with fault, technicians, and parts information
    /// 5. Invoke AI agent to generate work order
    /// 6. Parse and validate JSON response
    /// 7. Apply defaults and business logic
    /// 8. Save work order to Cosmos DB
    /// </summary>
    /// <param name="fault">Diagnosed fault from the Fault Diagnosis Agent</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Complete work order with repair plan saved to Cosmos DB</returns>
    public async Task<WorkOrder> PlanAndCreateWorkOrderAsync(
        DiagnosedFault fault,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Planning repair for fault {FaultId} ({FaultType}) on machine {MachineId}",
                fault.Id,
                fault.FaultType,
                fault.MachineId);

            // Step 1: Get required skills and parts from fault mapping
            var requiredSkills = faultMapping.GetRequiredSkills(fault.FaultType);
            var requiredPartNumbers = faultMapping.GetRequiredParts(fault.FaultType);

            logger.LogInformation(
                "Mapped fault to skills: [{Skills}] and parts: [{Parts}]",
                string.Join(", ", requiredSkills),
                string.Join(", ", requiredPartNumbers));

            // Step 2: Query available technicians with matching skills from Cosmos DB
            var availableTechnicians = await cosmosDb.GetAvailableTechniciansWithSkillsAsync(
                requiredSkills,
                cancellationToken);

            logger.LogInformation(
                "Found {Count} available technicians with matching skills",
                availableTechnicians.Count);

            // Step 3: Query parts inventory from Cosmos DB
            var availableParts = await cosmosDb.GetPartsInventoryAsync(
                requiredPartNumbers,
                cancellationToken);

            logger.LogInformation(
                "Found {Count} parts in inventory",
                availableParts.Count);

            // Step 4: Build context prompt with all relevant information
            var prompt = BuildPrompt(fault, requiredSkills, availableTechnicians, availableParts);

            logger.LogDebug("Generated prompt for agent: {Prompt}", prompt);

            // Step 5: Invoke the AI agent using Foundry Agents SDK
            var workOrderJson = await InvokeAgentAsync(prompt, cancellationToken);

            logger.LogDebug("Received agent response: {Response}", workOrderJson);

            // Step 6: Parse JSON response with number handling
            WorkOrder workOrder;
            try
            {
                // Use JsonOptions with AllowReadingFromString to handle LLM number quirks
                workOrder = JsonSerializer.Deserialize<WorkOrder>(workOrderJson, JsonOptions)
                    ?? throw new InvalidOperationException("Agent returned null work order");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse agent response as WorkOrder JSON: {Json}", workOrderJson);
                throw new InvalidOperationException(
                    $"Agent returned invalid JSON. Response: {workOrderJson}",
                    ex);
            }

            // Step 7: Apply defaults and business logic
            workOrder = ApplyDefaults(workOrder, fault);

            logger.LogInformation(
                "Created work order {WorkOrderNumber} with {TaskCount} tasks and {PartCount} parts",
                workOrder.WorkOrderNumber,
                workOrder.Tasks.Count,
                workOrder.PartsUsed.Count);

            // Step 8: Save work order to Cosmos DB
            var savedWorkOrder = await cosmosDb.CreateWorkOrderAsync(workOrder, cancellationToken);

            logger.LogInformation(
                "Successfully saved work order {Id} to Cosmos DB",
                savedWorkOrder.Id);

            return savedWorkOrder;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to plan and create work order for fault {FaultId}",
                fault.Id);
            throw;
        }
    }

    /// <summary>
    /// Builds the context prompt for the AI agent with fault, technician, and parts information.
    /// </summary>
    private static string BuildPrompt(
        DiagnosedFault fault,
        IReadOnlyList<string> requiredSkills,
        List<Technician> technicians,
        List<Part> parts)
    {
        // Build technicians section
        var techniciansSection = technicians.Count > 0
            ? string.Join("\n", technicians.Select(t =>
                $"- {t.Name} (ID: {t.Id}, Department: {t.Department}, Experience: {t.ExperienceYears} years, Skills: {string.Join(", ", t.Skills)})"))
            : "No technicians with matching skills available";

        // Build parts section
        var partsSection = parts.Count > 0
            ? string.Join("\n", parts.Select(p =>
                $"- {p.Name} (ID: {p.Id}, Part#: {p.PartNumber}, Available: {p.QuantityAvailable}, Cost: ${p.UnitCost:F2})"))
            : "No specific parts identified for this repair";

        return $"""
            FAULT INFORMATION:
            - Fault Type: {fault.FaultType}
            - Machine ID: {fault.MachineId}
            - Severity: {fault.Severity}
            - Confidence: {fault.Confidence:F2}
            - Detected At: {fault.DetectedAt:yyyy-MM-dd HH:mm:ss UTC}
            - Recommended Action: {fault.RecommendedAction}
            
            REQUIRED SKILLS:
            {string.Join(", ", requiredSkills)}
            
            AVAILABLE TECHNICIANS:
            {techniciansSection}
            
            AVAILABLE PARTS:
            {partsSection}
            
            Generate a comprehensive work order with:
            1. A work order number in format WO-YYYY-NNNNNN
            2. Appropriate title and description based on the fault
            3. Priority matching severity ({fault.Severity})
            4. Type (corrective or emergency based on severity)
            5. Assign the most qualified technician (highest experience with matching skills)
            6. Detailed tasks in logical sequence with time estimates
            7. Parts required (include partId, partNumber, quantity from available parts)
            8. Total estimated duration as sum of all task durations
            
            Return only valid JSON matching the WorkOrder schema.
            """;
    }

    /// <summary>
    /// Invokes the AI agent using Foundry Agents SDK and returns the response text.
    /// </summary>
    private async Task<string> InvokeAgentAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Invoking agent {AgentName}", AgentName);

            // Get the agent instance using the registered agent name
            // This retrieves the agent definition and configuration
            var agent = projectClient.GetAIAgent(name: AgentName);

            // Invoke the agent with the prompt
            // thread: null means no conversation history (single-turn interaction)
            // options: null uses default options
            var response = await agent.RunAsync(
                prompt,
                thread: null,
                options: null,
                cancellationToken: cancellationToken);

            // Extract the text response
            // ?? = means "if null, use this instead" (like Python's "or")
            var result = response.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("Agent returned empty response");
            }

            logger.LogInformation(
                "Agent returned response ({Length} characters)",
                result.Length);

            // Clean up response - remove markdown code blocks if present
            result = CleanJsonResponse(result);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invoke agent {AgentName}", AgentName);
            throw;
        }
    }

    /// <summary>
    /// Cleans the JSON response by removing markdown code blocks and extra whitespace.
    /// LLMs sometimes wrap JSON in ```json ... ``` blocks.
    /// </summary>
    private static string CleanJsonResponse(string response)
    {
        var trimmed = response.Trim();

        // Remove markdown code blocks (```json ... ``` or ``` ... ```)
        if (trimmed.StartsWith("```"))
        {
            // Find the first newline after opening ```
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }

            // Remove closing ```
            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed[..^3];
            }
        }

        return trimmed.Trim();
    }

    /// <summary>
    /// Applies defaults and business logic to the work order after parsing from JSON.
    /// </summary>
    private WorkOrder ApplyDefaults(WorkOrder workOrder, DiagnosedFault fault)
    {
        // Generate ID if not provided
        // ??= means "assign if null" (like Python's: x = x or default_value)
        workOrder.Id = string.IsNullOrWhiteSpace(workOrder.Id)
            ? Guid.NewGuid().ToString()
            : workOrder.Id;

        // Generate work order number if not provided or invalid
        if (string.IsNullOrWhiteSpace(workOrder.WorkOrderNumber) ||
            !workOrder.WorkOrderNumber.StartsWith("WO-"))
        {
            workOrder.WorkOrderNumber = $"WO-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        // Set machine ID from fault if not provided
        if (string.IsNullOrWhiteSpace(workOrder.MachineId))
        {
            workOrder.MachineId = fault.MachineId;
        }

        // Set fault reference
        workOrder.FaultId = fault.Id;

        // Apply default status if not set
        workOrder.Status = string.IsNullOrWhiteSpace(workOrder.Status) ? "pending" : workOrder.Status.ToLower();

        // Apply default priority if not set
        // Map severity to priority: critical→critical, high→high, etc.
        if (string.IsNullOrWhiteSpace(workOrder.Priority))
        {
            workOrder.Priority = fault.Severity.ToLower() switch
            {
                "critical" => "critical",
                "high" => "high",
                "medium" => "medium",
                "low" => "low",
                _ => "medium"
            };
        }

        // Apply default type if not set
        // Critical/high severity → emergency, others → corrective
        if (string.IsNullOrWhiteSpace(workOrder.Type))
        {
            workOrder.Type = fault.Severity.ToLower() switch
            {
                "critical" => "emergency",
                "high" => "corrective",
                _ => "corrective"
            };
        }

        // Set timestamps
        var now = DateTime.UtcNow;
        if (workOrder.CreatedAt == default)
        {
            workOrder.CreatedAt = now;
        }
        workOrder.UpdatedAt = now;

        // Validate and fix estimatedDuration (should be sum of task durations)
        var totalTaskDuration = workOrder.Tasks.Sum(t => t.EstimatedDurationMinutes);
        if (workOrder.EstimatedDuration <= 0 && totalTaskDuration > 0)
        {
            workOrder.EstimatedDuration = totalTaskDuration;
            logger.LogInformation(
                "Set estimatedDuration to sum of tasks: {Duration} minutes",
                totalTaskDuration);
        }

        // Calculate estimated cost from parts
        if (workOrder.EstimatedCost == 0 && workOrder.PartsUsed.Count > 0)
        {
            workOrder.EstimatedCost = workOrder.PartsUsed.Sum(p => p.UnitCost * p.Quantity);
        }

        logger.LogDebug(
            "Applied defaults: ID={Id}, WorkOrderNumber={WorkOrderNumber}, Status={Status}, Priority={Priority}, Type={Type}",
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.Status,
            workOrder.Priority,
            workOrder.Type);

        return workOrder;
    }
}
