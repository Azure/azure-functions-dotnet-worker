// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// A type which holds information about the parent class of a function.
    /// </summary>
    internal class FunctionClass
    {
        public FunctionClass(string fullyQualifiedClassName)
        {
            Name = fullyQualifiedClassName;
        }

        internal string Name { get; }
    }
}
