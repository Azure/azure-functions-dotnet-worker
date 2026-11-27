# Configuration

This sample demonstrates how to configure the Azure Functions Dotnet Isolated Worker. See `Program.cs` for details.

It also shows how to add configuration sources with `ConfigureAppConfiguration` and how to read `IConfiguration` in `ConfigureServices`. Both use the overloads that take a `HostBuilderContext`, which replace `IFunctionsConfigurationBuilder` and `IFunctionsHostBuilder` from the in-process model.
