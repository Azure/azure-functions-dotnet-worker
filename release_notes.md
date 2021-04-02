### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- The environment variables configuration provider is now registered by default
  - **Warning**: this is a potentially breaking change, but given this is the desirable behavior for
  local and hosted scenarios, the default registration call has been updated. The `ConfigureFunctionsWorker` remains unaltered and provides the ability to opt-out of this behavior.
- API to enable inline middleware registration
- Added `TimerInfo` definition allowing for binding information with a built-in type
- `WriteBytesAsync` API for `HttpResponseData`
- Updates/enhancements to tooling JSON output
- `IFunctionActivator` and supporting APIs revised and made public
  - Enables custom handling of function class instantiation
- Fixed issue with proxy medatada handling
- Other fixes and enhancements