// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.Converters
{
    internal class FromBodyConverter : IInputConverter
    {
        private readonly IFromBodyConversionHandler _conversionHandler;

        public FromBodyConverter(IFromBodyConversionHandler? handler = null)
        {
            _conversionHandler = handler ?? new DefaultFromBodyConversionHandler();
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            return _conversionHandler.ConvertAsync(context);
        }
    }
}
