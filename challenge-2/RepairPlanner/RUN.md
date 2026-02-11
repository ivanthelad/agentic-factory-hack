# Running the Repair Planner Agent

## Prerequisites

1. **Azure AI Foundry Project** with a deployed model (e.g., gpt-4o)
2. **Azure Cosmos DB** account with:
   - Database created (e.g., "tire-factory-db")
   - Three containers with their partition keys:
     - `Technicians` (partition key: `/department`)
     - `PartsInventory` (partition key: `/category`)
     - `WorkOrders` (partition key: `/status`)
3. **Sample data** loaded into Cosmos DB (technicians and parts)

## Environment Variables

Set the following environment variables before running:

```bash
export AZURE_AI_PROJECT_ENDPOINT="https://your-project.api.azureml.ms"
export MODEL_DEPLOYMENT_NAME="gpt-4o"
export COSMOS_ENDPOINT="https://your-account.documents.azure.com:443/"
export COSMOS_KEY="your-cosmos-key-here"
export COSMOS_DATABASE_NAME="tire-factory-db"
```

### For local development:

Create a `.env` file or use direnv:

```bash
# .env file
AZURE_AI_PROJECT_ENDPOINT=https://your-project.api.azureml.ms
MODEL_DEPLOYMENT_NAME=gpt-4o
COSMOS_ENDPOINT=https://your-account.documents.azure.com:443/
COSMOS_KEY=your-cosmos-key-here
COSMOS_DATABASE_NAME=tire-factory-db
```

## Build and Run

```bash
# Build the project
dotnet build

# Run the agent
dotnet run
```

## What It Does

The program demonstrates the complete repair planning workflow:

1. **Initialization**
   - Reads environment variables
   - Sets up dependency injection
   - Creates AIProjectClient with DefaultAzureCredential
   - Initializes CosmosClient and services

2. **Agent Registration**
   - Registers the RepairPlannerAgent with Azure AI Foundry
   - Defines the agent's instructions and capabilities

3. **Fault Processing**
   - Creates a sample diagnosed fault (curing_temperature_excessive)
   - Calls the agent to generate a repair plan

4. **Work Order Creation**
   - Agent analyzes the fault
   - Queries available technicians with required skills
   - Fetches required parts from inventory
   - Generates detailed repair tasks
   - Assigns the most qualified technician
   - Creates the work order in Cosmos DB

5. **Output Display**
   - Shows work order summary
   - Lists all repair tasks with durations
   - Shows required parts
   - Displays complete JSON representation

## Expected Output

```
=== Repair Planner Agent - Initialization ===
Azure AI Project Endpoint: https://...
Model Deployment: gpt-4o
Cosmos DB Endpoint: https://...
Cosmos DB Database: tire-factory-db

=== Starting Repair Planning Workflow ===

Step 1: Registering agent with Azure AI Foundry...
✓ Agent registered successfully

Step 2: Creating sample diagnosed fault...
  Fault Type: curing_temperature_excessive
  Machine ID: machine-001
  Severity: critical
  Recommended Action: Replace heating element controller...

Step 3: Generating repair plan and creating work order...
(This may take 15-30 seconds...)

=== Work Order Created Successfully ===

Work Order Number: WO-20250108-001
Status: open
Priority: critical
Type: corrective
Machine ID: machine-001
Assigned To: tech-001
Estimated Duration: 180 minutes
Number of Tasks: 4
Parts Required: 2

--- Tasks Summary ---
  1. Safety lockout and system isolation (15 min)
     Skills: electrical_systems, safety_procedures
  2. Replace heating element (60 min)
     Skills: tire_curing_press, electrical_systems
  3. Replace temperature sensor (45 min)
     Skills: instrumentation, sensor_alignment
  4. Calibration and testing (60 min)
     Skills: temperature_control, instrumentation

--- Parts Required ---
  • TCP-HTR-4KW (Qty: 1)
  • GEN-TS-K400 (Qty: 1)

=== Complete Work Order (JSON) ===
{
  "id": "...",
  "workOrderNumber": "WO-20250108-001",
  ...
}

=== Repair Planning Workflow Completed Successfully ===
```

## Troubleshooting

### Authentication Error
- Ensure you're authenticated with Azure: `az login`
- DefaultAzureCredential uses your Azure CLI credentials

### Cosmos DB Connection Error
- Verify COSMOS_ENDPOINT and COSMOS_KEY are correct
- Check that containers exist with correct partition keys

### No Technicians/Parts Found
- Ensure sample data is loaded in Cosmos DB
- Check that Technicians have `availability: "available"`
- Verify Parts exist in PartsInventory container

### Agent Error
- Verify MODEL_DEPLOYMENT_NAME matches your deployment
- Check Azure AI Foundry project permissions
- Ensure the model deployment is active
