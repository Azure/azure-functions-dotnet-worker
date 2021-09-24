// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters.Converter;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// Default implementation of <see cref="IInputConversionFeature"/>
    /// </summary>
    internal sealed class DefaultInputConversionFeature : IInputConversionFeature
    {
        private readonly IEnumerable<IInputConverter> _defaultConverters;
        private readonly IInputConverterProvider _inputConverterProvider;

        public DefaultInputConversionFeature(IInputConverterProvider inputConverterProvider)
        {
            _defaultConverters = inputConverterProvider.DefaultConverters;
            _inputConverterProvider = inputConverterProvider ?? throw new ArgumentNullException(nameof(inputConverterProvider));
        }

        public async ValueTask<ConversionResult> TryConvertAsync(ConverterContext converterContext)
        {
            // Check a converter is explicitly passed via the converter context.
            IInputConverter? converterFromContext = GetConverterFromContext(converterContext);

            if (converterFromContext != null)
            {
                var conversionResult = await ConvertAsyncUsingConverter(converterFromContext, converterContext);

                if (conversionResult.IsSuccess)
                {
                    return conversionResult;
                }
            }

            // Use the default converters.
            // The first converter to successfully convert wins.
            // For example, this allows a converter that parses JSON strings to return false if the
            // string is not valid JSON. This manager will then continue with the next matching provider.
            foreach (var defaultConverter in _defaultConverters)
            {
                var conversionResult = await ConvertAsyncUsingConverter(defaultConverter, converterContext);

                if (conversionResult.IsSuccess)
                {
                    return conversionResult;
                }
            }

            return default;
        }
                
        private ValueTask<ConversionResult> ConvertAsyncUsingConverter(IInputConverter converter, ConverterContext context)
        {
            var conversionResultTask = converter.ConvertAsync(context);

            if (conversionResultTask.IsCompletedSuccessfully)
            {
                return new ValueTask<ConversionResult>(conversionResultTask.Result);
            }
            else
            {
                return AwaitAndReturnConversionTaskResult(conversionResultTask);
            }
        }

        private async ValueTask<ConversionResult> AwaitAndReturnConversionTaskResult(ValueTask<ConversionResult> conversionResultTask)
        {
            var result = await conversionResultTask;

            return result;
        }

        private IInputConverter? GetConverterFromContext(ConverterContext context)
        {
            Type? converterType = default;

            // Check a converter is specified on the conversionContext.Properties. If yes,use that.
            if (context.Properties != null
                && context.Properties.TryGetValue(PropertyBagKeys.ConverterType, out var converterTypeObj))
            {
                converterType = (Type)converterTypeObj;
            }
            else
            {
                // check the class used as TargetType has a BindingConverter attribute decoration.
                var binderType = typeof(InputConverterAttribute);
                var binderAttr = context.TargetType.GetCustomAttributes(binderType, inherit: true).FirstOrDefault();

                if (binderAttr != null)
                {
                    converterType = ((InputConverterAttribute)binderAttr).ConverterType;
                }
            }

            if (converterType != null)
            {
                return this._inputConverterProvider.GetConverterInstance(converterType);
            }

            return null;
        }
    }
}
