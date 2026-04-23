## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk 0.4.0

- Initial SDK preview
- A replacement for `Microsoft.Azure.Functions.Worker.Sdk`
- Now an MSBuild SDK (and not a `PackageReference`)
- Overhauls the extension project generation and restore flow
  - Generated project renamed to `azure_functions.g.csproj`
  - will now make best effort to generate and restore as part of `dotnet restore` phase
- Removes dependencies on generated project
  - `Microsoft.NET.Sdk.Functions` removed
- No longer fully builds the extension project, only restores and directly collects extension files
- Extension trimming behavior has been removed
  - `Microsoft.Azure.Functions.Worker.Sdk` would previously only include extensions which had actual usage (determined by examining built code)
  - New SDK no longer takes this step. All referenced extensions are included, regardless of actual code usage
