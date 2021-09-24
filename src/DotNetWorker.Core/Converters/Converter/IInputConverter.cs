﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    public interface IInputConverter
    {
        ValueTask<ConversionResult> ConvertAsync(ConverterContext context);
    }

}
