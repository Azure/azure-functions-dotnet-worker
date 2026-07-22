## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore <next>

- Register `HttpContext.RequestAborted` on the function completion task so the ASP.NET Core pipeline releases the HTTP connection immediately on client disconnect instead of waiting for function completion or timeout (#3472)

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore 2.1.1

- Fix response cookie duplication in ASP.NET Core integration by giving `IHeaderDictionary` indexer setters replace semantics, including removing the header when assigned `StringValues.Empty` (#3353)
- Pass cancellation tokens (RequestAborted / CancellationToken) through to WaitAsync calls so cancelled HTTP
requests propagate promptly instead of waiting for timeout (#3415)
- Fix intermittent `InvalidOperationException` due to race condition between `SetResult` call on TCS and HTTP request cancellation/timeout (#3415)
