using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace SourceGenerator
{
    internal static class TypeInfoExtensions
    {
        public static bool IsImplementing(this TypeInfo typeInfo, string interfaceName)
        {
            return typeInfo.ImplementedInterfaces.Any(i => i.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));
        }

        public static CustomAttribute GetDisabledAttribute(this TypeDefinition type)
        {
            return type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }
    }
}