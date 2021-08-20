### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- Added support for binding of `Guid` and `Guid?` type parameters.
- Changed the order configuration sources are registered (This is a potentially breaking change)
  - Command line arguments now take precedence over environment variables by default