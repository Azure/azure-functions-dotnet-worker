// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Local representation of FunctionMetadata
    /// </summary>
    public class DefaultFunctionMetadata : IFunctionMetadata
    {
        private string? _functionId;
        private string? _name;
        private string? _entryPoint;
        private string? _scriptFile;

        /// <inheritdoc/>
        public string? FunctionId
        {
            get
            {
                _functionId ??= HashFunctionId(this);
                return _functionId;
            }
        }

        /// <inheritdoc/>
        public bool IsProxy { get; set; }

        /// <inheritdoc/>
        public string? Language { get; set; }

        /// <inheritdoc/>
        public bool ManagedDependencyEnabled { get; set; }

        /// <inheritdoc/>
        public string? Name { get => _name; set => ClearIdAndSet(value, ref _name); }

        /// <inheritdoc/>
        public string? EntryPoint { get => _entryPoint; set => ClearIdAndSet(value, ref _entryPoint); }

        /// <inheritdoc/>
        public IList<string>? RawBindings { get; set; }

        /// <inheritdoc/>
        public string? ScriptFile { get => _scriptFile; set => ClearIdAndSet(value, ref _scriptFile); }

        private static string? HashFunctionId(DefaultFunctionMetadata function)
        {
            // We use uint to avoid the '-' sign when we .ToString() the result.
            // This function is adapted from https://github.com/Azure/azure-functions-host/blob/71ecbb2c303214f96d7e17310681fd717180bdbb/src/WebJobs.Script/Utility.cs#L847-L863
            static uint GetStableHash(string value)
            {
                unchecked
                {
                    uint hash = 23;
                    foreach (char c in value)
                    {
                        hash = (hash * 31) + c;
                    }

                    return hash;
                }
            }

            unchecked
            {
                bool atLeastOnePresent = false;
                uint hash = 17;

                if (function.Name is not null)
                {
                    atLeastOnePresent = true;
                    hash = hash * 31 + GetStableHash(function.Name);
                }

                if (function.ScriptFile is not null)
                {
                    atLeastOnePresent = true;
                    hash = hash * 31 + GetStableHash(function.ScriptFile);
                }

                if (function.EntryPoint is not null)
                {
                    atLeastOnePresent = true;
                    hash = hash * 31 + GetStableHash(function.EntryPoint);
                }

                return atLeastOnePresent ? hash.ToString() : null;
            }
        }

        private void ClearIdAndSet(string? value, ref string? field)
        {
            if (!StringComparer.Ordinal.Equals(value, field))
            {
                _functionId = null;
            }

            field = value;
        }
    }
}
