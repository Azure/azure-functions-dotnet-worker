![Azure Functions Logo](https://raw.githubusercontent.com/Azure/azure-functions-cli/refs/heads/main/eng/res/functions.png)

|Branch|Status|
|---|---|
|main|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/.NET%20Worker/.NET%20Worker?branchName=main)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=45&branchName=main)|

# Azure Functions .NET Worker

Welcome to the Azure Functions .NET Worker Repository. Azure Functions' **Isolated Worker Model** is the recommended model for .NET functions. It moves function execution into a separate language worker process, giving you full control over your application's dependencies and enabling advanced .NET features such as middleware and dependency injection.

With the Isolated Worker Model, you build an executable that imports the .NET Isolated language worker as a NuGet package. Your app includes a [`Program.cs`](samples/FunctionApp/Program.cs) that starts the worker.

## Binding Model

.NET Isolated introduces a new binding model, slightly different from the binding model exposed in .NET in-process Azure Functions. More information can be [found here](https://github.com/Azure/azure-functions-dotnet-worker/wiki/.NET-Worker-bindings). Please review our samples for usage information.

## Middleware

The Azure Functions .NET Isolated supports middleware registration, following a model similar to what exists in ASP.NET and giving you the ability to inject logic into the invocation pipeline, pre and post function executions.

## Samples

You can find samples on how to use different features of the .NET Worker under `samples` ([link](https://github.com/Azure/azure-functions-dotnet-worker/tree/main/samples)).

## Create and run .NET Isolated Worker functions

Please see our [Guide for running C# Azure Functions in an isolated worker process](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide) for information on how to develop, debug and deploy using the Isolated Worker model.

## Extension payload transfer

The normal gRPC worker advertises `ExtensionPayload="1"` based only on transport support. Initialization and environment reload do not inspect payload paths, open files, enumerate directories, or create an archive. The capability is not a promise that deployment files are available or an instruction to send; a worker sends no payload without an explicit Host request.

Only when requested, the worker validates that `FUNCTIONS_APPLICATION_DIRECTORY` identifies a fully qualified worker-local directory containing readable `extensions.json` and an accessible `.azurefunctions` directory. Missing, malformed, or inaccessible payload inputs fail that request with a correlated terminal failure and local diagnostics, not worker initialization.

The Host requests a payload while acquiring its first accepted extension source, then reuses that source for later or concurrent worker links instead of requesting or comparing additional payloads. Later workers may still advertise support; the Host decides whether acquisition is needed.

`GetExtensionPayloadRequest` on the existing EventStream creates a temporary ZIP containing the root manifest and recursive `.azurefunctions` assets. The producer removes the SDK-owned Host metadata-loader startup from the archived `extensions.json` and excludes only its known root `Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll` and `.pdb` files. Deployment originals and ordinary co-located build/publish output are not changed, and no SDK opt-out is required. Other manifest fields, entries, hints, real extension assets, and shared dependencies are preserved; an already-filtered manifest is copied unchanged.

Discovered nested links are skipped without traversal, and linked root inputs are rejected. The limits are 256 MiB compressed, 1 GiB of archived file contents, 1 MiB each for manifest input and filtered output, 10,000 filesystem entries including implicit directories, and 32 path segments. Malformed or oversized manifests fail the request and remove its temporary ZIP. The archive has no comment or ZIP64 end records.

Responses share the request's outer `request_id`. Chunks contain at most 64 KiB, fit the configured protobuf-envelope limit, and have cumulative byte offsets. A live transfer ends with one success or failure result. Cancellation belongs to connection loss or worker shutdown; there is no payload-cancellation message or recoverable per-request cancellation on the same link. Shutdown cancels preparation or sending and drains in-flight writes before releasing the temporary ZIP. No terminal response is required on a closed or shutting-down connection. Payload writes await the actual serialized gRPC write, not admission to the logging queue.

Native-host workers do not advertise this capability: their current ABI reports queue admission rather than transport completion. They reject an unexpected request without preparing a ZIP. No new RPC service or HTTP download is introduced.

Worker indexing uses the same validated worker-local application directory when configured, including the `functions.metadata` fallback provider. When the variable is absent, the legacy metadata request directory is preserved. The Host and WorkerProxy do not gain access to these files.

## Running E2E Tests

### Requirements

- [Powershell 7](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.2)
- [CosmosDb Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21)
- Azurite (the set up script will download this automatically)

### Instructions

1. Run `setup-e2e-tests.ps1`. Once the build succeeds and the emulators are started correctly, you are done with the setup.
1. Run `run-e2e-tests.ps1` to run the tests or use the Test Explorer in VS.

**Note:** Do **not** add the switch to skip the core-tools download when running `set-up-e2e-tests.ps1` as it will lead to an incomplete setup. 

## Deploying to Azure

### Create the Azure resources

1. To deploy the app, first ensure that you've installed the Azure CLI. 

2. Login to the CLI.

    ```bash
    az login
    ```

3. If necessary, use `az account set` to select the subscription you want to use.
  
4. Create a resource group, Storage account, and Azure Functions app. If you would like to use an existing Azure Functions app, please skip this step.

    ```bash
    az group create --name AzureFunctionsQuickstart-rg --location westeurope
    az storage account create --name <STORAGE_NAME> --location westeurope --resource-group AzureFunctionsQuickstart-rg --sku Standard_LRS
    az functionapp create --resource-group AzureFunctionsQuickstart-rg --consumption-plan-location westeurope --runtime dotnet-isolated --functions-version 4 --name <APP_NAME> --storage-account <STORAGE_NAME>
    ```

### Deploy the app

1. Ensure you are in your functions project folder.
2. Deploy the app.

    ```bash
    func azure functionapp publish <APP_NAME>
    ```

## Feedback

Please create issues in this repo. Thanks!

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
