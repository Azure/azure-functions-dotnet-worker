// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core;
using Azure.Cosmos;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class CosmosDBConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.Source is not IBindingData bindingData)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            if (bindingData.Source is not "Microsoft.Azure.WebJobs.Extensions.CosmosDB")
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            try
            {
                object result = ToTargetType(context.TargetType, bindingData.Content);

                if (result is not null)
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(result));
                }
            }
            catch (Exception ex)
            {
                // TODO: DeserializeObject could throw
                Console.WriteLine(ex);
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        private object? ToTargetType(Type targetType, BinaryData content) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient)              => CreateCosmosReference<CosmosClient>(content),
            Type _ when targetType == typeof(CosmosDatabase)            => CreateCosmosReference<CosmosDatabase>(content),
            Type _ when targetType == typeof(CosmosContainer)           => CreateCosmosReference<CosmosContainer>(content),
            _ => CreateTargetObject(targetType, content)
        };

        private object CreateTargetObject(Type targetType, BinaryData content)
        {
            MethodInfo deserializeObjectMethod = GetType()
                                                .GetMethod(nameof(DeserializeTargetObject), BindingFlags.Instance | BindingFlags.NonPublic)
                                                .MakeGenericMethod(new Type[] { targetType });
            return deserializeObjectMethod.Invoke(this, new object[] { content });
        }

        private object? DeserializeTargetObject<T>(BinaryData content)
        {
            return content.ToObjectFromJson<T>();
        }

        private object CreateCosmosReference<T>(BinaryData content)
        {
            var cosmosAttribute = content.ToObjectFromJson<CosmosDBInputAttribute>();
            var connectionString = Environment.GetEnvironmentVariable(cosmosAttribute?.Connection);

            Type targetType = typeof(T);
            CosmosClient cosmosClient = new (connectionString);

            object cosmosReference = targetType switch {
                Type _ when targetType == typeof(CosmosDatabase)    => cosmosClient.GetDatabase(cosmosAttribute?.DatabaseName),
                Type _ when targetType == typeof(CosmosContainer)   => cosmosClient.GetContainer(cosmosAttribute?.DatabaseName, cosmosAttribute?.ContainerName),
                _ => cosmosClient
            };

            return (T)cosmosReference;
        }
    }
}
