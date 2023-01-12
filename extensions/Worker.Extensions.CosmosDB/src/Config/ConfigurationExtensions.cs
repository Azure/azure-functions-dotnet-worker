// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class ConfigurationExtensions
    {
        public static IConfigurationSection GetCosmosConnectionStringSection(this IConfiguration configuration, string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName))
            {
                connectionStringName = Constants.ExtensionName; // default
            }

            // first try prefixing
            string prefixedConnectionStringName = GetPrefixedConnectionStringName(connectionStringName);
            IConfigurationSection section = configuration.GetConnectionStringOrSetting(prefixedConnectionStringName);

            if (!section.Exists())
            {
                // next try a direct unprefixed lookup
                section = configuration.GetConnectionStringOrSetting(connectionStringName);
            }

            return section;
        }

        public static string GetPrefixedConnectionStringName(string connectionStringName)
        {
            return Constants.ConfigurationSectionName + connectionStringName;
        }

        /// <summary>
        /// Looks for a connection string by first checking the ConfigurationStrings section, and then the root.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionName">The connection string key.</param>
        /// <returns></returns>
        public static IConfigurationSection GetConnectionStringOrSetting(this IConfiguration configuration, string connectionName)
        {
            if (configuration.GetSection(Constants.ConnectionStringsSectionName).Exists())
            {
                IConfigurationSection onConnectionStrings = configuration.GetSection(Constants.ConnectionStringsSectionName).GetSection(connectionName);
                if (onConnectionStrings.Exists())
                {
                    return onConnectionStrings;
                }
            }

            return configuration.GetSection(connectionName);
        }
    }
}