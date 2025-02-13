# External Extension Project

This sample shows how to supply the worker extension project manually.

## What is the worker extension project?

To support triggers and bindings in dotnet-isolated function apps, an extensions payload needs to be constructed and loaded into the host process (separate from your worker process). For example, if you use `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`, a host-side extension `Microsoft.Azure.WebJobs.Extensions.ServiceBus` also needs to be loaded into the function host.

Collecting these extensions to be loaded is accomplished by build steps provided by `Microsoft.Azure.Functions.Worker.Sdk`. These steps will scan for WebJobs extensions indicated by Worker extensions. These extensions are added as `PackageReference`'s to a _new_ `WorkerExtensions.csproj` which is dynamically generated during build. This project is then restored, built, and outputs collected into the `.azurefunctions` folder of your function app build output. This process is often referred to as the "extension inner-build".

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

This will generate the same csproj as the inner-build would. Absent of any external influences by your build process (ie, directory-based imports), this project _should_ produce a valid extension payload.

> [!NOTE]
> The target `GenerateExtensionProject` can be ran whenever to regenerate the csproj. **However**, it will overwrite the contents of the csproj indicated by `ProjectReference` each time. Make sure to re-apply any customizations you have!

> [!TIP]
> To avoid needing to re-apply customizations, this sample shows putting all custom logic into `Directory.Build.props` and `Directory.Build.targets` and leaving the csproj to always be the generated contents.

### 3. Add the extension project to be built as part of your regular build

If using a solution, make sure to add this new project to the solution file. Failure to do so may cause Visual Studio to skip building this project.

## Example Scenarios

### Scenario 1. No-network build phase

In some cases, a CI's build phase may restrict network access. If this network restriction blocks access to nuget feeds, then the extension inner-build will fail. Using external extension project and ensuring it is part of your existing restore phase will workaround this issue. No project customization is needed by default, unless there are rules enforced by your CI (such as mandating central package versioning). The exact changes needed in those cases will be your responsibility to determine and implement.

### Scenario 2. Authenticated nuget feeds

The extension inner-build inherits the nuget configuration of your function app. If the configured feeds require authentication there are two routes:

1. First, see if you can authenticate using your CI's features. For example, in Azure Devops see [NuGetAuthenticate@1](https://learn.microsoft.com/azure/devops/pipelines/tasks/reference/nuget-authenticate-v1?view=azure-pipelines).
2. If option 1 does not work and there is no feasible way to pass in authentication context into the extension inner-build, then performing same steps as [scenario 1](#scenario-1-no-network-build-phase) may workaround the auth issue.

### Scenario 3. Extension development testing

This feature is useful for extension development itself, as the `PackageReference` for the WebJobs extension can be replaced with a `ProjectReference`.

``` diff
<ItemGroup>
  <PackageReference Include="Microsoft.NETCore.Targets" Version="3.0.0" PrivateAssets="all" />
  <PackageReference Include="Microsoft.NET.Sdk.Functions" Version="4.6.0" />
- <PackageReference Include="Some.WebJobs.Extension.In.Development" Version="1.0.0" />
+ <ProjectReference Include="../Some.WebJobs.Extension.In.Development/Some.WebJobs.Extension.In.Development.csproj" />
</ItemGroup>
```

With the above change you can have a function app to locally test your extension without any need for nuget pack and publishing.

### Scenario 4. Pinning a transitive dependency

In the case where an extension brings in a transitive dependency that is not compliant with some CI scans or rules you have, you can manually pin it to an in-compliance version.

``` diff
<ItemGroup>
  <PackageReference Include="Microsoft.NETCore.Targets" Version="3.0.0" PrivateAssets="all" />
  <PackageReference Include="Microsoft.NET.Sdk.Functions" Version="4.6.0" />
  <PackageReference Include="Some.WebJobs.Extension" Version="1.0.0" />
+ <PackageReference Include="Some.Transitive.Dependency" Version="1.1.0" />
</ItemGroup>
```

> [!CAUTION]
> Be very careful with this scenario, as pinning may bring in runtime breaking changes. Especially be careful about pinning across major versions. If the transitive dependency is `vN.x.x`, and you pin to `vN+y.x.x`, this may lead to runtime failures. It is recommended you validate the version you are pinning to is compatible with the originally requested version.

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
