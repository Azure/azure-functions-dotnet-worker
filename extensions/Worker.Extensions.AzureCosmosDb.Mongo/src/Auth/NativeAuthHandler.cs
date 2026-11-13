// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Driver;

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth
{
    internal sealed class NativeAuthHandler : IAuthHandler
    {
        public MongoClientSettings ConfigureAuth(string connectionString)
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ApplicationName = Constants.ApplicationName;
            return settings;
        }
    }
}