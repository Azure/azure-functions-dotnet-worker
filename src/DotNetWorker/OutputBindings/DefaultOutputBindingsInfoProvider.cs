// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal class DefaultOutputBindingsInfoProvider : IOutputBindingsInfoProvider
    {
        public OutputBindingsInfo GetBindingsInfo(FunctionMetadata metadata)
        {
            if (HasNoOutputBindings(metadata))
            {
                return EmptyOutputBindingsInfo.Instance;
            }
            else if (HasOnlyReturnBinding(metadata))
            {
                return new MethodReturnOutputBindingsInfo();
            }
            else
            {
                var bindingNames = metadata.OutputBindings.Keys.ToList();
                return new PropertyOutputBindingsInfo(bindingNames);
            }
        }

        private static bool HasOnlyReturnBinding(FunctionMetadata metadata)
        {
            if (metadata.OutputBindings.Any(kv => kv.Key.Equals(OutputBindingsConstants.ReturnBindingName, StringComparison.OrdinalIgnoreCase)))
            {
                int bindingCount = metadata.OutputBindings.Count;
                if (bindingCount > 1)
                {
                    throw new InvalidOperationException($"Only one output binding is supported when using a binding assigned to '$return'. " +
                        $"Found a total of {bindingCount} bindings. For more information: https://aka.ms/dotnet-worker-poco-binding.");
                }

                return true;
            }

            return false;
        }

        private static bool HasNoOutputBindings(FunctionMetadata metadata)
        {
            return !metadata.OutputBindings.Any();
        }
    }
}
