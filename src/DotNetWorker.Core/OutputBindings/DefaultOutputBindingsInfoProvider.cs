// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal class DefaultOutputBindingsInfoProvider : IOutputBindingsInfoProvider
    {
        public OutputBindingsInfo GetBindingsInfo(FunctionDefinition definition)
        {
            if (HasNoOutputBindings(definition))
            {
                return EmptyOutputBindingsInfo.Instance;
            }
            else if (HasOnlyReturnBinding(definition))
            {
                return new MethodReturnOutputBindingsInfo();
            }
            else
            {
                var bindingNames = definition.OutputBindings.Keys.ToList();
                return new PropertyOutputBindingsInfo(bindingNames);
            }
        }

        private static bool HasOnlyReturnBinding(FunctionDefinition definition)
        {
            if (definition.OutputBindings.Any(kv => kv.Key.Equals(OutputBindingsConstants.ReturnBindingName, StringComparison.OrdinalIgnoreCase)))
            {
                int bindingCount = definition.OutputBindings.Count;
                if (bindingCount > 1)
                {
                    throw new InvalidOperationException($"Only one output binding is supported when using a binding assigned to '$return'. " +
                        $"Found a total of {bindingCount} bindings. For more information: https://aka.ms/dotnet-worker-poco-binding.");
                }

                return true;
            }

            return false;
        }

        private static bool HasNoOutputBindings(FunctionDefinition definition)
        {
            return !definition.OutputBindings.Any();
        }
    }
}
