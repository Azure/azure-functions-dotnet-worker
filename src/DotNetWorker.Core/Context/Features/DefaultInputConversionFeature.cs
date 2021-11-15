// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// Default implementation of <see cref="IInputConversionFeature"/>
    /// </summary>
    internal sealed class DefaultInputConversionFeature : IInputConversionFeature
    {
        private readonly IInputConverterProvider _inputConverterProvider;
        private static readonly Type _inputConverterAttributeType = typeof(InputConverterAttribute);

        // Users may create a POCO and specify a special converter implementation
        // to be used using "InputConverter" attribute. We cache that mapping here.
        // Key is assembly qualified name of POCO and Value is assembly qualified name of converter implementation.
        private static readonly ConcurrentDictionary<string, string?> _typeToConverterCache = new();

        public DefaultInputConversionFeature(IInputConverterProvider inputConverterProvider)
        {
            _inputConverterProvider = inputConverterProvider ?? throw new ArgumentNullException(nameof(inputConverterProvider));
        }

        /// <summary>
        /// Executes a conversion operation with the context information provided.
        /// </summary>
        /// <param name="converterContext">The converter context.</param>
        /// <returns>An instance of <see cref="ConversionResult"/> representing the result of the conversion.</returns>
        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext converterContext)
        {
            // Check a converter is explicitly specified via the converter context. If so, use that.
            IInputConverter? converterFromContext = GetConverterFromContext(converterContext);

            if (converterFromContext is not null)
            {
                var conversionResult = await ConvertAsyncUsingConverter(converterFromContext, converterContext);

                if (conversionResult.Status != ConversionStatus.Unhandled)
                {
                    return conversionResult;
                }
            }

            // Use the registered converters. The first converter which can handle the conversion wins.
            foreach (var converter in _inputConverterProvider.RegisteredInputConverters)
            {
                var conversionResult = await ConvertAsyncUsingConverter(converter, converterContext);

                if (conversionResult.Status != ConversionStatus.Unhandled)
                {
                    return conversionResult;
                }

                // If "Status" is Unhandled, we move on to the next converter and try to convert with that.
            }

            return ConversionResult.Unhandled();
        }

        private ValueTask<ConversionResult> ConvertAsyncUsingConverter(IInputConverter converter, ConverterContext context)
        {
            var conversionResultTask = converter.ConvertAsync(context);

            if (conversionResultTask.IsCompletedSuccessfully)
            {
                return conversionResultTask;
            }

            return AwaitAndReturnConversionTaskResult(conversionResultTask);
        }

        private async ValueTask<ConversionResult> AwaitAndReturnConversionTaskResult(ValueTask<ConversionResult> conversionTask)
        {
            var result = await conversionTask;

            return result;
        }

        /// <summary>
        /// Gets an <see cref="IInputConverter"/> instance if converter context has information about what converter to be used.
        /// </summary>
        /// <param name="context">The converter context.</param>
        /// <returns>An IInputConverter instance or null</returns>
        private IInputConverter? GetConverterFromContext(ConverterContext context)
        {
            string? converterTypeFullName;

            // Check a converter is specified on the conversionContext.Properties. If yes, use that.
            if (context.Properties.TryGetValue(PropertyBagKeys.ConverterType, out var converterTypeAssemblyQualifiedNameObj)
                && converterTypeAssemblyQualifiedNameObj is string converterTypeAssemblyQualifiedName)
            {
                converterTypeFullName = converterTypeAssemblyQualifiedName;
            }
            else
            {
                // check the type used as "TargetType" has an "InputConverter" attribute decoration.
                converterTypeFullName = GetConverterTypeNameFromAttributeOnType(context.TargetType);
            }

            if (converterTypeFullName is not null)
            {
                return _inputConverterProvider.GetOrCreateConverterInstance(converterTypeFullName);
            }

            return null;
        }

        /// <summary>
        /// Checks a type has an "InputConverter" attribute decoration present
        /// and if present, return the assembly qualified name of the "ConverterType" property.
        /// else return null.
        /// </summary>
        private static string? GetConverterTypeNameFromAttributeOnType(Type targetType)
        {
            return _typeToConverterCache.GetOrAdd(targetType.AssemblyQualifiedName!, (key, type) =>
            {
                var converterAttribute = type.GetCustomAttributes(_inputConverterAttributeType, inherit: true)
                                             .FirstOrDefault();

                if (converterAttribute is null)
                {
                    return null;
                }

                Type converterType = ((InputConverterAttribute)converterAttribute).ConverterType;
                return converterType.AssemblyQualifiedName!;

            }, targetType);
        }
    }
}
