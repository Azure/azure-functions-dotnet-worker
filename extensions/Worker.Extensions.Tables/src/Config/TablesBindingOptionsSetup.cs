using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.Config
{
    internal class TablesBindingOptionsSetup: IConfigureNamedOptions<TablesBindingOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _componentFactory;

        private const string TablesServiceUriSubDomain = "table";

        public TablesBindingOptionsSetup(IConfiguration configuration, AzureComponentFactory componentFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
        }

        public void Configure(TablesBindingOptions options)
        {
            Configure(Options.DefaultName, options);
        }

        public void Configure(string name, TablesBindingOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Constants.Storage; // default
            }

            IConfigurationSection connectionSection = _configuration.GetWebJobsConnectionStringSection(name);

            if (!connectionSection.Exists())
            {
                // Not found
                throw new InvalidOperationException($"Tables connection configuration '{name}' does not exist. " +
                                                    "Make sure that it is a defined App Setting.");
            }

            if (!string.IsNullOrWhiteSpace(connectionSection.Value))
            {
                options.ConnectionString = connectionSection.Value;
            }
            else
            {
                if (TryGetServiceUri(connectionSection, out Uri serviceUri))
                {
                    options.ServiceUri = serviceUri;
                }
            }

            options.TableClientOptions = (TableClientOptions)_componentFactory.CreateClientOptions(typeof(TableClientOptions), null, connectionSection);
            options.Credential = _componentFactory.CreateTokenCredential(connectionSection);
        }

        /// <summary>
        /// Either constructs the serviceUri from the provided accountName
        /// or retrieves the serviceUri for the specific resource (i.e. blobServiceUri or queueServiceUri)
        /// </summary>
        private bool TryGetServiceUri(IConfiguration configuration, out Uri serviceUri)
        {
            var serviceUriConfig = string.Format(CultureInfo.InvariantCulture, "{0}ServiceUri", TablesServiceUriSubDomain);

            string accountName;
            string uriStr;
            if ((accountName = configuration.GetValue<string>("accountName")) is not null)
            {
                serviceUri = FormatServiceUri(accountName);
                return true;
            }
            else if ((uriStr = configuration.GetValue<string>(serviceUriConfig)) is not null)
            {
                serviceUri = new Uri(uriStr);
                return true;
            }

            serviceUri = default(Uri)!;
            return false;
        }

        /// <summary>
        /// Generates the serviceUri for a particular storage resource
        /// </summary>
        private Uri FormatServiceUri(string accountName, string defaultProtocol = "https", string endpointSuffix = "core.windows.net")
        {
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}://{1}.{2}.{3}", defaultProtocol, accountName, TablesServiceUriSubDomain, endpointSuffix);
            return new Uri(uri);
        }
    }
}
