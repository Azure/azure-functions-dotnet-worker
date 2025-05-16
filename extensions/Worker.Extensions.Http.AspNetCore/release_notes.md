## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore 2.0.2

- Fix intermittent error `IFeatureCollection has been disposed` exception in multiple-output binding scenarios. (#2896)

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Analyzers 1.0.4

- Include case where customer POCO is wrapped in `Task<T>` in the `HttpResultAttribute` analyzer for multiple output binding scenarios. (#3506)
