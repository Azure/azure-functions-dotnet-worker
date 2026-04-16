# Microsoft.Azure.Functions.Worker.Sdk

This package provides development-time support for Azure Functions .NET isolated worker apps, including MSBuild build and publish targets, a Roslyn source generator for function metadata, and analyzers.

## Getting Started

This package is typically added via the Azure Functions project template. To add it manually:

```dotnet
dotnet add package Microsoft.Azure.Functions.Worker.Sdk
```

For more information, see the [Azure Functions .NET isolated worker guide](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide).

## What's Included

### Build & Publish Targets

The SDK adds MSBuild targets that run during build and publish:

- **Function indexing** — Analyzes your compiled assembly and generates function metadata (`functions.metadata`) so the Azure Functions host can discover your functions without runtime reflection.
- **Extension bundling** — Auto-generates a `WorkerExtensions` project that compiles all binding extension packages into a single assembly.
- **Publish support** — Configures output for deployment, including Docker base image assignment for container scenarios.

### Source Generators

Three Roslyn source generators run at compile time:

- **FunctionMetadataProvider** — Generates a `IFunctionMetadataProvider` implementation that returns metadata for all `[Function]`-attributed methods. This enables build-time indexing for fast cold starts.
- **FunctionExecutor** — Generates strongly-typed wrapper methods for invoking user functions with correct parameter binding.
- **ExtensionStartupRunner** — Generates a startup orchestrator that calls `Configure()` on all extension startup types.

### Analyzers

| Diagnostic | Severity | Description |
|------------|----------|-------------|
| AZFW0001 | Error | WebJobs binding attributes (in-process) used instead of isolated worker attributes |
| AZFW0002 | Error | Async function method returns `void` instead of `Task` |
| AZFW0009 | Error | `SupportsDeferredBinding` applied to an output binding |
| AZFW0010 | Warning | Parameter type not supported by the binding attribute |
| AZFW0011 | Error | Blob container path requires an iterable type or `BlobContainerClient` |

## Configuration

The SDK exposes MSBuild properties you can set in your `.csproj` to control its behavior:

```xml
<PropertyGroup>
  <!-- Azure Functions runtime version (default: v4) -->
  <AzureFunctionsVersion>v4</AzureFunctionsVersion>

  <!-- Disable build-time function indexing (default: true) -->
  <FunctionsEnableWorkerIndexing>true</FunctionsEnableWorkerIndexing>

  <!-- Control metadata source generation independently (default: mirrors FunctionsEnableWorkerIndexing) -->
  <FunctionsEnableMetadataSourceGen>true</FunctionsEnableMetadataSourceGen>

  <!-- Control executor source generation independently (default: true) -->
  <FunctionsEnableExecutorSourceGen>true</FunctionsEnableExecutorSourceGen>

  <!-- Namespace for generated code (default: your project's RootNamespace) -->
  <FunctionsGeneratedCodeNamespace>MyApp.Generated</FunctionsGeneratedCodeNamespace>
</PropertyGroup>
```
