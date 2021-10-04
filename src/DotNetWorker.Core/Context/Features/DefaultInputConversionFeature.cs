﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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

            if (converterFromContext != null)
            {
                var conversionResult = await ConvertAsyncUsingConverter(converterFromContext, converterContext);

                if (conversionResult.IsHandled)
                {
                    return conversionResult;
                }
            }

            // The first converter which can handle the conversion wins.
            foreach (var converter in _inputConverterProvider.DefaultConverters)
            {
                var conversionResult = await ConvertAsyncUsingConverter(converter, converterContext);

                if (conversionResult.IsHandled)
                {
                    return conversionResult;
                }
                            
                // If "IsHandled" is false, we move on to the next converter and try to convert with that.
            }

            return ConversionResult.Unhandled();
        }

        private ValueTask<ConversionResult> ConvertAsyncUsingConverter(IInputConverter converter, ConverterContext context)
        {
            var conversionResultTask = converter.ConvertAsync(context);

            if (conversionResultTask.IsCompletedSuccessfully)
            {
                return new ValueTask<ConversionResult>(conversionResultTask.Result);
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
            Type? converterType = default;

            // Check a converter is specified on the conversionContext.Properties. If yes,use that.
            if (context.Properties != null
                && context.Properties.TryGetValue(PropertyBagKeys.ConverterType, out var converterTypeAssemblyQualifiedNameObj)
                && converterTypeAssemblyQualifiedNameObj is string converterTypeAssemblyQualifiedName)
            {
                converterType = Type.GetType(converterTypeAssemblyQualifiedName);
            }
            else
            {
                // check the type used as "TargetType" has an "InputConverter" attribute decoration.
                var converterAttribute = context.TargetType.GetCustomAttributes(_inputConverterAttributeType, inherit: true)
                                                           .FirstOrDefault();

                if (converterAttribute != null)
                {
                    converterType = ((InputConverterAttribute)converterAttribute).ConverterType;
                }
            }

            if (converterType != null)
            {
                return _inputConverterProvider.GetOrCreateConverterInstance(converterType);
            }

            return null;
        }
    }
}
