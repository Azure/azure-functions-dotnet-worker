// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// An assembly level attribute to inform that this assembly contains a worker extension startup implementation type.
    /// </summary>

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class WorkerExtensionStartupAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WorkerExtensionStartupAttribute"/>.
        /// </summary>
        /// <param name="startupType">The type of the extension startup class implementation.</param>
        /// <exception cref="ArgumentNullException">Throws when startupType is null.</exception>

        public WorkerExtensionStartupAttribute(Type startupType)
        {
            StartupType = startupType ?? throw new ArgumentNullException(nameof(startupType));
        }

        /// <summary>
        /// Gets the type of the startup class implementation.
        /// </summary>
        public Type StartupType { get; }
    }
}
