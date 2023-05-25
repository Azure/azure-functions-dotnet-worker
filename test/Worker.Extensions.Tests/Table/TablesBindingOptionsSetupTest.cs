using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.Config;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Table
{
    public class TablesBindingOptionsSetupTest
    {
        private Mock<IConfiguration> configuration;
        private Mock<AzureComponentFactory> componentFactory;
        public TablesBindingOptionsSetupTest()
        {
            configuration = new Mock<IConfiguration>();
            componentFactory = new Mock<AzureComponentFactory>();
        }

        [Fact]
        public void Configure_With_Default_Name()
        {
            var configSection = new Mock<IConfigurationSection>();

            configSection.Setup(c => c.Key).Returns("key");
            configSection.Setup(c => c.Value).Returns("connectionString");
            configuration.Setup(c => c.GetSection("AzureWebJobsStorage")).Returns(configSection.Object);

            var tableBindingOptions = new Mock<TablesBindingOptions>();
            var tokenCredential = new Mock<TokenCredential>();

            componentFactory.Setup(c => c.CreateClientOptions(typeof(TableClientOptions), null, configSection.Object)).Returns(null);
            componentFactory.Setup(c => c.CreateTokenCredential(configSection.Object)).Returns(tokenCredential.Object);

            var tablesBindingOptionsSetup = new Mock<TablesBindingOptionsSetup>(configuration.Object, componentFactory.Object);
            tablesBindingOptionsSetup.Object.Configure(tableBindingOptions.Object);

            Assert.Equal("connectionString", tableBindingOptions.Object.ConnectionString);
            Assert.NotNull(tableBindingOptions.Object.Credential);
        }

        [Fact]
        public void Configure_With_Given_Name()
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(c => c.Key).Returns("key");
            configSection.Setup(c => c.Value).Returns("connectionString");
            configuration.Setup(c => c.GetSection("AzureWebJobsCustom")).Returns(configSection.Object);

            var tableBindingOptions = new Mock<TablesBindingOptions>();
            var tokenCredential = new Mock<TokenCredential>();

            componentFactory.Setup(c => c.CreateClientOptions(typeof(TableClientOptions), null, configSection.Object)).Returns(null);
            componentFactory.Setup(c => c.CreateTokenCredential(configSection.Object)).Returns(tokenCredential.Object);

            var tablesBindingOptionsSetup = new Mock<TablesBindingOptionsSetup>(configuration.Object, componentFactory.Object);
            tablesBindingOptionsSetup.Object.Configure("Custom",tableBindingOptions.Object);

            Assert.Equal("connectionString", tableBindingOptions.Object.ConnectionString);
            Assert.NotNull(tableBindingOptions.Object.Credential);
        }

        [Fact]
        public void Configure_With_ServiceUri()
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(c => c.Key).Returns("key");
            configSection.Setup(c => c.Value).Returns("");

            var inMemorySettings = new Dictionary<string, string> {
                {"AzureWebJobsStorage:accountName", "test"},
            };

            IConfiguration configurationReal = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var tableBindingOptions = new Mock<TablesBindingOptions>();
            var tokenCredential = new Mock<TokenCredential>();
            componentFactory.Setup(c => c.CreateClientOptions(typeof(TableClientOptions), null, configSection.Object)).Returns(null);
            componentFactory.Setup(c => c.CreateTokenCredential(configSection.Object)).Returns(tokenCredential.Object);

            var tablesBindingOptionsSetup = new Mock<TablesBindingOptionsSetup>(configurationReal, componentFactory.Object);
            tablesBindingOptionsSetup.Object.Configure(tableBindingOptions.Object);

            Assert.Null(tableBindingOptions.Object.ConnectionString);
            Assert.Equal("https://test.table.core.windows.net/", tableBindingOptions.Object.ServiceUri.ToString());
        }
    }
}
