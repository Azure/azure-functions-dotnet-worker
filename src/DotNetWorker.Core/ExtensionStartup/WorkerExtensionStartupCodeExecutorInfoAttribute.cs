// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// An assembly level attribute to inform that this assembly contains information about 
    /// extension startup code executor type (The auto generated class which has calls to 
    /// each of the participating extension's Configure method).
    /// If any of the extensions are participating in the startup, our source generator will
    /// add this assembly attribute with information about the extension startup code executor type.
    /// </summary>

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class WorkerExtensionStartupCodeExecutorInfoAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WorkerExtensionStartupCodeExecutorInfoAttribute"/>.
        /// </summary>
        /// <param name="extensionStartupCodeExecutorType">The type of the extension startup code executor class.</param>
        /// <exception cref="ArgumentNullException">Throws when startupType is null.</exception>
        public WorkerExtensionStartupCodeExecutorInfoAttribute(Type extensionStartupCodeExecutorType)
        {
            StartupCodeExecutorType = extensionStartupCodeExecutorType ??
                                      throw new ArgumentNullException(nameof(extensionStartupCodeExecutorType));
        }

        /// <summary>
        /// Gets the type of the startup code executor.
        /// </summary>
        public Type StartupCodeExecutorType { get; }
    }
}
