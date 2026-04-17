# Azure Functions .NET Worker Analyzer Rules

This page lists all analyzer rules shipped with the Azure Functions .NET Worker packages.

## Microsoft.Azure.Functions.Worker.Sdk

| Rule ID | Severity | Description |
|---------|----------|-------------|
| [AZFW0001](AZFW0001.md) | Error | Invalid binding attributes |
| [AZFW0002](AZFW0002.md) | Error | Avoid async void |
| [AZFW0003](AZFW0003.md) | Error | Invalid base class for extension startup type |
| [AZFW0004](AZFW0004.md) | Error | Extension startup type is missing parameterless constructor |
| [AZFW0005](AZFW0005.md) | Error | Multiple Azure Functions Binding Attributes grouped together |
| [AZFW0006](AZFW0006.md) | Warning | Symbol not found |
| [AZFW0007](AZFW0007.md) | Error | Multiple HTTP response binding types for Azure Function |
| [AZFW0008](AZFW0008.md) | Error | Invalid cardinality |
| [AZFW0009](AZFW0009.md) | Error | Invalid use of SupportsDeferredBinding attribute |
| [AZFW0010](AZFW0010.md) | Warning | Invalid binding type |
| [AZFW0011](AZFW0011.md) | Error | Invalid binding type |
| [AZFW0012](AZFW0012.md) | Error | Invalid retry options |
| [AZFW0013](AZFW0013.md) | Error | Unable to parse binding argument |
| [AZFW0017](AZFW0017.md) | Error | Duplicate function name |

## Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore

| Rule ID | Severity | Description |
|---------|----------|-------------|
| [AZFW0014](AZFW0014.md) | Error | Missing registration for ASP.NET Core Integration |
| [AZFW0015](AZFW0015.md) | Error | Missing HttpResult attribute for multi-output function |
| [AZFW0016](AZFW0016.md) | Warning | Missing HttpResult attribute for multi-output function |
