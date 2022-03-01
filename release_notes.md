### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- Added support for .NET Standard 2.0. This enables Workers built using .NET Framework
- Storage extensions moved to 5.0.0
- SignalR extensions:
    * Support AAD authentication in connection string. (#803)
    * Add some convenient POJOs. (#774)
    * Support multiple Azure SignalR Service instances. (#566)
    * Fix a bug that group name is required when group action is "RemoveAll". (#347)