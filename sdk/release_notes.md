## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.16.1 (meta package)

- Update Microsoft.Azure.Functions.Worker.Sdk.Generators dependency to 1.1.3

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.1.3

- Fixed casing bug in source-generation. Binding types were changed from pascal case to camel case to match legacy generation (#2022)
- Reverted: Default to optimized function executor (#2012)
- Updated source generated versions of IFunctionExecutor to use `global::` namespace prefix to avoid build errors for function class with the same name as its containing namespace. (#1993)
- Updated source generated versions of IFunctionExecutor to include XML documentation for all public types and members
- Updated source generated versions of IFunctionMedatadaProvider to include XML documentation for all public types and members
- Updated source generated versions of IFunctionExecutor to use case-sensitive comparison to fix incorrect invocation of functions with method names only differ in casing. (#2003)
