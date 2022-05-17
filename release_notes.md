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
- Bug fix: Handle = character in cookie value (#838)
- Error handling for in-proc SDK usage inside isolated app. (#824)
- Update ServiceBus, EventGrid, and EventHubs extensions to latest. (#875)
- APIs for reading and updating input binding data, output binding data and invocation result.(#814)
- Startup hook for extensions. (#830)
- Throw build error when in-proc SDK is referenced in isolated function apps. (#841)
- Use better approach for pre/post-build MSBuild targets. Thanks, @f1x3d (#842)
- Exposing FunctionInputConverterException and FunctionWorkerException. (#843)
- Adding DateTimeConverter which will handle binding of DateTime/DateTimeOffset type parameters. (#852)
- Adding the Visual Studio Design time targets to the SDK. (#860)
- Adding UseWhen extension methods for middleware registration. (#865)
