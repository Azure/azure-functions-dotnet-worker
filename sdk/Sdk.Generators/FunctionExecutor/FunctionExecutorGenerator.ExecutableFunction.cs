// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

internal partial class FunctionExecutorGenerator
{
    /// <summary>
    /// A type which holds information about the functions which can be executed from an invocation.
    /// </summary>
    internal class ExecutableFunction
    {
        /// <summary>
        ///  True if the function returns Task or void.
        /// </summary>
        internal bool IsReturnValueAssignable { set; get; }

        /// <summary>
        /// Whether the function should be awaited or not for getting the result of execution.
        /// </summary>
        internal bool ShouldAwait { get; set; }

        /// <summary>
        /// The method name(which is part of EntryPoint prop value).
        /// </summary>
        internal string MethodName { get; set; } = null!;

        internal bool IsStatic { get; set; }

        /// <summary>
        /// Ex: MyNamespace.MyClass.MyMethodName
        /// </summary>
        internal string EntryPoint { get; set; } = null!;

        internal FunctionClass ParentFunctionClass { set; get; } = null!;

        /// <summary>
        /// A collection of fully qualified type names of the parameters of the function.
        /// </summary>
        internal IEnumerable<string> ParameterTypeNames { set; get; } = Enumerable.Empty<string>();
    }
}
