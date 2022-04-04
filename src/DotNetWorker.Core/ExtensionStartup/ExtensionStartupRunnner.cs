// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Core
{
    internal class ExtensionStartupRunnner
    {
        /// <summary>
        /// Run extension startup execution code.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> instance.</param>

        /// Our source generator creates a class(WorkerExtensionStartupCodeExecutor)
        /// which internally calls the "Configure" method on each of the participating 
        /// extensions. Here we are calling the uber "Configure" method on the generated class.
        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            var entryAssembly = Assembly.GetEntryAssembly()!;

            // Find the assembly attribute which has information about the startup code executor class
            var startupCodeExecutorInfoAttr = entryAssembly.GetCustomAttribute<WorkerExtensionStartupCodeExecutorInfoAttribute>();

            // Our source generator will not create the WorkerExtensionStartupCodeExecutor class
            // and will not add the above assembly attribute when no extension startup hooks are found.
            if (startupCodeExecutorInfoAttr == null)
            {
                return;
            }

            var startupCodeExecutorInstance =
                Activator.CreateInstance(startupCodeExecutorInfoAttr
                    .StartupCodeExecutorType) as IWorkerExtensionStartup;
            startupCodeExecutorInstance!.Configure(builder);
        }
    }
}
