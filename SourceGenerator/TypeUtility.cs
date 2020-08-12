using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace SourceGenerator
{
    internal static class TypeUtility
    {
        public static CustomAttribute GetCustomAttribute(this Mono.Cecil.ICustomAttributeProvider provider, Type parameterType)
        {
            return provider.CustomAttributes.SingleOrDefault(p => p.AttributeType.FullName == parameterType.FullName);
        }

        public static string GetReflectionFullName(this TypeReference typeRef)
        {
            return typeRef.FullName.Replace("/", "+");
        }

        public static Attribute ToReflection(this CustomAttribute customAttribute)
        {
            var attributeType = customAttribute.AttributeType.ToReflectionType();

            Type[] constructorParams = customAttribute.Constructor.Parameters
                 .Select(p => p.ParameterType.ToReflectionType())
                 .ToArray();

            Attribute attribute = attributeType.GetConstructor(constructorParams)
                .Invoke(customAttribute.ConstructorArguments.Select(p => NormalizeArg(p)).ToArray()) as Attribute;

            foreach (var namedArgument in customAttribute.Properties)
            {
                attributeType.GetProperty(namedArgument.Name)?.SetValue(attribute, namedArgument.Argument.Value);
                attributeType.GetField(namedArgument.Name)?.SetValue(attribute, namedArgument.Argument.Value);
            }

            return attribute;
        }

        public static Type ToReflectionType(this TypeReference typeDef)
        {
            Type t = Type.GetType(typeDef.GetReflectionFullName());

            if (t == null)
            {
#if NETCOREAPP2_1
                Assembly a = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(typeDef.Resolve().Module.FileName));
#else
                Assembly a = Assembly.LoadFrom(Path.GetFullPath(typeDef.Resolve().Module.FileName));
#endif
                t = a.GetType(typeDef.GetReflectionFullName());
            }

            return t;
        }

        private static object NormalizeArg(CustomAttributeArgument arg)
        {
            if (arg.Type.IsArray)
            {
                var arguments = arg.Value as CustomAttributeArgument[];
                Type arrayType = arg.Type.GetElementType().ToReflectionType();
                var array = Array.CreateInstance(arrayType, arguments.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(arguments[i].Value, i);
                }
                return array;
            }

            if (arg.Value is TypeDefinition typeDef)
            {
                return typeDef.ToReflectionType();
            }

            return arg.Value;
        }
    }
}
