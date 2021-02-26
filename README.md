# Azure Functions .NET Isolated

Welcome to a preview of .NET Isolated in Azure Functions. .NET Isolated provides .NET 5 support in Azure Functions. It runs in an out-of-process language worker that is separate from the Azure Functions runtime. This allows you to have full control over your application's dependencies as well as other new features like a middleware pipeline.

A .NET Isolated function app works differently than a .NET Core 3.1 function app. For .NET Isolated, you build an executable that imports the .NET Isolated language worker as a NuGet package. Your app includes a [`Program.cs`](FunctionApp/Program.cs) that starts the worker.

As this is a preview, there may be some breaking changes to be expected.

## Binding Model

.NET Isolated introduces a new binding model, slightly different from the binding model exposed in .NET Core 3 Azure Functions. More information can be [found here](https://github.com/Azure/azure-functions-dotnet-worker/wiki/.NET-Worker-bindings). Please review our samples for usage information.

## Middleware

The Azure Functions .NET Isolated supports middleware registration, following a model similar to what exists in ASP.NET and giving you the ability to inject logic into the invocation pipeline, pre and post function executions.

While the full middleware registration set of APIs is not yet exposed, middleware registration is supported and we've added an [example](https://github.com/Azure/azure-functions-dotnet-worker-preview/tree/main/FunctionApp/Middleware) to the sample application under the `Middleware` folder.

## Samples

The samples for .NET Isolated using various Azure Functions bindings are available under `samples/SampleApp`([link](https://github.com/Azure/azure-functions-dotnet-worker/tree/main/samples/SampleApp))

## How to run the sample

**Note: Templates and tooling support for .NET Isolated are on the way. In the meanwhile, please use our sample projects as a starting point.**

### Install .NET 5.0
Download .NET 5.0 [from here](https://dotnet.microsoft.com/download/dotnet/5.0)

### Install the Azure Functions Core Tools
Please make sure you have Azure Functions Core Tools >= `3.0.3284`.

To download, please check out our docs at [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)

### Obtain functions project locally
Download or clone the `samples/SampleApp` repository, and setup the relevant functions with connection strings (similar to a .NET Core 3 function).

### Run functions locally
Run `func host start` in the sample app directory.

**Note:** the entire project will not run initially as it requires several connection strings to the services. We suggest you only keep the functions you are trying, and remove others to start. Http functions do not require any setup.

### Attaching the debugger

#### Visual Studio

To debug in Visual Studio, uncomment the `Debugger.Launch()` statements in *Program.cs*. The process will attempt to launch a debugger before continuing.

**YOU CAN NOT DEBUG DIRECTLY USING "Start Debugging" IN VISUAL STUDIO DIRECTLY.** You need to use the command line as mentioned in the previous **Run the sample locally** part of this readme.

We're working with the Visual Studio team to provide an integrated debugging experience.

## Deploying to Azure

### Create the Azure resources

1. To deploy the app, first ensure that you've installed the Azure CLI. 

2. Login to the CLI.

    ```bash
    az login
    ```

3. If necessary, use `az account set` to select the subscription you want to use.
  
4. Create a resource group, Storage account, and Azure Functions app.

    ```bash
    az group create --name AzureFunctionsQuickstart-rg --location westeurope
    az storage account create --name <STORAGE_NAME> --location westeurope --resource-group AzureFunctionsQuickstart-rg --sku Standard_LRS
    az functionapp create --resource-group AzureFunctionsQuickstart-rg --consumption-plan-location westeurope --runtime dotnet --functions-version 3 --name <APP_NAME> --storage-account <STORAGE_NAME>
    ```

### Deploy the app

1. Ensure you're in your functions project (`SampleApp`) folder.
2. Deploy the app.

    ```bash
    func azure functionapp publish <APP_NAME>
    ```
## Known issues

* Deployment to Azure is currently limited to Windows plans. Note that some optimizations are not in place in the consumption plan and you may experience longer cold starts.

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
