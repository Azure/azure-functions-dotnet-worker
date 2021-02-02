// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestFunctionDefinition : FunctionDefinition
    {

        public override FunctionMetadata Metadata { get; set; }

        public override ImmutableArray<FunctionParameter> Parameters { get; set; }

        public override IFunctionInvoker Invoker { get; set; }
    }
}
