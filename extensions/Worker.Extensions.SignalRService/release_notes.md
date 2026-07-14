## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.SignalRService <version>

- Added serverless authentication refresh support. Use the new `[SignalRRefreshInput]` binding to refresh a live SignalR client connection's authentication (and application claims) without reconnecting; it binds a `SignalRConnectionInfo` carrying the refreshed access token and `TokenLifetimeSeconds`. .NET isolated hubs can also call `ServerlessHub.RefreshConnectionAuthenticationAsync`/`GetConnectionClaimsAsync` directly.
- `SignalRConnectionInfo` now includes `TokenLifetimeSeconds` so a refresh-aware client can schedule its refresh before the token expires.
