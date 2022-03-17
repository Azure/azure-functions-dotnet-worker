// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// An attribute to inform that this assembly contains a worker extension startup type.
    /// </summary>

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class WorkerExtensionStartupAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WorkerExtensionStartupAttribute"/>.
        /// </summary>
<<<<<<< HEAD
        /// <param name="startupType">The type of the extension startup class implementation.</param>
        /// <exception cref="InvalidOperationException">Throws when startupType is not an implementation of IWorkerExtensionStartup.</exception>
        /// <exception cref="ArgumentNullException">Throws when startupType is null.</exception>
=======
        /// <param name="startupType">The type of the extension startup class implementation.
        /// defaulted based on naming convention.</param>
>>>>>>> f10c7430b38af4ae100733def2605f7c25feca0e
        public WorkerExtensionStartupAttribute(Type startupType)
        {
            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            var interfaceType = typeof(IWorkerExtensionStartup);
            if (!interfaceType.IsAssignableFrom(startupType))
            {
                throw new InvalidOperationException($"{startupType.Name} must implement {interfaceType.FullName} to be used as an input converter.");
            }

            StartupType = startupType;
        }

        /// <summary>
        /// Gets the type of the startup class implementation.
        /// </summary>
        public Type StartupType { get; }
    }
}
