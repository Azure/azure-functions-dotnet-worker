// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestFunctionDefinition : FunctionDefinition
    {
        public TestFunctionDefinition(FunctionMetadata metadata = null, IEnumerable<FunctionParameter> parameters = null, OutputBindingsInfo outputBindingsInfo = null)
        {
            Metadata = metadata;
            Parameters = parameters == null ? ImmutableArray<FunctionParameter>.Empty : parameters.ToImmutableArray();
            OutputBindingsInfo = outputBindingsInfo;
        }

        public override FunctionMetadata Metadata { get; }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override OutputBindingsInfo OutputBindingsInfo { get; }
    }
}
