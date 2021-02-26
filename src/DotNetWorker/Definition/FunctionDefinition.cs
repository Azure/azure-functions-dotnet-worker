// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A representation of a function.
    /// </summary>
    public abstract class FunctionDefinition
    {
        /// <summary>
        /// Gets the metadata for the function.
        /// </summary>
        public abstract FunctionMetadata Metadata { get; }

        /// <summary>
        /// Gets the parameters for the function.
        /// </summary>
        public abstract ImmutableArray<FunctionParameter> Parameters { get; }

        /// <summary>
        /// Gets the output bindings for the function.
        /// </summary>
        public abstract OutputBindingsInfo OutputBindingsInfo { get; }

        /// <summary>
        /// Gets custom items for the function.
        /// </summary>
        public abstract IDictionary<string, object> Items { get; }
    }
}
