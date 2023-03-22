### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

- Logging an error on a function invocation exception. This error will also be logged by the host, but this allows for richer telemetry coming directly from the worker. See #1421 for details on how to disable this log if desired.
- Add query as property to HttpRequestData (#1408)