// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// <see cref="FunctionContext"/> extension methods for metadata.
    /// </summary>
    internal static class FunctionContextMetadataExtensions
    {
        /// <summary>
        /// Gets the <see cref="MethodInfo"/> of the function entry point.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static MethodInfo? GetMethodInfo(this FunctionContext context)
        {
            // note:
            //  - Use caching strategy for MethodInfo if you want to speed up.
            //  - Not cached at this time because of limited use.

            var assembly = findAssembly(context.FunctionDefinition.PathToAssembly.AsSpan());
            if (assembly is null)
                return null;

            var entryPoint = context.FunctionDefinition.EntryPoint;
            return findMethod(assembly, entryPoint.AsSpan());


            #region Local Functions
            static Assembly? findAssembly(ReadOnlySpan<char> path)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var location = assembly.Location.AsSpan();
                    if (location.Equals(path, StringComparison.Ordinal))
                        return assembly;
                }
                return null;
            }


            static MethodInfo? findMethod(Assembly assembly, ReadOnlySpan<char> entryPoint)
            {
                var index = entryPoint.LastIndexOf('.');
                var typeName = entryPoint.Slice(0, index).ToString();
                var methodName = entryPoint.Slice(index + 1).ToString();
                var type = assembly.GetType(typeName);
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                return type?.GetMethod(methodName, flags);
            }
            #endregion
        }
    }
}
