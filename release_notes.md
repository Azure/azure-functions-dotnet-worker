### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- Fixed bugs causing large input payloads to fail.
- Fixed a bug causing a crash when attempting to log before the gRPC communication to Functions Host is set up.
- Hosting configuration now applies `AZURE_FUNCTIONS_` prefixed environment variables by default.
This is a behavior change that provides the expected behavior when debugging locally or adding the variable to an environment.
