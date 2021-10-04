﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal sealed class DefaultConverterContextFactory : IConverterContextFactory
    {
        /// <inheritdoc/>
        public ConverterContext Create(Type targetType, object? source, FunctionContext functionContext)
        {
            return new DefaultConverterContext(targetType, source, functionContext);
        }
    }
}
