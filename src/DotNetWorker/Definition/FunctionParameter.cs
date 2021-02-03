// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Definition
{
    public class FunctionParameter
    {
        public FunctionParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public Type Type { get; }

        // TODO: Pop out to Context (or Invocation)
        public object? Value { get; set; }
    }
}
