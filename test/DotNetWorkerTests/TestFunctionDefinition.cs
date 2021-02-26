// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public partial class TestFunctionDefinition : FunctionDefinition
    {
        public TestFunctionDefinition(string functionId = null, IDictionary<string, BindingMetadata> outputBindings = null, IEnumerable<FunctionParameter> parameters = null, OutputBindingsInfo outputBindingsInfo = null)
        {
            Parameters = parameters == null ? ImmutableArray<FunctionParameter>.Empty : parameters.ToImmutableArray();
            OutputBindings = outputBindings == null ? ImmutableDictionary<string, BindingMetadata>.Empty : outputBindings.ToImmutableDictionary();
            OutputBindingsInfo = outputBindingsInfo;
            Id = functionId;
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override OutputBindingsInfo OutputBindingsInfo { get; }

        public override string PathToAssembly { get; }

        public override string EntryPoint { get; }

        public override string Id { get; }

        public override string Name { get; }

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }
    }
}
