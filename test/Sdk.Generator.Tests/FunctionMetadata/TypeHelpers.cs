// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public static class TypeHelpers
    {
        public static MethodDefinition? GetMethodDefinition(Type type, string methodName)
        {
            return GetTypeDefinition(type).Methods.SingleOrDefault(p => p.Name == methodName);
        }

        public static TypeDefinition GetTypeDefinition(Type type)
        {
            var module = ModuleDefinition.ReadModule(type.Assembly.Location);
            return module.GetType(type.FullName!.Replace("+", "/"));
        }

        public static IEnumerable<CustomAttribute> GetCustomAttributes(Type type, string methodName, string parameterName)
        {
            var methodDef = GetMethodDefinition(type, methodName);
            var paramDef = methodDef!.Parameters.SingleOrDefault(p => p.Name == parameterName);
            return paramDef!.CustomAttributes;
        }
    }
}
