// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    internal class NullFunctionActivator : IFunctionActivator
    {
        private NullFunctionActivator()
        {
        }

        public static NullFunctionActivator Instance { get; } = new NullFunctionActivator();

        public object? CreateInstance(Type instanceType, FunctionContext context)
        {
            return null;
        }
    }
}
