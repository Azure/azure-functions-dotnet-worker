# Azure Functions Language Worker Protobuf

|Branch|Status|
|---|---|
|main|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/.NET%20Worker/.NET%20Worker?branchName=main)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=45&branchName=main)|
|release/1.x|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/.NET%20Worker/.NET%20Worker?branchName=release%2F1.x)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=45&branchName=release%2F1.x)|

To use this repo in Azure Functions language workers, follow steps below to add this repo as a subtree (*Adding This Repo*). If this repo is already embedded in a language worker repo, follow the steps to update the consumed file (*Pulling Updates*).

Learn more about Azure Function's projects on the [meta](https://github.com/azure/azure-functions) repo.

## Adding This Repo

From within the Azure Functions language worker repo:
1.	Define remote branch for cleaner git commands
    -	`git remote add proto-file https://github.com/azure/azure-functions-language-worker-protobuf.git`
    -	`git fetch proto-file`
2.	Index contents of azure-functions-worker-protobuf to language worker repo
    -	`git read-tree  --prefix=<path in language worker repo> -u proto-file/<version branch>`
3.	Add new path in language worker repo to .gitignore file
    -   In .gitignore, add path in language worker repo
4.	Finalize with commit
    -	`git commit -m "Added subtree from https://github.com/azure/azure-functions-language-worker-protobuf. Branch: <version branch>. Commit: <latest protobuf commit hash>"`
    -	`git push`

## Pulling Updates

From within the Azure Functions language worker repo:
1.	Define remote branch for cleaner git commands
    -	`git remote add proto-file https://github.com/azure/azure-functions-language-worker-protobuf.git`
    -	`git fetch proto-file`
2.	Pull a specific release tag
    -   `git fetch proto-file refs/tags/<tag-name>`
        -   Example: `git fetch proto-file refs/tags/v1.1.0-protofile`
3.	Merge updates
    -   Merge with an explicit path to subtree: `git merge -X subtree=<path in language worker repo> --squash <tag-name> --allow-unrelated-histories --strategy-option theirs`
        -   Example: `git merge -X subtree=src/WebJobs.Script.Grpc/azure-functions-language-worker-protobuf --squash v1.1.0-protofile --allow-unrelated-histories --strategy-option theirs`
4.	Finalize with commit
    -	`git commit -m "Updated subtree from https://github.com/azure/azure-functions-language-worker-protobuf. Tag: <tag-name>. Commit: <commit hash>"`
    -	`git push`

## Releasing a Language Worker Protobuf version

1.	Draft a release in the GitHub UI
    -   Be sure to include details of the release
2.	Create a release version, following semantic versioning guidelines ([semver.org](https://semver.org/))
3.	Tag the version with the pattern: `v<M>.<m>.<p>-protofile` (example: `v1.1.0-protofile`)
3.	Merge `dev` to `master`

## Consuming FunctionRPC.proto
*Note: Update versionNumber before running following commands*

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
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
