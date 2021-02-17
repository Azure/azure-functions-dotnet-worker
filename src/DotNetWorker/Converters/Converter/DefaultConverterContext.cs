// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class DefaultConverterContext : ConverterContext
    {
        public DefaultConverterContext(FunctionParameter parameter, object? source, FunctionContext context)
        {
            Parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            FunctionContext = context ?? throw new System.ArgumentNullException(nameof(context));
            Source = source;
        }

        public override object? Source { get; set; }

        public override FunctionParameter Parameter { get; set; }

        public override FunctionContext FunctionContext { get; set; }
    }
}
