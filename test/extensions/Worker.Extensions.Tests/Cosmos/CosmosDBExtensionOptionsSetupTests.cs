// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Cosmos
{
    public class CosmosDBExtensionOptionsSetupTests
    {
        [Fact]
        public void ConfigureCosmosDBExtension_RegistersExpectedServices()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            FunctionsWorkerApplicationBuilder builder = new(services);

            // Act
            builder.ConfigureCosmosDBExtension();

            // Assert
            ServiceProvider provider = services.BuildServiceProvider();

            // CosmosDBExtensionOptions should have PostConfigure registered
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetService<IOptions<CosmosDBExtensionOptions>>();
            Assert.NotNull(extensionOptions);
            Assert.NotNull(extensionOptions.Value);
        }

        [Fact]
        public void ConfigureCosmosDBExtension_NullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IFunctionsWorkerApplicationBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>("builder", () => builder.ConfigureCosmosDBExtension());
        }

        [Fact]
        public void ConfigureCosmosDBExtension_WithConfigure_AppliesOptions()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            FunctionsWorkerApplicationBuilder builder = new(services);

            JsonObjectSerializer customSerializer = new();

            // Act
            builder.ConfigureCosmosDBExtension(options =>
            {
                options.Configure(o => o.Serializer = customSerializer);
            });

            // Assert
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();
            Assert.Same(customSerializer, extensionOptions.Value.Serializer);
        }

        [Fact]
        public void ConfigureCosmosDBExtension_NullConfigure_DoesNotThrow()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            FunctionsWorkerApplicationBuilder builder = new(services);

            // Act & Assert (should not throw)
            builder.ConfigureCosmosDBExtension(configure: null);
        }

        [Fact]
        public void ConfigureCosmosDBExtensionOptions_AppliesAction()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();

            JsonObjectSerializer customSerializer = new();

            // Act
            builder.ConfigureCosmosDBExtensionOptions(o =>
            {
                o.Serializer = customSerializer;
            });

            // Assert
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();
            Assert.Same(customSerializer, extensionOptions.Value.Serializer);
        }

        [Fact]
        public void ConfigureCosmosDBExtensionOptions_NullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IFunctionsWorkerApplicationBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>("builder", () =>
                builder.ConfigureCosmosDBExtensionOptions(o => { }));
        }

        [Fact]
        public void PostConfigure_SetsCosmosSerializer_WhenSerializerIsSet()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            JsonObjectSerializer customSerializer = new();
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();
            builder.ConfigureCosmosDBExtensionOptions(o => o.Serializer = customSerializer);

            // Act
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();

            // Assert - PostConfigure should have set ClientOptions.Serializer
            Assert.NotNull(extensionOptions.Value.ClientOptions.Serializer);
            Assert.IsType<WorkerCosmosSerializer>(extensionOptions.Value.ClientOptions.Serializer);
        }

        [Fact]
        public void PostConfigure_SetsCosmosSerializer_FromWorkerOptionsSerializer()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            JsonObjectSerializer workerSerializer = new();
            services.AddOptions<WorkerOptions>()
                .Configure(o => o.Serializer = workerSerializer);
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();

            // Act
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();

            // Assert - PostConfigure should use WorkerOptions.Serializer as fallback
            Assert.NotNull(extensionOptions.Value.ClientOptions.Serializer);
            Assert.IsType<WorkerCosmosSerializer>(extensionOptions.Value.ClientOptions.Serializer);
        }

        [Fact]
        public void PostConfigure_DoesNotOverrideExistingCosmosSerializer()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            WorkerCosmosSerializer existingCosmosSerializer = new(new JsonObjectSerializer());
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();
            builder.ConfigureCosmosDBExtensionOptions(o =>
            {
                o.ClientOptions.Serializer = existingCosmosSerializer;
                o.Serializer = new JsonObjectSerializer();
            });

            // Act
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();

            // Assert - PostConfigure should NOT override existing ClientOptions.Serializer
            Assert.Same(existingCosmosSerializer, extensionOptions.Value.ClientOptions.Serializer);
        }

        [Fact]
        public void PostConfigure_NoSerializerSet_DoesNotSetCosmosSerializer()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();

            // Act
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();

            // Assert - No serializer on extension options or worker options, so ClientOptions.Serializer stays null
            Assert.Null(extensionOptions.Value.ClientOptions.Serializer);
        }

        [Fact]
        public void UseWorkerSerializer_SetsSerializerFromWorkerOptions()
        {
            // Arrange
            ServiceCollection services = CreateServices();
            JsonObjectSerializer workerSerializer = new();
            services.AddOptions<WorkerOptions>()
                .Configure(o => o.Serializer = workerSerializer);
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension(options =>
            {
                options.UseWorkerSerializer();
            });

            // Act
            ServiceProvider provider = services.BuildServiceProvider();
            IOptions<CosmosDBExtensionOptions> extensionOptions = provider.GetRequiredService<IOptions<CosmosDBExtensionOptions>>();

            // Assert
            Assert.Same(workerSerializer, extensionOptions.Value.Serializer);
        }

        [Fact]
        public void UseWorkerSerializer_NullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            OptionsBuilder<CosmosDBExtensionOptions> builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>("builder", () => builder.UseWorkerSerializer());
        }

        [Fact]
        public void DefaultExtensionOptions_HasGatewayConnectionMode()
        {
            // Arrange & Act
            CosmosDBExtensionOptions options = new();

            // Assert
            Assert.Equal(ConnectionMode.Gateway, options.ClientOptions.ConnectionMode);
        }

        [Fact]
        public void DefaultExtensionOptions_SerializerIsNull()
        {
            // Arrange & Act
            CosmosDBExtensionOptions options = new();

            // Assert
            Assert.Null(options.Serializer);
        }

        [Fact]
        public void BindingOptions_Serializer_UsesExtensionOptionsSerializer()
        {
            // Arrange
            JsonObjectSerializer customSerializer = new();
            CosmosDBExtensionOptions extensionOptions = new()
            {
                Serializer = customSerializer
            };

            CosmosDBBindingOptions bindingOptions = new()
            {
                CosmosExtensionOptions = extensionOptions
            };

            // Act
            ObjectSerializer serializer = bindingOptions.Serializer;

            // Assert
            Assert.Same(customSerializer, serializer);
        }

        [Fact]
        public void BindingOptions_Serializer_FallsBackToDefault_WhenExtensionSerializerIsNull()
        {
            // Arrange
            CosmosDBExtensionOptions extensionOptions = new(); // Serializer is null
            CosmosDBBindingOptions bindingOptions = new()
            {
                CosmosExtensionOptions = extensionOptions
            };

            // Act
            ObjectSerializer serializer = bindingOptions.Serializer;

            // Assert - should fall back to internal default (JsonObjectSerializer with case-insensitive)
            Assert.NotNull(serializer);
            Assert.IsType<JsonObjectSerializer>(serializer);
        }

        [Fact]
        public void BindingOptions_Serializer_FallsBackToDefault_WhenExtensionOptionsIsNull()
        {
            // Arrange
            CosmosDBBindingOptions bindingOptions = new(); // CosmosExtensionOptions is null

            // Act
            ObjectSerializer serializer = bindingOptions.Serializer;

            // Assert
            Assert.NotNull(serializer);
            Assert.IsType<JsonObjectSerializer>(serializer);
        }

        [Fact]
        public void BindingOptionsSetup_SetsCosmosExtensionOptions()
        {
            // Arrange
            JsonObjectSerializer customSerializer = new();
            ServiceCollection services = CreateServices("CosmosConnection", "AccountEndpoint=https://test.documents.azure.test:443/;AccountKey=FAKE==;");
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();
            builder.ConfigureCosmosDBExtensionOptions(o => o.Serializer = customSerializer);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act - resolve named binding options (triggers CosmosDBBindingOptionsSetup)
            IOptionsFactory<CosmosDBBindingOptions> bindingOptionsFactory = provider.GetRequiredService<IOptionsFactory<CosmosDBBindingOptions>>();
            CosmosDBBindingOptions bindingOptions = bindingOptionsFactory.Create("CosmosConnection");

            // Assert - CosmosExtensionOptions should be wired and serializer should flow through
            Assert.NotNull(bindingOptions.CosmosExtensionOptions);
            Assert.Same(customSerializer, bindingOptions.Serializer);
        }

        [Fact]
        public void BindingOptionsSetup_UseWorkerSerializer_FlowsToBindingOptions()
        {
            // Arrange
            JsonObjectSerializer workerSerializer = new();
            ServiceCollection services = CreateServices("CosmosConnection", "AccountEndpoint=https://test.documents.azure.test:443/;AccountKey=FAKE==;");
            services.AddOptions<WorkerOptions>().Configure(o => o.Serializer = workerSerializer);
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension(options =>
            {
                options.UseWorkerSerializer();
            });

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IOptionsFactory<CosmosDBBindingOptions> bindingOptionsFactory = provider.GetRequiredService<IOptionsFactory<CosmosDBBindingOptions>>();
            CosmosDBBindingOptions bindingOptions = bindingOptionsFactory.Create("CosmosConnection");

            // Assert
            Assert.NotNull(bindingOptions.CosmosExtensionOptions);
            Assert.Same(workerSerializer, bindingOptions.Serializer);
        }

        [Fact]
        public void BindingOptionsSetup_NoSerializer_UsesDefaultSerializer()
        {
            // Arrange
            ServiceCollection services = CreateServices("CosmosConnection", "AccountEndpoint=https://test.documents.azure.test:443/;AccountKey=FAKE==;");
            FunctionsWorkerApplicationBuilder builder = new(services);
            builder.ConfigureCosmosDBExtension();

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IOptionsFactory<CosmosDBBindingOptions> bindingOptionsFactory = provider.GetRequiredService<IOptionsFactory<CosmosDBBindingOptions>>();
            CosmosDBBindingOptions bindingOptions = bindingOptionsFactory.Create("CosmosConnection");

            // Assert - no serializer set anywhere, so binding options falls back to default
            Assert.NotNull(bindingOptions.Serializer);
            Assert.IsType<JsonObjectSerializer>(bindingOptions.Serializer);
            Assert.Null(bindingOptions.CosmosExtensionOptions.Serializer);
        }

        private static ServiceCollection CreateServices()
        {
            return CreateServices(connectionName: null, connectionValue: null);
        }

        private static ServiceCollection CreateServices(string connectionName, string connectionValue)
        {
            Dictionary<string, string> configData = new();
            if (connectionName is not null)
            {
                configData[$"ConnectionStrings:{connectionName}"] = connectionValue;
            }

            ServiceCollection services = new();
            services.AddOptions<WorkerOptions>();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build());
            return services;
        }

    }
}
