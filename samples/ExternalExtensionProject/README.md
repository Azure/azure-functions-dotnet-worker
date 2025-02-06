# External Extension Project

This sample shows how to supply the worker extension project manually.

## What is the worker extension project?

To enable extensions in dotnet-isolated function apps an extension bundle needs to be loaded into the host process (separate from your worker process). For example, if you use `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`, an host-side extension `Microsoft.Azure.WebJobs.Extensions.ServiceBus` also needs to be loaded into the function host.

Loading of these extensions is accomplished by build steps provided by `Microsoft.Azure.Functions.Worker.Sdk`. These steps will scan for WebJobs extensions indicated by Worker extensions. These extensions are collected and a _new_ csproj is dynamically generated during build called `WorkerExtensions.csproj`. This project is then restored, built, and outputs collected into the `.azurefunctions` folder of your function app build output. This process is often referred to as the "extension inner-build".

## What is this scenario for?

For most customers, this inner-build process is frictionless and requires no customization. However, for some customers this process conflicts with some external factors (no network during build, nuget feed auth issues, among others). To accommodate these conflicts SDK `2.1.0` and on supports the ability to externally supply this extension project, giving full control of the extension project to the customer. This project can now be restored and built alongside the function app. Since the csproj is controlled by the customer, any changes can be made to it.

There is a major drawback though: ensuring the extension project builds a *valid* payload is now the customer's responsibility. Failures to build a valid payload will only be discovered at runtime. Issues may be obscure and varied, from assembly load failures, method missing exceptions, to odd behavior due to mismatching worker & webjobs extensions. Any time the set of extensions for the function app changes, this external project will need to be manually updated. As such, this scenario is only recommended if customization is **absolutely** necessary.

## How to use external extension project feature

### 1. Prepare the project for external extension
Add the follow item to your csproj:

``` diff
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

+  <ItemGroup>
+    <ProjectReference Include="{{path-to-extension-csproj}}" ReferenceOutputAssembly="false" WorkerExtensions="true" />
+  </ItemGroup>

  <!-- rest of csproj -->
</Project>
```

### 2. First time generation of external extension csproj

Run a target to generate the extension project one-time:

``` shell
dotnet build -t:GenerateExtensionProject {{path-to-function-app-csproj}}
```

This will generate the same csproj as the inner-build would. Absent of any external influences by your build process (ie, directory-based imports), this project _should_ produce a valid extension bundle.

> [!NOTE]
> The target `GenerateExtensionProject` can be ran whenever to regenerate the csproj. **However**, it will overwrite the contents of the csproj indicated by `ExtensionsCsProj` each time. Make sure to re-apply any customizations you have!

> [!TIP]
> To avoid needing to re-apply customizations, this sample shows putting all custom logic into `Directory.Build.props` and `Directory.Build.targets` and leaving the csproj to always be the generated contents.

### 3. Add the extension project to be built as part of your regular build

If using a solution, make sure to add this new project to the solution file. Failure to do so may cause Visual Studio to skip building this project.

## Things to be aware of

❌ DO NOT change the `TargetFramework` of the extension project

> [!CAUTION]
> The target framework, and all dependent assemblies of this generated project, must be compatible with the host process. Changing the TFM risks assembly load failures.

⚠️ AVOID changing the packages of the extension project

> [!WARNING]
> The package closure of the extension project is sensitive to changes. The host process ultimately controls the dependency graph and assembly loads. Depending on a package/assembly not supported by the host process may cause issues. E.G, trying to depend on any `Microsoft.Extensions.*/9x` from the extension project will cause issues.

❌ DO NOT include more than 1 `ProjectReference` with `WorkerExtensions=true`

> [!NOTE]
> The build will be intentionally failed if there are more than 1 extension projects declared.

✔️ DO set `ReferenceOutputAssembly=false` on the `ProjectReference` with `WorkerExtensions=true`

> [!IMPORTANT]
> Setting `ReferenceOutputAssembly=false` will exclude this extensions projects package references from being included in your function app. This is not done automatically as it needs to be present for restore phase (and the functions SDK targets are not present until _after_ restore).
