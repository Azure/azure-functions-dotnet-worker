// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET8_0_OR_GREATER
using Azure.Core;
using Azure.Identity;
using MongoDB.Driver.Authentication.Oidc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth
{
    internal sealed class EntraIdOidcCallback : IOidcCallback
    {
        private readonly TokenCredential _credential;
        private readonly string? _tenantId;
        private static readonly string[] Scopes = new[] { "https://ossrdbms-aad.database.windows.net/.default" };

        public EntraIdOidcCallback(string? tenantId = null, string? managedIdentityClientId = null)
        {
            var options = new DefaultAzureCredentialOptions();

            if (!string.IsNullOrEmpty(tenantId))
            {
                options.TenantId = tenantId;
            }

            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ManagedIdentityClientId = managedIdentityClientId;
            }

            _credential = new DefaultAzureCredential(options);
            _tenantId = tenantId;
        }

        public EntraIdOidcCallback(TokenCredential credential, string? tenantId = null)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _tenantId = tenantId;
        }

        public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var tokenRequestContext = new TokenRequestContext(Scopes, tenantId: _tenantId);
            var accessToken = _credential.GetToken(tokenRequestContext, cancellationToken);

            return new OidcAccessToken(accessToken.Token, accessToken.ExpiresOn - DateTimeOffset.UtcNow);
        }

        public async Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var tokenRequestContext = new TokenRequestContext(Scopes, tenantId: _tenantId);
            var accessToken = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken)
                .ConfigureAwait(false);

            return new OidcAccessToken(accessToken.Token, accessToken.ExpiresOn - DateTimeOffset.UtcNow);
        }
    }
}
#endif