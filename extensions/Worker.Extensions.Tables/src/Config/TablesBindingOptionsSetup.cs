using System;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.Config
{
    internal class TablesBindingOptionsSetup : IConfigureNamedOptions<TablesBindingOptions>
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
                if (connectionSection.TryGetServiceUriForStorageAccounts(TablesServiceUriSubDomain, out Uri serviceUri))
                {
                    options.ServiceUri = serviceUri;
                }
            }

            options.TableClientOptions = (TableClientOptions)_componentFactory.CreateClientOptions(typeof(TableClientOptions), null, connectionSection);
            options.Credential = _componentFactory.CreateTokenCredential(connectionSection);
        }
    }
}
