# Development tests

Run commands from the repository root.

## Azure Decision API tests

Run the unit tests with:

```powershell
dotnet test development/Tests/Tests.csproj --configuration Release --filter FullyQualifiedName~AzureOpenAIDecisionFactoryTests
```

The live tests read `AzureDecisionEndpoint`, `AzureDecisionDeployment`, and
`AzureDecisionApiKey` from the shared .NET User Secrets store used by
`development/Secrets/Secrets.csproj`:

```powershell
dotnet user-secrets set "AzureDecisionEndpoint" "https://YOUR_RESOURCE.services.ai.azure.com" --project development/Secrets/Secrets.csproj
dotnet user-secrets set "AzureDecisionDeployment" "Decision-1" --project development/Secrets/Secrets.csproj
dotnet user-secrets set "AzureDecisionApiKey" "YOUR_API_KEY" --project development/Secrets/Secrets.csproj
```

Sign in with Azure CLI for RBAC access. Run all four live cases, covering API-key
and RBAC authentication, with:

```powershell
dotnet test development/Tests/Tests.csproj --configuration Release --filter FullyQualifiedName~AzureOpenAIDecisionFactoryLiveTests
```

Live tests make billable API calls.
