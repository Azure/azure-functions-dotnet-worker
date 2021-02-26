// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker
{
    public abstract class FunctionMetadata
    {
        public abstract string PathToAssembly { get; set; }

        public abstract string EntryPoint { get; set; }

        public abstract string FunctionId { get; set; }

        public abstract string Name { get; set; }

        public abstract IImmutableDictionary<string, BindingMetadata> InputBindings { get; set; }

        public abstract IImmutableDictionary<string, BindingMetadata> OutputBindings { get; set; }
    }
}
