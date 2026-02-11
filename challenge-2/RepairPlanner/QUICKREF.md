# Program.cs Quick Reference

## 🎯 What It Does
Entry point for the Repair Planner Agent that demonstrates the complete workflow from fault diagnosis to work order creation.

## 📦 Key Dependencies
```csharp
using Azure.AI.Projects;         // AIProjectClient, agent SDK
using Azure.Identity;            // DefaultAzureCredential
using Microsoft.Azure.Cosmos;    // CosmosClient
using Microsoft.Extensions.DependencyInjection;  // DI container
using Microsoft.Extensions.Logging;              // Logging
```

## 🔑 Environment Variables (All Required)
```bash
AZURE_AI_PROJECT_ENDPOINT     # e.g., https://your-project.api.azureml.ms
MODEL_DEPLOYMENT_NAME         # e.g., gpt-4o
COSMOS_ENDPOINT               # e.g., https://your-account.documents.azure.com:443/
COSMOS_KEY                    # Your Cosmos DB key
COSMOS_DATABASE_NAME          # e.g., tire-factory-db
```

## 🏗️ Service Registration Order
```csharp
1. Logging (Console, Information level)
2. AIProjectClient (with DefaultAzureCredential)
3. CosmosClient (endpoint + key)
4. CosmosDbService (wraps Cosmos operations)
5. IFaultMappingService (fault→skills/parts)
6. RepairPlannerAgent (main orchestrator)
```

## 🔄 Workflow Steps
```
Initialize → Register Agent → Create Fault → Plan Repair → Save Work Order → Display Results
```

## 📊 Sample Output Structure
```
=== Initialization ===
[Environment config displayed]

=== Workflow ===
Step 1: Register agent ✓
Step 2: Create sample fault
Step 3: Generate plan (15-30s)

=== Results ===
Work Order Summary
Tasks (with durations/skills)
Parts Required
Complete JSON
```

## 🐛 Error Handling
- Missing env vars → InvalidOperationException with clear message
- Runtime errors → Full exception details + stack trace
- Exit code 1 on failure

## 🧪 Testing Locally
```bash
# 1. Set environment variables
export AZURE_AI_PROJECT_ENDPOINT="..."
# ... (set all 5 variables)

# 2. Authenticate
az login

# 3. Run
dotnet run
```

## 📝 Key C# Patterns for Python Devs
| C# | Python Equivalent |
|----|-------------------|
| `??` | `or` |
| `??=` | `x = x or default` |
| `await using` | `async with` |
| `GetRequiredService<T>()` | Dependency injection |
| Primary constructor | `__init__` |
| Lambda `sp =>` | `lambda sp:` |

## 🔗 Related Files
- `RepairPlannerAgent.cs` - Agent logic
- `Models/DiagnosedFault.cs` - Input model
- `Models/WorkOrder.cs` - Output model
- `Services/CosmosDbService.cs` - DB operations
- `Services/FaultMappingService.cs` - Mappings

## ⚡ Quick Commands
```bash
# Build
dotnet build

# Run
dotnet run

# Clean + Rebuild
dotnet clean && dotnet build

# Release build
dotnet build -c Release
```

## 🎓 Learning Points
1. **Dependency Injection**: All services registered in ServiceCollection
2. **Async/Await**: Async throughout, with proper cancellation tokens
3. **Resource Management**: `await using` ensures cleanup
4. **Error Handling**: Try-catch with detailed messages
5. **Logging**: Structured logging with ILogger<T>
6. **Configuration**: Environment-based, validated at startup
7. **Azure Integration**: DefaultAzureCredential for auth
8. **Data Access**: Cosmos DB via SDK

## 🚀 Next Steps After Running
1. Review the generated work order JSON
2. Check Cosmos DB for the created work order
3. Verify technician assignment logic
4. Test with different fault types
5. Add integration tests
6. Connect to Challenge 3 (Scheduling)

## 📚 Additional Resources
- [Azure AI Foundry Agents SDK](https://learn.microsoft.com/azure/ai-studio/)
- [Cosmos DB .NET SDK](https://learn.microsoft.com/azure/cosmos-db/nosql/sdk-dotnet-v3)
- [Dependency Injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)

---
**Status**: ✅ Fully Implemented | ✅ Builds Successfully | ✅ Ready to Run
