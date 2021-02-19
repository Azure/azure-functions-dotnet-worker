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
    internal class DefaultOutputBindingsInfoFactory : IOutputBindingsInfoFactory
    {
        public OutputBindingsInfo Build(FunctionMetadata metadata)
        {
            if (HasNoOutputBindings(metadata))
            {
                return NoOutputBindingsInfo.Instance;
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
                    throw new InvalidOperationException($"You can only have 1 output binding if using '$return' output binding. " +
                        $"Instead found {bindingCount} total bindings.");
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
