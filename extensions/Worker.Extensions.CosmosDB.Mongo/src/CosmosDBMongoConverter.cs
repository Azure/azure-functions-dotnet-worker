// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB.Mongo;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind CosmosDB type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    internal class CosmosDBMongoConverter : IInputConverter
    {
        private readonly IOptionsMonitor<CosmosDBBindingOptions> _cosmosOptions;
        private readonly ILogger<CosmosDBMongoConverter> _logger;
        private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

        public CosmosDBMongoConverter(IOptionsMonitor<CosmosDBBindingOptions> cosmosOptions, ILogger<CosmosDBMongoConverter> logger)
        {
            _cosmosOptions = cosmosOptions ?? throw new ArgumentNullException(nameof(cosmosOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return context?.Source switch
            {
                ModelBindingData binding => await ConvertFromBindingDataAsync(context, binding),
                _ => throw new ArgumentOutOfRangeException(nameof(context.Source), context.Source, "Invalid source type.")
            };
        }

        private async ValueTask<ConversionResult> ConvertFromBindingDataAsync(ConverterContext context, ModelBindingData modelBindingData)
        {
            try
            {
                if (modelBindingData.Source is not Constants.CosmosExtensionName)
                {
                    throw new InvalidBindingSourceException(modelBindingData.Source, Constants.CosmosExtensionName);
                }

                var cosmosAttribute = GetBindingDataContent(modelBindingData);
                object result = await ToTargetTypeAsync(context.TargetType, cosmosAttribute);

                return ConversionResult.Success(result);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }

        private CosmosDBMongoInputAttribute GetBindingDataContent(ModelBindingData bindingData)
        {
            if (bindingData is null)
            {
                throw new ArgumentNullException(nameof(bindingData));
            }

            return bindingData.ContentType switch
            {
                Constants.JsonContentType => bindingData.Content.ToObjectFromJson<CosmosDBMongoInputAttribute>(),
                _ => throw new InvalidContentTypeException(bindingData.ContentType, Constants.JsonContentType)
            };
        }

        private async Task<object> ToTargetTypeAsync(Type targetType, CosmosDBMongoInputAttribute cosmosAttribute)
        {
            // TODO: Implement
            throw new NotImplementedException();
        }
    }
}
