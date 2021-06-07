### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- Fixed bugs causing large input payloads to fail.
- Fixed bugs when referencing packages with `IHostedService` causing grpc communication issues.
- Hosting configuration now applies `AZURE_FUNCTIONS_` prefixed environment variables by default.
This is a behavior change that provides the expected behavior when debugging locally or adding the variable to an environment.
