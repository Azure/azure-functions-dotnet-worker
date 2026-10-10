// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth
{
    /// <summary>
    /// Defines the authentication methods supported for connecting to Azure Cosmos DB for MongoDB (vCore).
    /// The authentication method is auto-detected based on TenantId:
    /// - If TenantId is specified -> MicrosoftEntraID
    /// - If TenantId is not specified -> NativeAuth
    /// </summary>
    internal enum AuthMethod
    {
        NativeAuth = 0,
        MicrosoftEntraID = 1
    }
}