# Azure Functions .NET Worker SDK Rules

Rules emitted by [Azure.Functions.Sdk](../../src/Azure.Functions.Sdk/README.md) during build and restore. These diagnostics use the `AZFW01xx` ID range.

| Rule ID | Name | Severity | Description |
| --- | --- | --- | --- |
| [AZFW0100](AZFW0100.md) | CannotRunFuncCli | Error | Unable to launch `func` CLI when running `dotnet run`. |
| [AZFW0101](AZFW0101.md) | ExtensionPackageConflict | Error | The same extension package was added with conflicting versions. |
| [AZFW0102](AZFW0102.md) | ExtensionPackageDuplicate | Warning | The same extension package was added with identical versions. |
| [AZFW0103](AZFW0103.md) | InvalidExtensionPackageVersion | Error | An extension package was resolved with an invalid version. |
| [AZFW0104](AZFW0104.md) | EndOfLifeFunctionsVersion | Warning | The `AzureFunctionsVersion` value is out of support. |
| [AZFW0105](AZFW0105.md) | UsingIncompatibleSdk | Error | An incompatible SDK package reference was detected. |
| [AZFW0106](AZFW0106.md) | UnknownFunctionsVersion | Error | The `AzureFunctionsVersion` value is unknown. |
| [AZFW0107](AZFW0107.md) | UnsupportedTargetFramework | Warning | The `TargetFramework` is unsupported for this function app. |
| [AZFW0108](AZFW0108.md) | ExtensionsNotRestored | Warning | Extension bundle was not restored prior to build. |
| [AZFW0109](AZFW0109.md) | GeneratedProjectShouldNotBeBuilt | Warning | The generated `azure_functions.g.csproj` is being built directly. |
