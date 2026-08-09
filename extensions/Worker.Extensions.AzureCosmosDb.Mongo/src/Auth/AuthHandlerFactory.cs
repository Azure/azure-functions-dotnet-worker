// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
#if NET8_0_OR_GREATER
using Azure.Core;
#endif

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth
{
    internal static class AuthHandlerFactory
    {
        /// <summary>
        /// Auto-detects authentication method based on TenantId presence.
        /// If TenantId is specified, uses Microsoft Entra ID authentication.
        /// Otherwise, uses native MongoDB authentication.
        /// </summary>
        public static IAuthHandler Create(
            string? tenantId = null,
            string? managedIdentityClientId = null)
        {
            AuthMethod authMethod = string.IsNullOrEmpty(tenantId)
                ? AuthMethod.NativeAuth
                : AuthMethod.MicrosoftEntraID;

            return Create(authMethod, tenantId, managedIdentityClientId);
        }

        internal static IAuthHandler Create(
            AuthMethod authMethod,
            string? tenantId,
            string? managedIdentityClientId)
        {
            switch (authMethod)
            {
                case AuthMethod.MicrosoftEntraID:
#if NET8_0_OR_GREATER
                    if (string.IsNullOrEmpty(tenantId))
                    {
                        throw new InvalidOperationException(
                            "TenantId is required for Microsoft Entra ID authentication. " +
                            "Please specify the TenantId property.");
                    }
                    return new EntraIdAuthHandler(tenantId, managedIdentityClientId);
#else
                    throw new PlatformNotSupportedException(
                        "Microsoft Entra ID authentication is only supported on .NET 8.0 or later. " +
                        "Please target .NET 8.0 or remove the TenantId property to use native authentication.");
#endif

                case AuthMethod.NativeAuth:
                default:
                    return new NativeAuthHandler();
            }
        }

#if NET8_0_OR_GREATER
        internal static IAuthHandler CreateEntraId(TokenCredential credential, string? tenantId)
        {
            return new EntraIdAuthHandler(credential, tenantId);
        }
#endif
    }
}