## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore <version>

- Fix response cookie duplication in ASP.NET Core integration by giving `IHeaderDictionary` indexer setters replace semantics, including removing the header when assigned `StringValues.Empty` (#3353)

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Analyzers  <version>

- <entry>
