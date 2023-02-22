# Worker.Extensions.Shared

This is much like the shared-project `.shproj` where files are included directly in referencing projects. The purpose of this is to let us have common code between projects that we do not want to be part of our public API surface.

## What belongs here?

Include helpers, utility classes, or extension methods here. Keep in mind the dependencies of the types you include here, as this is intended for minimal dependencies. Try to keep the dependencies here rather "Core". If it is not good enough for **ALL** `Worker.Extensions.*` packages to reference it, it does not belong here.

✅ **GOOD** using base class library types
✅ **GOOD** "Core" `Microsoft.Extensions.*` packages (ie `Microsoft.Extensions.Configuration`)
❌ **BAD** Specific Azure services. ie: `Azure.Storage.*`
❌ **BAD** AspNetCore libraries `Microsoft.AspNetCore.*`

## Adding files

Just add any `.cs` or `.resx` file here as needed. Some things to keep in mind:

❌ **never** include `public` types. \
✅ `internal` or `private` are fine. \
⚠️ consider rooting the namespace of shared types as `Microsoft.Azure.Functions.Worker.Extensions`. \

## How to reference

❌ **BAD** Do not include this as a `ProjectReference`

``` xml
<ItemGroup>
  <ProjectReference Include="../../Worker.Extensions.Shared/Worker.Extensions.Shared.csproj" />
</ItemGroup>
```

✅ **GOOD** Include this project as a `SharedReference`

``` xml
<ItemGroup>
  <SharedReference Include="../../Worker.Extensions.Shared/Worker.Extensions.Shared.csproj" />
</ItemGroup>
```

## How does it work?

The `.csproj` in this directly exists for two purposes: (1) showing files in Visual Studio. and (2) listing _what_ items to include via `MSBuild` targets. When you include `<SharedReference>`, that project will call into project in the `Include` to ask it "What are your compile items?" "What are your embedded resources?" and then the calling project will include those directly into its own compile and embedded resource item groups.

This project uses `Microsoft.Build.NoTargets` SDK, which means it has absolutely **zero** build output. There is no `dll` and no `nupkg` package produced from this project. Instead, all files are directly linked into projects that reference this via a `SharedReference Include=".."` msbuild item.
