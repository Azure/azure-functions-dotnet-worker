## Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

- Support sdk-type binding reference type (#1107)
- Add MS Build property to disable source generation of function metadata (it is enabled automatically) (#1200)
  - Prop name: `FunctionsMetadataSourceGen_Enabled`
- Avoid registering metadata loader extension when running in the native worker (#1216)