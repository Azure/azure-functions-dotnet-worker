// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class StringToByteConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!(context.TargetType.IsAssignableFrom(typeof(byte[])) &&
                  context.Source is string sourceString))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            var byteArray = Encoding.UTF8.GetBytes(sourceString);
            var conversionResult = ConversionResult.Success(byteArray);

            return new ValueTask<ConversionResult>(conversionResult);
        }
    }
}
