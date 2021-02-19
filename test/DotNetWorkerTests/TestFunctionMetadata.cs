// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal class TestFunctionMetadata : FunctionMetadata
    {
        public override string PathToAssembly { get; set; }

        public override string EntryPoint { get; set; }

        public override string FunctionId { get; set; }

        public override string Name { get; set; }

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; set; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; set; }
    }
}
