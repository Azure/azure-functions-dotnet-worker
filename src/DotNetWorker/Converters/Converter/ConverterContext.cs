// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal abstract class ConverterContext
    {
        public abstract FunctionParameter Parameter { get; set; }

        public abstract object? Source { get; set; }

        public abstract FunctionContext FunctionContext { get; set; }
    }
}
