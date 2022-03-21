// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Core
{
    internal class ExtensionStartupRunnner
    {
        // The attribute which is applied on the auto generated class(WorkerExtensionStartupCodeExecutor)
        // which has the code to execute each extension's startup
        private const string StartupAttributeName = "WorkerExtensionStartupRunnerAttribute";

        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            // Find the auto(source) generated class(WorkerExtensionStartupRunner)
            var entryAssembly = Assembly.GetEntryAssembly()!;
            var startupExecutorType = entryAssembly
                                        .GetTypes()
                                        .FirstOrDefault(type => type.GetCustomAttributes()
                                                                    .Any(attr => attr.GetType().Name == StartupAttributeName));

            // Our source generator will not create the file when no extension startup hooks are found.
            if (startupExecutorType == null)
            {
                return;
            }

            var method = startupExecutorType.GetMethod("RunStartupForExtensions");
            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Types decorated with {StartupAttributeName} must have a RunStartupForExtensions method.");
            }

            var startupRunnerInstance = Activator.CreateInstance(startupExecutorType);
            method.Invoke(startupRunnerInstance, parameters: new object[] { builder });
        }
    }
}
