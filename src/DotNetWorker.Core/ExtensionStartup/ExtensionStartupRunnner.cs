// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Core
{
    internal class ExtensionStartupRunnner
    {
        private const string StartupDataAttributeName = "WorkerExtensionStartupAttribute";

        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            // Find the auto(source) generated class which has extension startup type name and assembly names.
            Type? startupDataProviderGeneratedType = (Assembly.GetEntryAssembly())!.GetTypes()
                .FirstOrDefault(v => v.GetCustomAttributes()
                                      .Any(at => at.GetType().Name == StartupDataAttributeName));

            // Our source generator will not create the file when no extension startup hooks are found.
            if (startupDataProviderGeneratedType == null)
            {
                return;
            }

            
            var method = startupDataProviderGeneratedType.GetMethod("RunStartupForExtensions");
            if (method == null)
            {
                throw new InvalidOperationException($"Types decorated with {StartupDataAttributeName} must have a RunStartupForExtensions method.");
            }

            var extensionStartupType = Activator.CreateInstance(startupDataProviderGeneratedType);
            method.Invoke(extensionStartupType, parameters: new[] { builder });
        }
    }
}
