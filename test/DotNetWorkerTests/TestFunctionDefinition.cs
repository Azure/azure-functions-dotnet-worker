// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestFunctionDefinition : FunctionDefinition
    {
        public TestFunctionDefinition(string functionId = null, IDictionary<string, BindingMetadata> outputBindings = null, IEnumerable<FunctionParameter> parameters = null)
        {
            if (functionId is not null)
            {
                Id = functionId;
            }

            Parameters = parameters == null ? ImmutableArray<FunctionParameter>.Empty : parameters.ToImmutableArray();
            OutputBindings = outputBindings == null ? ImmutableDictionary<string, BindingMetadata>.Empty : outputBindings.ToImmutableDictionary();
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override string PathToAssembly { get; }

        public override string EntryPoint { get; }

        public override string Id { get; } = Guid.NewGuid().ToString();

        public override string Name { get; }

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }
    }
}
