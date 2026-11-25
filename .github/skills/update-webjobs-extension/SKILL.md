---
name: update-webjobs-extension
description: Update the WebJobs (host) extension reference of a .NET isolated worker extension in this repository, such as Storage Blobs/Queues, Cosmos DB, Event Hubs, Service Bus, Event Grid, Kafka, RabbitMQ, SendGrid, SignalR Service, or Tables. Use when bumping a `<WebJobsExtension>` version, preparing a worker extension package for release to pick up a host extension fix, or checking which worker extensions are behind the latest WebJobs packages on NuGet.
---

# Update a WebJobs extension reference

Each worker extension under `extensions/<Name>/src/<Name>.csproj` declares the host-side WebJobs package it pairs with:

```xml
<ItemGroup>
  <WebJobsExtension Include="Microsoft.Azure.WebJobs.Extensions.Storage.Blobs" Version="5.3.10" />
</ItemGroup>
```

`eng/build/WorkerExtensions.targets` turns this item into an `ExtensionInformationAttribute` on the worker assembly. The Functions SDK reads that attribute to decide which WebJobs package to install for the host. On CI builds (`ContinuousIntegrationBuild=true`) it also generates and restores an `ExtensionValidation.csproj` to confirm the version exists. Changing this version changes what every consuming app runs on the host, so the worker package must be versioned and released.

Reference PRs: #3546 and #3470 (Storage Blobs/Queues and metapackage), #3389 (Cosmos DB), #3494 (SignalR Service), #3059 (Event Hubs), #3006 (Tables), and #3159 (revert of an update that shipped an upstream regression).

## 1. Check NuGet before choosing the version (required)

Always run the status script. Don't rely on the issue, the upstream PR, or memory for the latest version:

```powershell
pwsh .github/skills/update-webjobs-extension/scripts/Get-WebJobsExtensionStatus.ps1                       # all extensions
pwsh .github/skills/update-webjobs-extension/scripts/Get-WebJobsExtensionStatus.ps1 -Filter '*Storage*'   # a subset
pwsh .github/skills/update-webjobs-extension/scripts/Get-WebJobsExtensionStatus.ps1 -Filter '*Storage.Blobs' -TargetVersion 5.3.10
```

For each `<WebJobsExtension>` the script reports the current version, the latest listed stable and prerelease versions on NuGet, the worker package's `VersionPrefix`, and whether that worker version is already published. With `-TargetVersion` it exits `0` if the target is the latest listed stable version, `2` if a newer stable version exists, and `1` if the target isn't listed on NuGet or the filter matched more than one package.

- Exit code `2`: stop. Tell the user which newer version exists, and only continue with the older target if they confirm a reason (for example, a known regression in the newer version, as in #3159).
- Exit code `1`, or the script can't reach NuGet: stop and report it. Don't guess versions.
- Don't take a prerelease WebJobs version into a stable worker package unless the user explicitly asks for it.
- Report the NuGet result (target vs. latest stable) to the user and in the PR description.

## 2. Review the upstream release

Before editing, read the WebJobs package's release notes or changelog (usually in `Azure/azure-sdk-for-net` or the extension's own repo) for every version between the current and target versions:

- Collect the fixes and features to summarize in the release notes, with links to the upstream PRs/issues.
- Check the package's dependencies (NuGet "Dependencies" tab or nuspec) for jumps the Functions host may not support yet. For example, WebJobs Storage 5.3.8 depended on `Microsoft.Extensions.*` 10.x, which failed to load in Core Tools until a .NET 10 based host shipped (#3470). If you find such a jump, flag it to the user before merging.
- Check for known regressions in the target version (#3159 reverted Storage 5.3.5 for this reason).
- Note whether the update addresses a security advisory in a transitive dependency (#3494); mention it in the release notes.

## 3. Update the reference

Change only the `Version` on the `<WebJobsExtension>` item in the extension's `src` project. Keep the file's encoding, BOM, and trailing newline as they are to avoid noise in the diff.

Only change direct `<PackageReference>` versions in the worker project (for example, `Azure.Storage.Blobs` or `Microsoft.Extensions.Azure`) when the update requires it or the user asks. Don't update `samples/` projects; they reference published packages.

## 4. Version the worker packages

Use the `WorkerVersionPublished` column from step 1:

- `True`: bump `VersionPrefix`. Use a patch bump for a reference-only update or bug fixes (Storage 6.8.2 to 6.8.3), and a minor bump when the update adds features, new configuration, or notable dependency changes (SignalR Service 2.0.1 to 2.1.0). Use a major bump only for breaking changes, and confirm with the user first.
- `False`: the current `VersionPrefix` is unreleased, so don't bump it again. Add your entry under its existing release notes section.

Worker versions follow each worker package's own scheme. Don't copy the WebJobs version number (for example, worker Event Hubs 6.4.0 paired with WebJobs 6.5.1, and worker Storage.Queues 5.5.x pairs with WebJobs Storage 5.3.x).

Find dependent worker packages that must be released with the updated package. They need a version bump and release notes too, even though they have no `<WebJobsExtension>` of their own:

```powershell
git grep -n "<UpdatedProjectName>.csproj" -- "extensions/*/src/*.csproj"
```

For example, `extensions/Worker.Extensions.Storage` (the Storage metapackage) references the Blobs and Queues projects. When either changes, bump the metapackage too.

## 5. Update release notes

Each extension has `extensions/<Name>/release_notes.md`. Storage is an exception: `extensions/Worker.Extensions.Storage/release_notes.md` has sections for the metapackage, Blobs, and Queues. After a release, these files are reset to placeholders:

```markdown
### Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs <version>

- <entry>
```

Replace `<version>` with the new worker version and `- <entry>` with entries in the established style:

```markdown
### Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs 6.8.3

- Update `Microsoft.Azure.WebJobs.Extensions.Storage.Blobs` to `5.3.10` (#3516)
  - Fixes ... (Azure/azure-sdk-for-net#62764)
```

For dependent packages, add an entry such as ``- Update `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` to `6.8.3` (#PR)``. Keep existing entries for unreleased changes. Reference the tracking issue, or the PR number once it's known.

## 6. Validate

Run these and report the results:

```powershell
# Builds the extension in CI mode: verifies the generated ExtensionInformationAttribute and restores the
# generated ExtensionValidation project, which fails if the WebJobs version can't be resolved.
dotnet build extensions/<Name>/src/<Name>.csproj -c Release -p:ContinuousIntegrationBuild=true
Select-String -Path extensions/<Name>/src/obj/Release/*/<Name>.AssemblyInfo.cs -Pattern ExtensionInformation

# Extension unit tests (adjust the filter to the extension, e.g. Storage|Blob|Queue, CosmosDB, EventHubs).
dotnet test test/extensions/Worker.Extensions.Tests/Worker.Extensions.Tests.csproj --filter "FullyQualifiedName~<Area>"
```

When dependent packages are involved, run `dotnet pack` on the top-level package into a temporary folder and check that its nuspec depends on the new worker versions. For high-impact fixes, also build a throwaway function app outside the repo that references the packed packages, using a `nuget.config` that lists the local folder plus the repository's feed. Confirm that `bin/<config>/<tfm>/.azurefunctions/` contains the WebJobs assemblies at the target version. Delete temporary folders afterwards.

## 7. Open the pull request

- Keep the PR scoped to the extension update: the project file(s), any dependent package(s), and release notes.
- Follow `.github/pull_request_template.md`, link the tracking issue (`resolves #...`), and check the release notes item.
- In the description, list the old and new WebJobs versions, the worker version bumps, the NuGet latest-version check result, any host-compatibility concerns from step 2, and the exact validation commands with their results.
- Publishing the worker packages happens after merge through the release process, not in this PR.
