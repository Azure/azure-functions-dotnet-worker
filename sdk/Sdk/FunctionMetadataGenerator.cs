using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class FunctionMetadataGenerator
    {
        private readonly IndentableLogger _logger;

        public FunctionMetadataGenerator()
            : this((l, m) => { })
        {
        }

        public FunctionMetadataGenerator(Action<TraceLevel, string> log)
        {
            _logger = new IndentableLogger(log);
        }

        public IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(string assemblyPath, IEnumerable<string> referencePaths)
        {
            string sourcePath = Path.GetDirectoryName(assemblyPath);
            string[] targetAssemblies = Directory.GetFiles(sourcePath, "*.dll");

            var functions = new List<SdkFunctionMetadata>();

            _logger.LogMessage($"Found { targetAssemblies.Length} assemblies to evaluate in '{sourcePath}':");

            foreach (var path in targetAssemblies)
            {
                using (_logger.Indent())
                {
                    _logger.LogMessage($"{Path.GetFileName(path)}");

                    using (_logger.Indent())
                    {
                        try
                        {
                            BaseAssemblyResolver resolver = new DefaultAssemblyResolver();

                            foreach (string referencePath in referencePaths.Select(p => Path.GetDirectoryName(p)).Distinct())
                            {
                                resolver.AddSearchDirectory(referencePath);
                            }

                            resolver.AddSearchDirectory(Path.GetDirectoryName(path));

                            ReaderParameters readerParams = new ReaderParameters { AssemblyResolver = resolver };

                            var moduleDefintion = ModuleDefinition.ReadModule(path, readerParams);

                            functions.AddRange(GenerateFunctionMetadata(moduleDefintion));
                        }
                        catch (BadImageFormatException)
                        {
                            _logger.LogMessage($"Skipping file '{Path.GetFileName(path)}' because of a {nameof(BadImageFormatException)}.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Could not evaluate '{Path.GetFileName(path)}' for functions metadata. Exception message: {ex.ToString()}");
                        }
                    }
                }
            }

            return functions;
        }

        internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(ModuleDefinition module)
        {
            var functions = new List<SdkFunctionMetadata>();

            foreach (TypeDefinition type in module.Types)
            {
                var functionsResult = GenerateFunctionMetadata(type).ToArray();
                if (functionsResult.Length > 0)
                {
                    _logger.LogMessage($"Found {functionsResult.Length} functions in '{type.GetReflectionFullName()}'.");
                }

                functions.AddRange(functionsResult);
            }

            return functions;
        }

        internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(TypeDefinition type)
        {
            var functions = new List<SdkFunctionMetadata>();

            foreach (MethodDefinition method in type.Methods)
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


        private IEnumerable<ExpandoObject> CreateBindingMetadata(MethodDefinition method)
        {
            var bindingMetadata = new List<ExpandoObject>();

            foreach (ParameterDefinition parameter in method.Parameters)
            {
                foreach (CustomAttribute attribute in parameter.CustomAttributes)
                {
                    if (IsWebJobsBinding(attribute))
                    {
                        string bindingType = attribute.AttributeType.Name.Replace("Attribute", string.Empty);

                        ExpandoObject binding = new ExpandoObject();
                        var bindingDict = (IDictionary<string, object>)binding;
                        bindingDict["Name"] = parameter.Name;
                        bindingDict["Type"] = bindingType;
                        bindingDict["Direction"] = GetBindingDirection(parameter);

                        foreach (var property in GetAttributeProperties(attribute))
                        {
                            bindingDict.Add(property.Key, property.Value);
                        }

                        bindingMetadata.Add(binding);

                        // TODO: Fix $return detection
                        // auto-add a return type for http for now
                        if (string.Equals(bindingType, "httptrigger", StringComparison.OrdinalIgnoreCase))
                        {
                            IDictionary<string, object> returnBinding = new ExpandoObject();
                            returnBinding["Name"] = "$return";
                            returnBinding["Type"] = "http";
                            returnBinding["Direction"] = "Out";

                            bindingMetadata.Add((ExpandoObject)returnBinding);
                        }
                    }
                }
            }

            return bindingMetadata;
        }

        private IDictionary<string, object> GetAttributeProperties(CustomAttribute attribute)
        {
            var properties = new Dictionary<string, object>();

            // To avoid needing to instantiate any types, assume that the constructor
            // argument names are equal to property names.
            var constructorParams = attribute.Constructor.Resolve().Parameters;
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

            foreach (var namedArgument in attribute.Properties)
            {
                object? argValue = namedArgument.Argument.Value;

                if (argValue == null)
                {
                    continue;
                }

                argValue = GetParamValue(namedArgument.Argument.Type, argValue);

                properties[namedArgument.Name] = argValue!;
            }

            return properties;
        }

        internal static object? GetParamValue(TypeReference paramType, object arg)
        {
            if (TryGetEnumName(paramType.Resolve(), arg, out string? enumName))
            {
                return enumName;
            }
            else if (paramType.IsArray)
            {
                var arrayValue = arg as IEnumerable<CustomAttributeArgument>;
                return arrayValue.Select(p => p.Value).ToArray();
            }
            else
            {
                return arg;
            }
        }

        private static bool TryGetEnumName(TypeDefinition typeDef, object enumValue, out string? enumName)
        {
            if (typeDef.IsEnum)
            {
                enumName = typeDef.Fields.Single(f => Equals(f.Constant, enumValue)).Name;
                return true;
            }

            enumName = null;
            return false;
        }

        private string GetBindingDirection(ParameterDefinition parameter)
        {
            if (parameter.ParameterType.IsGenericInstance &&
                parameter.ParameterType.Resolve().FullName == "Microsoft.Azure.Functions.Worker.OutputBinding`1")
            {
                return "Out";
            }

            return "In";
        }

        private static bool IsWebJobsBinding(CustomAttribute attribute)
        {
            return attribute.AttributeType.Resolve().CustomAttributes
                .Any(p => p.AttributeType.FullName == "Microsoft.Azure.WebJobs.Description.BindingAttribute");
        }

        private bool TryCreateFunctionMetadata(MethodDefinition method, out SdkFunctionMetadata? function)
        {
            function = null;

            foreach (CustomAttribute attribute in method.CustomAttributes)
            {
                if (attribute.AttributeType.FullName == "Microsoft.Azure.WebJobs.FunctionNameAttribute")
                {
                    TypeDefinition declaringType = method.DeclaringType;

                    string assemblyName = declaringType.Module.Assembly.Name.Name;

                    var functionName = attribute.ConstructorArguments.SingleOrDefault().Value.ToString();

                    if (string.IsNullOrEmpty(functionName))
                    {
                        continue;
                    }

                    function = new SdkFunctionMetadata
                    {
                        Name = functionName,
                        ScriptFile = $"bin/{assemblyName}.dll",
                        EntryPoint = $"{declaringType.GetReflectionFullName()}.{method.Name}",
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