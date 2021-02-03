// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class DefaultConverterContext : ConverterContext
    {
        public DefaultConverterContext(FunctionParameter parameter, object? source, FunctionExecutionContext context)
        {
            Parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            ExecutionContext = context ?? throw new System.ArgumentNullException(nameof(context));
            Source = source;
        }

        public override object? Source { get; set; }

        public override FunctionParameter Parameter { get; set; }

        public override FunctionExecutionContext ExecutionContext { get; set; }
    }
}
