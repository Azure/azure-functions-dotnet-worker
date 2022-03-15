// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Core
{
    // to do: Catch exception on each Configure call of exn startup type instance.
    internal class ExtensionStartupRunnner
    {
        private const string StartupDataAttributeName = "ExtensionStartupDataAttribute";

        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            // Find the auto(source) generated class which has extension startup type name and assembly names.
            Type? startupDataProviderGeneratedType = (Assembly.GetEntryAssembly())!.GetTypes()
                .FirstOrDefault(v => v.GetCustomAttributes()
                                      .Any(at => at.GetType().Name == StartupDataAttributeName));

            if (startupDataProviderGeneratedType == null)
            {
                //ServiceCollectionExtensions.cs
                return;
            }

            var method = startupDataProviderGeneratedType.GetMethod("RunStartup");
            if (method == null)
            {
                throw new InvalidOperationException($"Types decorated with {StartupDataAttributeName} must have a GetStartupTypes method.");
            }

            var extensionStartupType = Activator.CreateInstance(startupDataProviderGeneratedType);
            var methodInvocationResult = method!.Invoke(extensionStartupType, parameters: new [] { builder });
        }
    }
}
