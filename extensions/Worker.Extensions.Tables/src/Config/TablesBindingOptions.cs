using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.Config
{
    internal class TablesBindingOptions
    {
        public string? ConnectionString { get; set; }

        public Uri? ServiceUri { get; set; }

        public TokenCredential? Credential { get; set; }

        public TableClientOptions? TableClientOptions { get; set; }

        internal TableServiceClient CreateClient()
        {
            return string.IsNullOrEmpty(ConnectionString)
                    ? new TableServiceClient(ServiceUri, Credential, TableClientOptions) // AAD auth
                    : new TableServiceClient(ConnectionString, TableClientOptions); // Connection string based auth
        }
    }
}
