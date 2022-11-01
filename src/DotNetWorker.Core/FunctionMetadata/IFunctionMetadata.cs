// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Public interface that represents properties in function metadata.
    /// </summary>
    public interface IFunctionMetadata
    {
        /// <summary>
        /// Unique function id.
        /// </summary>
        string? FunctionId { get; }

        /// <summary>
        /// If function is a proxy function return true, else return false.
        /// </summary>
        bool IsProxy { get; }

        /// <summary>
        /// Language that the function is written in.
        /// </summary>
        string? Language { get; }

        /// <summary>
        /// If managed dependency is enabled return true, else return false.
        /// </summary>
        bool ManagedDependencyEnabled { get; }

        /// <summary>
        /// Name of function.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Function entrypoint (ex. HttpTrigger.Run).
        /// </summary>
        string? EntryPoint { get; }

        /// <summary>
        /// List of function's bindings in json string format.
        /// </summary>
        IList<string>? RawBindings { get; }
        
        /// <summary>
        /// The function app assembly (ex. FunctionApp.dll).
        /// </summary>
        string? ScriptFile { get; }
    }
}
