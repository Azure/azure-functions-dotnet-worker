// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        private const string BindingType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingAttribute";
        private const string OutputBindingType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.OutputBindingAttribute";
        private const string FunctionNameType = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
        private const string ExtensionsInformationType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.ExtensionInformationAttribute";
        private const string HttpResponseType = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";
        private const string TaskGenericType = "System.Threading.Tasks.Task`1";
        private const string TaskType = "System.Threading.Tasks.Task";
        private const string ReturnBindingName = "$return";

        private readonly IndentableLogger _logger;

        // TODO: Verify that we don't need to allow
        // same extensions of different versions. Picking the last version for now.
        // We can also just add all the versions of extensions and then let the
        // build pick the one it likes.
        private readonly IDictionary<string, string> _extensions;

        public FunctionMetadataGenerator()
            : this((l, m) => { })
        {
            _extensions = new Dictionary<string, string>();
        }

        public FunctionMetadataGenerator(Action<TraceLevel, string> log)
        {
            _logger = new IndentableLogger(log);
            _extensions = new Dictionary<string, string>();
        }

        public IDictionary<string, string> Extensions
        {
            get
            {
                return _extensions;
            }
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
                var functionsResult = GenerateFunctionMetadata(module,  type).ToArray();
                if (functionsResult.Any())
                {
                    _logger.LogMessage($"Found {functionsResult.Length} functions in '{type.GetReflectionFullName()}'.");
                }

                functions.AddRange(functionsResult);
            }

            return functions;
        }

        internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(ModuleDefinition module, TypeDefinition type)
        {
            var functions = new List<SdkFunctionMetadata>();

            foreach (MethodDefinition method in type.Methods)
            {
                AddFunctionMetadataIfFunction(module, functions, method);
            }

            return functions;
        }

        private void AddFunctionMetadataIfFunction(ModuleDefinition module, IList<SdkFunctionMetadata> functions, MethodDefinition method)
        {
            if (TryCreateFunctionMetadata(method, out SdkFunctionMetadata? metadata)
                && metadata != null)
            {
                var allBindings = CreateBindingMetadataAndAddExtensions(module, method);

                foreach(var binding in allBindings)
                {
                    metadata.Bindings.Add(binding);
                }

                functions.Add(metadata);
            }
        }

        private bool TryCreateFunctionMetadata(MethodDefinition method, out SdkFunctionMetadata? function)
        {
            function = null;

            foreach (CustomAttribute attribute in method.CustomAttributes)
            {
                if (string.Equals(attribute.AttributeType.FullName, FunctionNameType, StringComparison.Ordinal))
                {
                    string functionName = attribute.ConstructorArguments.SingleOrDefault().Value.ToString();

                    if (string.IsNullOrEmpty(functionName))
                    {
                        continue;
                    }

                    TypeDefinition declaringType = method.DeclaringType;

                    string actualMethodName = method.Name;
                    string declaryingTypeName = declaringType.GetReflectionFullName();
                    string assemblyName = declaringType.Module.Assembly.Name.Name;

                    function = CreateSdkFunctionMetadata(functionName, actualMethodName, declaryingTypeName, assemblyName);

                    return true;
                }
            }

            return false;
        }

        private SdkFunctionMetadata CreateSdkFunctionMetadata(string functionName, string actualMethodName, string declaringTypeName, string assemblyName)
        {
            var function = new SdkFunctionMetadata
            {
                Name = functionName,
                ScriptFile = $"{assemblyName}.dll",
                EntryPoint = $"{declaringTypeName}.{actualMethodName}",
                Language = "dotnet-isolated"
            };

            function.Properties["IsCodeless"] = false;

            return function;
        }

        private IEnumerable<ExpandoObject> CreateBindingMetadataAndAddExtensions(ModuleDefinition module, MethodDefinition method)
        {
            var bindingMetadata = new List<ExpandoObject>();

            AddInputTriggerBindingsAndExtensions(bindingMetadata, method);
            AddOutputBindingsAndExtensions(module, bindingMetadata, method);

            return bindingMetadata;
        }

        private void AddOutputBindingsAndExtensions(ModuleDefinition module, IList<ExpandoObject> bindingMetadata, MethodDefinition method)
        {
            if (!TryAddOutputBindingFromMethod(bindingMetadata, method))
            {
                AddOutputBindingsFromReturnType(module, bindingMetadata, method);
            }
        }

        private void AddOutputBindingsFromReturnType(ModuleDefinition module, IList<ExpandoObject> bindingMetadata, MethodDefinition method)
        {
            string? returnType = GetTaskElementType(method.ReturnType);

            if (returnType is not null)
            {
                if (string.Equals(returnType, HttpResponseType, StringComparison.Ordinal))
                {
                    AddHttpOutputBinding(bindingMetadata, ReturnBindingName);
                }
                else
                {
                    TypeDefinition returnDefinition = module.GetType(returnType)
                        ?? throw new InvalidOperationException($"Couldn't find the type definition {returnType}");

                    AddOutputBindingsFromProperties(bindingMetadata, returnDefinition);
                }
            }
        }

        private void AddOutputBindingsFromProperties(IList<ExpandoObject> bindingMetadata, TypeDefinition typeDefinition)
        {
            bool foundHttpOutput = false;

            foreach (PropertyDefinition property in typeDefinition.Properties)
            {
                if (string.Equals(property.PropertyType.FullName, HttpResponseType, StringComparison.Ordinal))
                {
                    if (foundHttpOutput)
                    {
                        throw new InvalidOperationException($"Found multiple public properties with type '{HttpResponseType}' defined in output type '{typeDefinition.FullName}'. " +
                            $"Only one HTTP response binding type is supported in your return type definition.");
                    }

                    foundHttpOutput = true;
                    AddHttpOutputBinding(bindingMetadata, property.Name);
                    continue;
                }

                AddOutputBindingFromProperty(bindingMetadata, property, typeDefinition.FullName);
            }
        }

        private void AddOutputBindingFromProperty(IList<ExpandoObject> bindingMetadata, PropertyDefinition property, string typeName)
        {
            bool foundOutputAttribute = false;

            foreach (CustomAttribute propertyAttribute in property.CustomAttributes)
            {
                if (IsOutputBindingType(propertyAttribute))
                {
                    if (foundOutputAttribute)
                    {
                        throw new InvalidOperationException($"Found multiple output attributes on property '{property.Name}' defined in the function return type '{typeName}'. " +
                            $"Only one output binding attribute is is supported on a property.");
                    }

                    foundOutputAttribute = true;

                    AddOutputBindingMetadata(bindingMetadata, propertyAttribute, property.Name);
                    AddExtensionInfo(_extensions, propertyAttribute);
                }
            }
        }

        private bool TryAddOutputBindingFromMethod(IList<ExpandoObject> bindingMetadata, MethodDefinition method)
        {
            bool foundBinding = false;

            foreach (CustomAttribute methodAttribute in method.CustomAttributes)
            {
                if (IsOutputBindingType(methodAttribute))
                {
                    if (foundBinding)
                    {
                        throw new Exception($"Found multiple Output bindings on method '{method.FullName}'. " +
                            $"Please use an encapsulation to define the bindings in properties. For more information: https://aka.ms/dotnet-worker-poco-binding.");
                    }

                    AddOutputBindingMetadata(bindingMetadata, methodAttribute, ReturnBindingName);
                    AddExtensionInfo(_extensions, methodAttribute);

                    foundBinding = true;
                }
            }

            return foundBinding;
        }

        private void AddInputTriggerBindingsAndExtensions(IList<ExpandoObject> bindingMetadata, MethodDefinition method)
        {
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                foreach (CustomAttribute parameterAttribute in parameter.CustomAttributes)
                {
                    if (IsFunctionBindingType(parameterAttribute))
                    {
                        AddBindingMetadata(bindingMetadata, parameterAttribute, parameter.Name);
                        AddExtensionInfo(_extensions, parameterAttribute);
                    }
                }
            }
        }

        private static string? GetTaskElementType(TypeReference typeReference)
        {
            if (typeReference is null || typeReference.FullName == TaskType)
            {
                return null;
            }

            if (typeReference.IsGenericInstance
                && typeReference is GenericInstanceType genericType 
                && string.Equals(typeReference.GetElementType().FullName, TaskGenericType, StringComparison.Ordinal))
            {
                // T from Task<T>
                return genericType.GenericArguments[0].FullName;
            }
            else
            {
                return typeReference.FullName;
            }
        }

        private static void AddOutputBindingMetadata(IList<ExpandoObject> bindingMetadata, CustomAttribute attribute, string? name = null)
        {
            AddBindingMetadata(bindingMetadata, attribute, parameterName: name);
        }

        private static void AddBindingMetadata(IList<ExpandoObject> bindingMetadata, CustomAttribute attribute, string? parameterName)
        {
            string bindingType = GetBindingType(attribute);

            ExpandoObject binding = BuildBindingMetadataFromAttribute(attribute, bindingType, parameterName);
            bindingMetadata.Add(binding);
        }

        private static ExpandoObject BuildBindingMetadataFromAttribute(CustomAttribute attribute, string bindingType, string? parameterName)
        {
            ExpandoObject binding = new ExpandoObject();

            var bindingDict = (IDictionary<string, object>)binding;

            if (!string.IsNullOrEmpty(parameterName))
            {
                bindingDict["Name"] = parameterName!;
            }

            bindingDict["Type"] = bindingType;
            bindingDict["Direction"] = GetBindingDirection(attribute);

            foreach (var property in attribute.GetAllDefinedProperties())
            {
                bindingDict.Add(property.Key, property.Value);
            }

            return binding;
        }

        private static string GetBindingType(CustomAttribute attribute)
        {
            var attributeType = attribute.AttributeType.Name;

            // TODO: Should "webjob type" be a property of the "worker types" and come from there?
            return attributeType
                    .Replace("TriggerAttribute", "Trigger")
                    .Replace("InputAttribute", string.Empty)
                    .Replace("OutputAttribute", string.Empty);
        }

        private static void AddHttpOutputBinding(IList<ExpandoObject> bindingMetadata, string name)
        {
            IDictionary<string, object> returnBinding = new ExpandoObject();
            returnBinding["Name"] = name;
            returnBinding["Type"] = "http";
            returnBinding["Direction"] = "Out";

            bindingMetadata.Add((ExpandoObject)returnBinding);
        }

        private static void AddExtensionInfo(IDictionary<string, string> extensions, CustomAttribute attribute)
        {
            AssemblyDefinition extensionAssemblyDefintion = attribute.AttributeType.Resolve().Module.Assembly;

            foreach (var assemblyAttribute in extensionAssemblyDefintion.CustomAttributes)
            {
                if (string.Equals(assemblyAttribute.AttributeType.FullName, ExtensionsInformationType, StringComparison.Ordinal))
                {
                    string extensionName = assemblyAttribute.ConstructorArguments[0].Value.ToString();
                    string extensionVersion = assemblyAttribute.ConstructorArguments[1].Value.ToString();

                    extensions[extensionName] = extensionVersion;

                    // Only 1 extension per library
                    return;
                }
            }
        }

        private static string GetBindingDirection(CustomAttribute attribute)
        {
            if (IsOutputBindingType(attribute))
            {
                return "Out";
            }

            return "In";
        }

        private static bool IsOutputBindingType(CustomAttribute attribute)
        {
            return TryGetBaseAttributeType(attribute, OutputBindingType, out _);
        }

        private static bool IsFunctionBindingType(CustomAttribute attribute)
        {
            return TryGetBaseAttributeType(attribute, BindingType, out _);
        }

        private static bool TryGetBaseAttributeType(CustomAttribute attribute, string baseType, out TypeReference? baseTypeRef)
        {
            baseTypeRef = attribute.AttributeType?.Resolve()?.BaseType;

            while (baseTypeRef != null)
            {
                if (string.Equals(baseTypeRef.FullName, baseType, StringComparison.Ordinal))
                {
                    return true;
                }

                baseTypeRef = baseTypeRef.Resolve().BaseType;
            }

            return false;
        }
    }
}
