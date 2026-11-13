// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to MongoDB SDK rich types (<see cref="IMongoClient"/>, <see cref="IMongoDatabase"/>,
    /// and <see cref="IMongoCollection{BsonDocument}"/>) for the Azure Cosmos DB for MongoDB (vCore) extension.
    /// Only the advertised target types use deferred binding; all other parameter types continue to be
    /// bound via the host-provided serialized value.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(IMongoClient))]
    [SupportedTargetType(typeof(IMongoDatabase))]
    [SupportedTargetType(typeof(IMongoCollection<BsonDocument>))]
    internal class CosmosDBMongoConverter : IInputConverter
    {
        private readonly IOptionsMonitor<MongoBindingOptions> _mongoOptions;

        public CosmosDBMongoConverter(IOptionsMonitor<MongoBindingOptions> mongoOptions)
        {
            _mongoOptions = mongoOptions ?? throw new ArgumentNullException(nameof(mongoOptions));
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => new ValueTask<ConversionResult>(ConvertFromBindingData(context, binding)),
                _ => new ValueTask<ConversionResult>(ConversionResult.Unhandled()),
            };
        }

        private ConversionResult ConvertFromBindingData(ConverterContext context, ModelBindingData modelBindingData)
        {
            try
            {
                if (modelBindingData.Source is not Constants.MongoExtensionName)
                {
                    throw new InvalidBindingSourceException(modelBindingData.Source, Constants.MongoExtensionName);
                }

                CosmosDBMongoInputAttribute attribute = GetBindingDataContent(modelBindingData);
                object? result = ToTargetType(context.TargetType, attribute);

                if (result is null)
                {
                    return ConversionResult.Unhandled();
                }

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private static CosmosDBMongoInputAttribute GetBindingDataContent(ModelBindingData bindingData)
        {
            return bindingData.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<CosmosDBMongoInputAttribute>()
                    ?? throw new InvalidOperationException("Unable to deserialize the Mongo binding data content."),
                _ => throw new InvalidContentTypeException(bindingData.ContentType, Constants.JsonContentType),
            };
        }

        private object? ToTargetType(Type targetType, CosmosDBMongoInputAttribute attribute)
        {
            if (targetType == typeof(IMongoClient))
            {
                return CreateClient(attribute);
            }

            if (targetType == typeof(IMongoDatabase))
            {
                return CreateClient(attribute).GetDatabase(attribute.DatabaseName);
            }

            if (targetType == typeof(IMongoCollection<BsonDocument>))
            {
                return CreateClient(attribute)
                    .GetDatabase(attribute.DatabaseName)
                    .GetCollection<BsonDocument>(attribute.CollectionName);
            }

            return null;
        }

        private IMongoClient CreateClient(CosmosDBMongoInputAttribute attribute)
        {
            string connectionName = string.IsNullOrEmpty(attribute.ConnectionStringSetting)
                ? Constants.DefaultConnectionStringKey
                : attribute.ConnectionStringSetting!;

            MongoBindingOptions options = _mongoOptions.Get(connectionName);
            return options.GetClient(attribute.TenantId, attribute.ManagedIdentityClientId);
        }
    }
}