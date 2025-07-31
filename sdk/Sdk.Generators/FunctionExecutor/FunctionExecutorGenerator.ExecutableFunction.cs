// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// A type which holds information about the functions which can be executed from an invocation.
    /// </summary>
    internal class ExecutableFunction
    {
        /// <summary>
        ///  False if the function returns Task or void.
        /// </summary>
        internal bool IsReturnValueAssignable { set; get; }

        /// <summary>
        /// Whether the function should be awaited or not for getting the result of execution.
        /// </summary>
        internal bool ShouldAwait { get; set; }

        /// <summary>
        /// The method name (which is part of EntryPoint property value).
        /// </summary>
        internal string MethodName { get; set; } = null!;

        /// <summary>
        /// A value indicating whether the function is static or not.
        /// </summary>
        internal bool IsStatic { get; set; }

        /// <summary>
        /// Ex: MyNamespace.MyClass.MyMethodName
        /// </summary>
        internal string EntryPoint { get; set; } = null!;

        /// <summary>
        /// Type name of the parent class in default symbol format.
        /// Ex: MyNamespace.MyClass
        /// </summary>
        internal string ParentFunctionClassName { get; set; } = null!;

        /// <summary>
        /// Fully qualified type name of the parent class.
        /// Ex: global::MyNamespace.MyClass
        /// </summary>
        internal string ParentFunctionFullyQualifiedClassName { get; set; } = null!;

        /// <summary>
        /// A collection of fully qualified type names of the parameters of the function.
        /// </summary>
        internal IEnumerable<string> ParameterTypeNames { set; get; } = Enumerable.Empty<string>();

        /// <summary>
        /// Get a value indicating the visibility of the executable function.
        /// </summary>
        internal FunctionMethodVisibility Visibility { get; set; }

        /// <summary>
        /// Gets the assembly identity of the function.
        /// ex: FooAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=9475d07f10cb09df
        /// </summary>
        internal string AssemblyIdentity { get; set; } = null!;

        /// <summary>
        /// Gets or sets if the function is Obsolete.
        /// </summary>
        internal bool IsObsolete { get; set; }
    }
}
