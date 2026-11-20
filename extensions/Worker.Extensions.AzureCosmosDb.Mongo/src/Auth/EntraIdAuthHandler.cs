// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET8_0_OR_GREATER
using Azure.Core;
using MongoDB.Driver;

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth
{
    /// <summary>
    /// Handler for Microsoft Entra ID authentication.
    /// Supports System-assigned MI, User-assigned MI, and custom TokenCredential.
    /// </summary>
    internal sealed class EntraIdAuthHandler : IAuthHandler
    {
        private readonly string? _tenantId;
        private readonly string? _managedIdentityClientId;
        private readonly TokenCredential? _customCredential;

        public EntraIdAuthHandler(
            string? tenantId = null,
            string? managedIdentityClientId = null)
        {
            _tenantId = tenantId;
            _managedIdentityClientId = managedIdentityClientId;
            _customCredential = null;
        }

        public EntraIdAuthHandler(TokenCredential credential, string? tenantId = null)
        {
            _customCredential = credential ?? throw new System.ArgumentNullException(nameof(credential));
            _tenantId = tenantId;
            _managedIdentityClientId = null;
        }

        public MongoClientSettings ConfigureAuth(string connectionString)
        {
            var parser = new ConnectionStringParser(connectionString);
            var preparedConnectionString = parser.PrepareForEntraIdAuth();
            var settings = MongoClientSettings.FromConnectionString(preparedConnectionString);

            EntraIdOidcCallback oidcCallback;

            if (_customCredential != null)
            {
                oidcCallback = new EntraIdOidcCallback(_customCredential, _tenantId);
            }
            else
            {
                oidcCallback = new EntraIdOidcCallback(_tenantId, _managedIdentityClientId);
            }

            settings.Credential = MongoCredential.CreateOidcCredential(oidcCallback);

            settings.UseTls = true;
            settings.RetryWrites = false;
            settings.MaxConnectionIdleTime = System.TimeSpan.FromMinutes(2);
            settings.ApplicationName = Constants.ApplicationName;

            return settings;
        }
    }
}
#endif