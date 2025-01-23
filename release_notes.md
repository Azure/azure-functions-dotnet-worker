### Release notes

<!-- Please add your release notes in the following format:
- My change description (#PR)
-->
- Update Java Worker Version to [2.18.0](https://github.com/Azure/azure-functions-java-worker/releases/tag/2.18.0)

- Allow for an output binding value of an invocation result to be null (#10698)
- Updated dotnet-isolated worker to 1.0.12.
  - [Corrected the path for the prelaunch app location.](https://github.com/Azure/azure-functions-dotnet-worker/pull/2897)
  - [Added net9 prelaunch app.](https://github.com/Azure/azure-functions-dotnet-worker/pull/2898)
- Update the `DefaultHttpProxyService` to better handle client disconnect scenarios (#10688)
  - Replaced `InvalidOperationException` with `HttpForwardingException` when there is a ForwarderError
- Updated `WebJobs.Script` to target .NET 8 (instead of .NET Standard 2.1)
