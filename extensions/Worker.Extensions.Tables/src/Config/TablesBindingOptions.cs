// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Core;
using Azure.Data.Tables;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.Config
{
    internal class TablesBindingOptions
    {
        public string? ConnectionString { get; set; }

        public Uri? ServiceUri { get; set; }

        public TokenCredential? Credential { get; set; }

        public TableClientOptions? TableClientOptions { get; set; }

        private TableServiceClient? TableServiceClient;

        internal virtual TableServiceClient CreateClient()
        {
            if (ConnectionString is null && ServiceUri is null)
            {
                throw new ArgumentNullException(nameof(ConnectionString) + " " + nameof(ServiceUri));
            }

            if (TableServiceClient is not null)
            {
                return TableServiceClient;
            }

            TableServiceClient = !string.IsNullOrEmpty(ConnectionString)
                ? new TableServiceClient(ConnectionString, TableClientOptions) // Connection string based auth;
                : new TableServiceClient(ServiceUri, Credential, TableClientOptions); // AAD auth

            return TableServiceClient;
        }
    }
}
