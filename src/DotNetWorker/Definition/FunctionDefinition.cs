// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker
{
    public abstract class FunctionDefinition
    {
        public abstract FunctionMetadata Metadata { get; set; }

        public abstract ImmutableArray<FunctionParameter> Parameters { get; set; }

        public abstract IFunctionInvoker Invoker { get; set; }
    }
}
