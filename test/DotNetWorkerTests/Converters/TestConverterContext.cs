// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    internal class TestConverterContext : ConverterContext
    {
        public TestConverterContext(Type targetType, object source, FunctionContext context = null)
        {
            TargetType = targetType;
            Source = source;
            FunctionContext = context ?? new TestFunctionContext();
        }

        public override object Source { get; }

        public override FunctionContext FunctionContext { get; }

        public override Type TargetType { get; }

        public override IReadOnlyDictionary<string, object> Properties { get; }
    }
}
