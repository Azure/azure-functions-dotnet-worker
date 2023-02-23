// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

internal partial class FunctionExecutorGenerator
{
    /// <summary>
    /// A type which holds information about the parent class of a function.
    /// </summary>
    internal class FunctionClass
    {
        public FunctionClass(string fullyQualifiedClassName)
        {
            ClassName = fullyQualifiedClassName;
        }

        internal string ClassName { get; }
        
        /// <summary>
        /// A collection of fully qualified type names of the constructor argument.
        /// </summary>
        internal IEnumerable<string> ConstructorParameterTypeNames { set; get; } = Enumerable.Empty<string>();
    }
}
