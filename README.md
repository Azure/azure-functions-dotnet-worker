![Azure Functions Logo](https://raw.githubusercontent.com/Azure/azure-functions-cli/master/src/Azure.Functions.Cli/npm/assets/azure-functions-logo-color-raster.png)

|Branch|Status|
|---|---|
|main|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/.NET%20Worker/.NET%20Worker?branchName=main)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=45&branchName=main)|
|release/1.x|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/.NET%20Worker/.NET%20Worker?branchName=release%2F1.x)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=45&branchName=release%2F1.x)|


# Azure Functions .NET Worker

Welcome to the Azure Functions .NET Worker Repository. The .NET Worker provides .NET 5 support in Azure Functions, introducing an **Isolated Model**, running as an out-of-process language worker that is separate from the Azure Functions runtime. This allows you to have full control over your application's dependencies as well as other new features like a middleware pipeline.

A .NET Isolated function app works differently than a .NET Core 3.1 function app. For .NET Isolated, you build an executable that imports the .NET Isolated language worker as a NuGet package. Your app includes a [`Program.cs`](samples/FunctionApp/Program.cs) that starts the worker.

## Binding Model

.NET Isolated introduces a new binding model, slightly different from the binding model exposed in .NET Core 3 Azure Functions. More information can be [found here](https://github.com/Azure/azure-functions-dotnet-worker/wiki/.NET-Worker-bindings). Please review our samples for usage information.

## Middleware

The Azure Functions .NET Isolated supports middleware registration, following a model similar to what exists in ASP.NET and giving you the ability to inject logic into the invocation pipeline, pre and post function executions.

## Samples

You can find samples on how to use different features of the .NET Worker under `samples` ([link](https://github.com/Azure/azure-functions-dotnet-worker/tree/main/samples)).

## Create and run .NET Isolated functions

**Note: Visual Studio and Visual Studio Code support is on the way. In the meantime, please use `azure-functions-core-tools` or the sample projects as a starting point.**

### Install .NET 5.0
Download .NET 5.0 [from here](https://dotnet.microsoft.com/download/dotnet/5.0)

### Install the Azure Functions Core Tools

To download Core Tools, please check out our docs at [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)

### Create a .NET Isolated project
In an empty directory, run `func init` and select `dotnet (Isolated Process)`.

### Add a function
Run `func new` and select any trigger (`HttpTrigger` is a good one to start). Fill in the function name.

### Run functions locally
Run `func host start` in the sample app directory.

**Note:** If you selected a trigger different from `HttpTrigger`, you may need to setup local connection strings or emulator for the trigger service.

### Debugging

#### Visual Studio

Debugging for the Isolated model is supported in Visual Studio 2019 and 2022 with the Azure Development workloads support installed.

#### JetBrains Rider

> NOTE: To debug your Worker, you must be using the Azure Functions Core Tools version 3.0.3381 or higher. You must also have the [Azure Toolkit for Rider](https://plugins.jetbrains.com/plugin/11220-azure-toolkit-for-rider) installed.

In Rider, make sure a Run Configuration is generated for your Azure Functions project is active. You can also create a custom Run Configuration from the **Run \| Edit Configurations...** menu.

To start debugging, select the run configuration and start debugging. This will compile your project, run the Core Tools, and attach the debugger to your project.

Under the hood, Rider launches the Core Tools with the `--dotnet-isolated-debug` argument, and attached to the process ID for your worker process.

You can place a breakpoint in any function, and inspect your code as it is running. Note that [debugging startup code may timeout (#434)](https://github.com/Azure/azure-functions-dotnet-worker/issues/434).

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
  
4. Create a resource group, Storage account, and Azure Functions app. If you would like to use an existing Windows .NET Core 3 function app, please skip this step.

    ```bash
    az group create --name AzureFunctionsQuickstart-rg --location westeurope
    az storage account create --name <STORAGE_NAME> --location westeurope --resource-group AzureFunctionsQuickstart-rg --sku Standard_LRS
    az functionapp create --resource-group AzureFunctionsQuickstart-rg --consumption-plan-location westeurope --runtime dotnet-isolated --functions-version 3 --name <APP_NAME> --storage-account <STORAGE_NAME>
    ```

### Deploy the app

1. Ensure you are in your functions project folder.
2. Deploy the app.

    ```bash
    func azure functionapp publish <APP_NAME>
    ```

## Known issues

* Optimizations are not all in place in the consumption plan and you may experience longer cold starts.

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
