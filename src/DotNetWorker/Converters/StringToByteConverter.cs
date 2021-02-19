// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class StringToByteConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = default;

            if (!(context.Parameter.Type.IsAssignableFrom(typeof(byte[])) &&
                  context.Source is string sourceString))
            {
                return false;
            }

            target = Encoding.UTF8.GetBytes(sourceString);
            return true;
        }
    }
}
