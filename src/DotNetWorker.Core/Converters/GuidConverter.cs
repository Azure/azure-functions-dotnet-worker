// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Guid/Guid? type parameters.
    /// </summary>
    internal class GuidConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = default;

            if (context.Parameter.Type == typeof(Guid) || context.Parameter.Type == typeof(Guid?))
            {
                if (context.Source is string sourceString && Guid.TryParse(sourceString, out Guid parsedGuid))
                {
                    target = parsedGuid;
                    return true;
                }
            }

            return false;
        }
    }
}
