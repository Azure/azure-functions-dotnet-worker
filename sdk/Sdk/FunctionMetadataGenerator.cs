using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class FunctionMetadataGenerator
    {
        private readonly Action<TraceLevel, string> _log;

        public FunctionMetadataGenerator()
            : this((l, m) => { })
        {
        }

        public FunctionMetadataGenerator(Action<TraceLevel, string> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(string assemblyPath)
        {
            string path = Path.GetDirectoryName(typeof(object).Assembly.Location);

            string[] runtimeAssemblies = Directory.GetFiles(path, "*.dll");
            string[] outputAssemblies = Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*.dll");

            var paths = new List<string>(runtimeAssemblies);
            paths.AddRange(outputAssemblies);

            var resolver = new PathAssemblyResolver(paths);

            using (var loadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.FullName))
            {
                var functions = new List<SdkFunctionMetadata>();

                Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

                foreach (Type t in assembly.GetTypes())
                {
                    functions.AddRange(GenerateFunctionMetadata(t));
                }

                return functions;
            }
        }

        internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(Type t)
        {
            var functions = new List<SdkFunctionMetadata>();

            foreach (MethodInfo method in t.GetMethods())
            {
                if (!TryCreateFunctionMetadata(method, out SdkFunctionMetadata? metadata)
                    || metadata == null)
                {
                    continue;
                }

                foreach (var binding in CreateBindingMetadata(method))
                {
                    metadata.Bindings.Add(binding);
                }

                functions.Add(metadata);
            }

            return functions;
        }


        private IEnumerable<ExpandoObject> CreateBindingMetadata(MethodInfo method)
        {
            var bindingMetadata = new List<ExpandoObject>();

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

                if (param == null)
                {
                    continue;
                }

                string? paramName = param.Name;
                object? paramValue = arg.Value;

                if (paramName == null || paramValue == null)
                {
                    continue;
                }

                // Temporary fix for timer trigger attribute property being different
                // from what is expected in FunctionMetadata
                // https://github.com/Azure/azure-functions-host/issues/6989
                if (string.Equals(paramName, "scheduleExpression", StringComparison.OrdinalIgnoreCase))
                {
                    paramName = "schedule";
                }

                paramValue = GetParamValue(param.ParameterType, paramValue);

                properties[paramName] = paramValue!;
            }

            foreach (var namedArgument in attribute.NamedArguments)
            {
                object? argValue = namedArgument.TypedValue.Value;

                if (argValue == null)
                {
                    continue;
                }

                argValue = GetParamValue(namedArgument.TypedValue.ArgumentType, argValue);

                properties[namedArgument.MemberName] = argValue!;
            }

            return properties;
        }

        internal static object? GetParamValue(Type paramType, object arg)
        {
            if (paramType.IsEnum)
            {
                return Enum.GetName(paramType, arg);
            }
            else if (paramType.IsArray)
            {
                var arrayValue = arg as ReadOnlyCollection<CustomAttributeTypedArgument>;
                return arrayValue.Select(p => p.Value).ToArray();
            }
            else
            {
                return arg;
            }
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

        private bool TryCreateFunctionMetadata(MethodInfo method, out SdkFunctionMetadata? function)
        {
            function = null;


            foreach (CustomAttributeData attribute in method.GetCustomAttributesData())
            {
                if (attribute.AttributeType != null) //.Name == "Microsoft.Azure.WebJobs.FunctionNameAttribute")
                {
                    Type? declaringType = method.DeclaringType;

                    if (declaringType == null)
                    {
                        continue; ;
                    }

                    string? assemblyName = declaringType.Assembly.GetName().Name;

                    if (string.IsNullOrEmpty(assemblyName))
                    {
                        continue;
                    }

                    var functionName = attribute.ConstructorArguments.SingleOrDefault().Value?.ToString();

                    if (string.IsNullOrEmpty(functionName))
                    {
                        continue;
                    }

                    function = new SdkFunctionMetadata
                    {
                        Name = functionName,
                        ScriptFile = $"bin/{assemblyName}.dll",
                        EntryPoint = $"{declaringType.FullName}.{method.Name}",
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