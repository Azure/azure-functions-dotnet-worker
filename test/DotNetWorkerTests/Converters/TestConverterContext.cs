// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    internal class TestConverterContext : ConverterContext
    {
        public TestConverterContext(string paramName, Type paramType, object source, FunctionContext context = null)
        {
            Parameter = new FunctionParameter(paramName, paramType);
            Source = source;
            FunctionContext = context ?? new TestFunctionContext();
        }

        public override FunctionParameter Parameter { get; set; }

        public override object Source { get; set; }

        public override FunctionContext FunctionContext { get; set; }
    }
}
