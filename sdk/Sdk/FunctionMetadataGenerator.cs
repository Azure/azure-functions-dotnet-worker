using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class FunctionMetadataGenerator
    {
        public IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(string assemblyPath)
        {
            var functions = new List<SdkFunctionMetadata>();

            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            string[] outputAssemblies = Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*.dll");

            var paths = new List<string>(runtimeAssemblies);
            paths.AddRange(outputAssemblies);

            var resolver = new PathAssemblyResolver(paths);

            using (var loadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.FullName))
            {

                Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                AssemblyName name = assembly.GetName();

                foreach (Type t in assembly.GetTypes())
                {
                    foreach (MethodInfo method in t.GetMethods())
                    {
                        if (!TryCreateFunctionMetadata(method, out SdkFunctionMetadata metadata))
                        {
                            continue;
                        }

                        foreach (var binding in CreateBindingMetadata(method))
                        {
                            metadata.Bindings.Add(binding);
                        }

                        functions.Add(metadata);
                    }
                }
            }

            return functions;
        }

        private IEnumerable<object> CreateBindingMetadata(MethodInfo method)
        {
            var bindingMetadata = new List<object>();

            foreach (var parameter in method.GetParameters())
            {
                foreach (var attribute in parameter.GetCustomAttributesData())
                {
                    if (IsWebJobsBinding(attribute))
                    {
                        string bindingType = attribute.AttributeType.Name.Replace("Attribute", string.Empty);

                        dynamic binding = new ExpandoObject();
                        binding.Name = parameter.Name;
                        binding.Type = bindingType;
                        binding.Direction = GetBindingDirection(parameter);

                        var bindingDict = (IDictionary<string, object>)binding;
                        foreach (var property in GetAttributeProperties(attribute))
                        {
                            bindingDict.Add(property.Key, property.Value);
                        }

                        bindingMetadata.Add(binding);

                        // TODO: Fix $return detection
                        // auto-add a return type for http for now
                        if (string.Equals(bindingType, "httptrigger", StringComparison.OrdinalIgnoreCase))
                        {
                            dynamic returnBinding = new ExpandoObject();
                            returnBinding.Name = "$return";
                            returnBinding.Type = "http";
                            returnBinding.Direction = "Out";

                            bindingMetadata.Add(returnBinding);
                        }
                    }
                }
            }

            return bindingMetadata;
        }

        private IDictionary<string, object> GetAttributeProperties(CustomAttributeData attribute)
        {
            var properties = new Dictionary<string, object>();

            // To avoid needing to instantiate any types, assume that the constructor
            // argument names are equal to property names.
            var constructorParams = attribute.Constructor.GetParameters();
            for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
            {
                var arg = attribute.ConstructorArguments[i];
                var param = constructorParams[i];

                string paramName = param.Name;
                object paramValue = arg.Value;

                if (param.ParameterType.IsEnum)
                {
                    paramValue = Enum.GetName(param.ParameterType, arg.Value);
                }
                else if (param.ParameterType.IsArray)
                {
                    var arrayValue = arg.Value as ReadOnlyCollection<CustomAttributeTypedArgument>;
                    paramValue = arrayValue.Select(p => p.Value);
                }

                properties[paramName] = paramValue;
            }

            return properties;
        }

        private string GetBindingDirection(ParameterInfo parameter)
        {
            if (parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition().FullName == "Microsoft.Azure.Functions.Worker.OutputBinding`1")
            {
                return "Out";
            }

            return "In";
        }

        private static bool IsWebJobsBinding(CustomAttributeData attribute)
        {
            return attribute.AttributeType.GetCustomAttributesData()
                .Any(p => p.AttributeType.FullName == "Microsoft.Azure.WebJobs.Description.BindingAttribute");
        }

        private static bool TryCreateFunctionMetadata(MethodInfo method, out SdkFunctionMetadata function)
        {
            function = null;

            foreach (CustomAttributeData attribute in method.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName == "Microsoft.Azure.WebJobs.FunctionNameAttribute")
                {
                    string assemblyName = method.DeclaringType.Assembly.GetName().Name;

                    function = new SdkFunctionMetadata
                    {
                        Name = attribute.ConstructorArguments.Single().Value.ToString(),
                        ScriptFile = $"bin/{assemblyName}.dll",
                        EntryPoint = $"{method.DeclaringType.FullName}.{method.Name}",
                        Language = "dotnet-isolated"
                    };

                    function.Properties["IsCodeless"] = false;

                    return true;
                }
            }

            return false;
        }
    }
}