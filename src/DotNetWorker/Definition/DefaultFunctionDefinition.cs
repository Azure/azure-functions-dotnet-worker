// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Definition
{
    internal class DefaultFunctionDefinition : FunctionDefinition
    {
        public DefaultFunctionDefinition(FunctionMetadata metadata, IEnumerable<FunctionParameter> parameters, OutputBindingsInfo outputBindings)
        {
            Metadata = metadata;
            Parameters = parameters.ToImmutableArray();
            OutputBindingsInfo = outputBindings;
        }

        public override FunctionMetadata Metadata { get; }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override OutputBindingsInfo OutputBindingsInfo { get; }
    }
}
