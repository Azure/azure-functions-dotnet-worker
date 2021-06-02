### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- Added ZipDeploy tasks to the SDK for publishing to Azure
- Fixed bug when publishing a functions project using `--no-build`
- Hosting configuration now applies `AZURE_FUNCTIONS_` prefixed environment variables by default.
This is a behavior change that provides the expected behavior when debugging locally or adding the variable to an environment.
