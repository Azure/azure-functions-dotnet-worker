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
        private readonly IndentableLogger _logger;

        // TODO: Verify that we don't need to allow
        // same extensions of different versions. Picking the last version for now.
        // We can also just add all the versions of extensions and then let the
        // build pick the one it likes.
        private readonly IDictionary<string, string> _extensions;

        public FunctionMetadataGenerator()
            : this((l, m, p) => { })
        {
            _extensions = new Dictionary<string, string>();
        }

        public FunctionMetadataGenerator(Action<TraceLevel, string, string> log)
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

                            var moduleDefinition = ModuleDefinition.ReadModule(path, readerParams);

                            functions.AddRange(GenerateFunctionMetadata(moduleDefinition));
                        }
                        catch (BadImageFormatException)
                        {
                            _logger.LogMessage($"Skipping file '{Path.GetFileName(path)}' because of a {nameof(BadImageFormatException)}.");
                        }
                        catch (FunctionsMetadataGenerationException ex)
                        {
                            _logger.LogError($"Failed to generate function metadata from {Path.GetFileName(path)}: {ex.Message}", path);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Could not evaluate '{Path.GetFileName(path)}' for functions metadata. Exception message: {ex}");
                        }
                    }
                }
            }

            return functions;
        }

        internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(ModuleDefinition module)
        {
            var functions = new List<SdkFunctionMetadata>();
            bool moduleExtensionRegistered = false;

            foreach (TypeDefinition type in module.Types)
            {
                var functionsResult = GenerateFunctionMetadata(type).ToArray();
                if (functionsResult.Any())
                {
                    moduleExtensionRegistered = true;
                    _logger.LogMessage($"Found {functionsResult.Length} functions in '{type.GetReflectionFullName()}'.");
                }

                functions.AddRange(functionsResult);
            }

            if (!moduleExtensionRegistered && TryAddExtensionInfo(_extensions, module.Assembly, usedByFunction: false))
            {
                _logger.LogMessage($"Implicitly registered {module.FileName} as an extension.");
            }

            return functions;
        }

        internal IEnumerable<SdkFunctionMetadata> GenerateFunctionMetadata(TypeDefinition type)
        {
            var functions = new List<SdkFunctionMetadata>();

            foreach (MethodDefinition method in type.Methods)
            {
                AddFunctionMetadataIfFunction(functions, method);
            }

            return functions;
        }

        private void AddFunctionMetadataIfFunction(IList<SdkFunctionMetadata> functions, MethodDefinition method)
        {
            if (TryCreateFunctionMetadata(method, out SdkFunctionMetadata? metadata)
                && metadata != null)
            {
                try
                {

                    var allBindings = CreateBindingMetadataAndAddExtensions(method);


                    foreach (var binding in allBindings)
                    {
                        metadata.Bindings.Add(binding);
                    }

                    functions.Add(metadata);
                }
                catch (FunctionsMetadataGenerationException ex)
                {
                    throw new FunctionsMetadataGenerationException($"Failed to generate medata for function '{metadata.Name}' (method '{method.FullName}'): {ex.Message}");
                }
            }
        }

        private bool TryCreateFunctionMetadata(MethodDefinition method, out SdkFunctionMetadata? function)
        {
            function = null;

            foreach (CustomAttribute attribute in method.CustomAttributes)
            {
                if (string.Equals(attribute.AttributeType.FullName, Constants.FunctionNameType, StringComparison.Ordinal))
                {
                    string functionName = attribute.ConstructorArguments.SingleOrDefault().Value.ToString();

                    if (string.IsNullOrEmpty(functionName))
                    {
                        continue;
                    }

                    TypeDefinition declaringType = method.DeclaringType;

                    string actualMethodName = method.Name;
                    string declaringTypeName = declaringType.GetReflectionFullName();
                    string assemblyName = declaringType.Module.Assembly.Name.Name;

                    function = CreateSdkFunctionMetadata(functionName, actualMethodName, declaringTypeName, assemblyName);

                    return true;
                }
            }

            return false;
        }

        private static SdkFunctionMetadata CreateSdkFunctionMetadata(string functionName, string actualMethodName, string declaringTypeName, string assemblyName)
        {
            var function = new SdkFunctionMetadata
            {
                Name = functionName,
                ScriptFile = $"{assemblyName}.dll",
                EntryPoint = $"{declaringTypeName}.{actualMethodName}",
                Language = "dotnet-isolated",
                Properties =
                {
                    ["IsCodeless"] = false
                }
            };


            return function;
        }

        private IEnumerable<ExpandoObject> CreateBindingMetadataAndAddExtensions(MethodDefinition method)
        {
            var bindingMetadata = new List<ExpandoObject>();

            AddInputTriggerBindingsAndExtensions(bindingMetadata, method);
            AddOutputBindingsAndExtensions(bindingMetadata, method);

            return bindingMetadata;
        }

        private void AddOutputBindingsAndExtensions(IList<ExpandoObject> bindingMetadata, MethodDefinition method)
        {
            if (!TryAddOutputBindingFromMethod(bindingMetadata, method))
            {
                AddOutputBindingsFromReturnType(bindingMetadata, method);
            }
        }

        private void AddOutputBindingsFromReturnType(IList<ExpandoObject> bindingMetadata, MethodDefinition method)
        {
            TypeReference? returnType = GetTaskElementType(method.ReturnType);

            if (returnType is not null && !string.Equals(returnType.FullName, Constants.VoidType, StringComparison.Ordinal))
            {
                if (string.Equals(returnType.FullName, Constants.HttpResponseType, StringComparison.Ordinal))
                {
                    AddHttpOutputBinding(bindingMetadata, Constants.ReturnBindingName);
                }
                else
                {
                    TypeDefinition returnDefinition = returnType.Resolve()
                        ?? throw new FunctionsMetadataGenerationException($"Couldn't find the type definition '{returnType}' for method '{method.FullName}'");

                    bool hasOutputModel = TryAddOutputBindingsFromProperties(bindingMetadata, returnDefinition);

                    // Special handling for HTTP results using POCOs/Types other 
                    // than HttpResponseData. We should improve this to expand this 
                    // support to other triggers without special handling
                    if (!hasOutputModel && bindingMetadata.Any(d => IsHttpTrigger(d)))
                    {
                        AddHttpOutputBinding(bindingMetadata, Constants.ReturnBindingName);
                    }
                }
            }
        }

        private static bool IsHttpTrigger(ExpandoObject bindingMetadata)
        {
            return bindingMetadata.Any(kvp => string.Equals(kvp.Key, "Type", StringComparison.Ordinal)
                && string.Equals(kvp.Value?.ToString(), Constants.HttpTriggerBindingType, StringComparison.Ordinal));
        }

        private bool TryAddOutputBindingsFromProperties(IList<ExpandoObject> bindingMetadata, TypeDefinition typeDefinition)
        {
            bool foundHttpOutput = false;
            int beforeCount = bindingMetadata.Count;

            foreach (PropertyDefinition property in typeDefinition.Properties)
            {
                if (string.Equals(property.PropertyType.FullName, Constants.HttpResponseType, StringComparison.Ordinal))
                {
                    if (foundHttpOutput)
                    {
                        throw new FunctionsMetadataGenerationException($"Found multiple public properties with type '{Constants.HttpResponseType}' defined in output type '{typeDefinition.FullName}'. " +
                            $"Only one HTTP response binding type is supported in your return type definition.");
                    }

                    foundHttpOutput = true;
                    AddHttpOutputBinding(bindingMetadata, property.Name);
                    continue;
                }

                AddOutputBindingFromProperty(bindingMetadata, property, typeDefinition.FullName);
            }

            return bindingMetadata.Count > beforeCount;
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
                        throw new FunctionsMetadataGenerationException($"Found multiple output attributes on property '{property.Name}' defined in the function return type '{typeName}'. " +
                            $"Only one output binding attribute is is supported on a property.");
                    }

                    foundOutputAttribute = true;

                    AddOutputBindingMetadata(bindingMetadata, propertyAttribute, property.PropertyType, property.Name);
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
                        throw new FunctionsMetadataGenerationException($"Found multiple Output bindings on method '{method.FullName}'. " +
                            "Please use an encapsulation to define the bindings in properties. For more information: https://aka.ms/dotnet-worker-poco-binding.");
                    }

                    AddOutputBindingMetadata(bindingMetadata, methodAttribute, methodAttribute.AttributeType, Constants.ReturnBindingName);
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
                        AddBindingMetadata(bindingMetadata, parameterAttribute, parameter.ParameterType, parameter.Name);
                        AddExtensionInfo(_extensions, parameterAttribute);
                    }
                }
            }
        }

        private static TypeReference? GetTaskElementType(TypeReference typeReference)
        {
            if (typeReference is null || string.Equals(typeReference.FullName, Constants.TaskType, StringComparison.Ordinal))
            {
                return null;
            }

            if (typeReference.IsGenericInstance
                && typeReference is GenericInstanceType genericType
                && string.Equals(typeReference.GetElementType().FullName, Constants.TaskGenericType, StringComparison.Ordinal))
            {
                // T from Task<T>
                return genericType.GenericArguments[0];
            }
            else
            {
                return typeReference;
            }
        }

        private static void AddOutputBindingMetadata(IList<ExpandoObject> bindingMetadata, CustomAttribute attribute, TypeReference parameterType, string? name = null)
        {
            AddBindingMetadata(bindingMetadata, attribute, parameterType, parameterName: name);
        }

        private static void AddBindingMetadata(IList<ExpandoObject> bindingMetadata, CustomAttribute attribute, TypeReference parameterType, string? parameterName)
        {
            string bindingType = GetBindingType(attribute);

            ExpandoObject binding = BuildBindingMetadataFromAttribute(attribute, bindingType, parameterType, parameterName);
            bindingMetadata.Add(binding);
        }

        private static ExpandoObject BuildBindingMetadataFromAttribute(CustomAttribute attribute, string bindingType, TypeReference parameterType, string? parameterName)
        {
            ExpandoObject binding = new ExpandoObject();

            var bindingDict = (IDictionary<string, object>)binding;

            if (!string.IsNullOrEmpty(parameterName))
            {
                bindingDict["Name"] = parameterName!;
            }

            bindingDict["Type"] = bindingType;
            bindingDict["Direction"] = GetBindingDirection(attribute);

            // Is string parameter type
            if (IsStringType(parameterType.FullName))
            {
                bindingDict["DataType"] = "String";
            }
            // Is binary parameter type
            else if (IsBinaryType(parameterType.FullName))
            {
                bindingDict["DataType"] = "Binary";
            }

            foreach (var property in attribute.GetAllDefinedProperties())
            {
                bindingDict.Add(property.Key, property.Value);
            }

            // Determine if we should set the "Cardinality" property based on
            // the presence of "IsBatched." This is a property that is from the
            // attributes that implement the ISupportCardinality interface.
            //
            // Note that we are directly looking for "IsBatched" today while we 
            // are not actually instantiating the Attribute type and instead relying
            // on type inspection via Mono.Cecil.
            // TODO: Do not hard-code "IsBatched" as the property to set cardinality.
            // We should rely on the interface
            // 
            // Conversion rule
            //     "IsBatched": true => "Cardinality": "Many"
            //     "IsBatched": false => "Cardinality": "One"
            if (bindingDict.TryGetValue(Constants.IsBatchedKey, out object isBatchedValue)
                && isBatchedValue is bool isBatched)
            {
                // Batching set to true
                if (isBatched)
                {
                    bindingDict["Cardinality"] = "Many";
                    // Throw if parameter type is *definitely* not a collection type.
                    // Note that this logic doesn't dictate what we can/can't do, and
                    // we can be more restrictive in the future because today some 
                    // scenarios result in runtime failures.
                    if (IsIterableCollection(parameterType, out DataType dataType))
                    {
                        if (dataType.Equals(DataType.String))
                        {
                            bindingDict["DataType"] = "String";
                        }
                        else if (dataType.Equals(DataType.Binary))
                        {
                            bindingDict["DataType"] = "Binary";
                        }
                    }
                    else
                    {
                        throw new FunctionsMetadataGenerationException("Function is configured to process events in batches but parameter type is not iterable. " +
                            $"Change parameter named '{ parameterName }' to be an IEnumerable type or set 'IsBatched' to false on your '{attribute.AttributeType.Name.Replace("Attribute", "")}' attribute.");
                    }
                }
                // Batching set to false
                else
                {
                    bindingDict["Cardinality"] = "One";
                }

                bindingDict.Remove(Constants.IsBatchedKey);
            }

            return binding;
        }

        private static bool IsIterableCollection(TypeReference type, out DataType dataType)
        {
            // Array and not byte array 
            bool isArray = type.IsArray && !string.Equals(type.FullName, Constants.ByteArrayType, StringComparison.Ordinal);
            if (isArray)
            {
                if (type is TypeSpecification typeSpecification)
                {
                    dataType = GetDataTypeFromType(typeSpecification.ElementType.FullName);
                    return true;
                }
            }

            bool isMappingEnumerable = IsOrDerivedFrom(type, Constants.IEnumerableOfKeyValuePair)
                || IsOrDerivedFrom(type, Constants.LookupGenericType)
                || IsOrDerivedFrom(type, Constants.DictionaryGenericType);
            if (isMappingEnumerable)
            {
                dataType = DataType.Undefined;
                return false;
            }

            // IEnumerable and not string or dictionary
            bool isEnumerableOfT = IsOrDerivedFrom(type, Constants.IEnumerableOfT);
            bool isEnumerableCollection =
                !IsStringType(type.FullName)
                && (IsOrDerivedFrom(type, Constants.IEnumerableType)
                    || IsOrDerivedFrom(type, Constants.IEnumerableGenericType)
                    || isEnumerableOfT);
            if (isEnumerableCollection)
            {
                dataType = DataType.Undefined;
                if (IsOrDerivedFrom(type, Constants.IEnumerableOfStringType))
                {
                    dataType = DataType.String;
                }
                else if (IsOrDerivedFrom(type, Constants.IEnumerableOfBinaryType))
                {
                    dataType = DataType.Binary;
                }
                else if (isEnumerableOfT)
                {
                    // Find real type that "T" in IEnumerable<T> resolves to
                    string typeName = ResolveIEnumerableOfTType(type, new Dictionary<string, string>()) ?? string.Empty;
                    dataType = GetDataTypeFromType(typeName);
                }
                return true;
            }

            dataType = DataType.Undefined;
            return false;
        }

        private static bool IsOrDerivedFrom(TypeReference type, string interfaceFullName)
        {
            bool isType = string.Equals(type.FullName, interfaceFullName, StringComparison.Ordinal);
            TypeDefinition definition = type.Resolve();
            return isType || IsDerivedFrom(definition, interfaceFullName);
        }

        private static bool IsDerivedFrom(TypeDefinition definition, string interfaceFullName)
        {
            var isType = string.Equals(definition.FullName, interfaceFullName, StringComparison.Ordinal);
            return isType || HasInterface(definition, interfaceFullName) || IsSubclassOf(definition, interfaceFullName);
        }

        private static bool HasInterface(TypeDefinition definition, string interfaceFullName)
        {
            return definition.Interfaces.Any(i => string.Equals(i.InterfaceType.FullName, interfaceFullName, StringComparison.Ordinal));
        }

        private static bool IsSubclassOf(TypeDefinition definition, string interfaceFullName)
        {
            if (definition.BaseType is null)
            {
                return false;
            }

            TypeDefinition baseType = definition.BaseType.Resolve();
            return IsDerivedFrom(baseType, interfaceFullName);
        }

        private static string? ResolveIEnumerableOfTType(TypeReference type, Dictionary<string, string> foundMapping)
        {
            // Base case: 
            // We are at IEnumerable<T> and want to return the most recent resolution of T
            // (Most recent is relative to IEnumerable<T>)
            if (string.Equals(type.FullName, Constants.IEnumerableOfT, StringComparison.Ordinal))
            {
                if (foundMapping.TryGetValue(Constants.GenericIEnumerableArgumentName, out string typeName))
                {
                    return typeName;
                }

                return null;
            }

            TypeDefinition definition = type.Resolve();
            if (definition.HasGenericParameters && type is GenericInstanceType genericType)
            {
                for (int i = 0; i < genericType.GenericArguments.Count(); i++)
                {
                    string name = genericType.GenericArguments.ElementAt(i).FullName;
                    string resolvedName = definition.GenericParameters.ElementAt(i).FullName;

                    if (foundMapping.TryGetValue(name, out string firstType))
                    {
                        foundMapping.Remove(name);
                        foundMapping.Add(resolvedName, firstType);
                    }
                    else
                    {
                        foundMapping.Add(resolvedName, name);
                    }
                }

            }

            return definition.Interfaces
                       .Select(i => ResolveIEnumerableOfTType(i.InterfaceType, foundMapping))
                       .FirstOrDefault(name => name is not null)
                   ?? ResolveIEnumerableOfTType(definition.BaseType, foundMapping);
        }

        private static DataType GetDataTypeFromType(string fullName)
        {
            if (IsStringType(fullName))
            {
                return DataType.String;
            }
            else if (IsBinaryType(fullName))
            {
                return DataType.Binary;
            }

            return DataType.Undefined;
        }

        private static bool IsStringType(string fullName)
        {
            return string.Equals(fullName, Constants.StringType, StringComparison.Ordinal);
        }

        private static bool IsBinaryType(string fullName)
        {
            return string.Equals(fullName, Constants.ByteArrayType, StringComparison.Ordinal)
                || string.Equals(fullName, Constants.ReadOnlyMemoryOfBytes, StringComparison.Ordinal);
        }

        private static string GetBindingType(CustomAttribute attribute)
        {
            var attributeType = attribute.AttributeType.Name;

            // TODO: Should "webjob type" be a property of the "worker types" and come from there?
            var bindingType = attributeType
                                .Replace("TriggerAttribute", "Trigger")
                                .Replace("InputAttribute", string.Empty)
                                .Replace("OutputAttribute", string.Empty)
                                .Replace("Attribute", string.Empty);

            // The first character of "Type" property value must be lower case for the scaling infrastructure to work correctly
            bindingType = bindingType.ToLowerFirstCharacter();

            return bindingType;
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
            AssemblyDefinition extensionAssemblyDefinition = attribute.AttributeType.Resolve().Module.Assembly;
            TryAddExtensionInfo(extensions, extensionAssemblyDefinition);
        }

        private static bool TryAddExtensionInfo(IDictionary<string, string> extensions, AssemblyDefinition extensionAssemblyDefinition, bool usedByFunction = true)
        {
            foreach (var assemblyAttribute in extensionAssemblyDefinition.CustomAttributes)
            {
                if (string.Equals(assemblyAttribute.AttributeType.FullName, Constants.ExtensionsInformationType, StringComparison.Ordinal))
                {
                    string extensionName = assemblyAttribute.ConstructorArguments[0].Value.ToString();
                    string extensionVersion = assemblyAttribute.ConstructorArguments[1].Value.ToString();
                    bool implicitlyRegister = false;

                    if (assemblyAttribute.ConstructorArguments.Count >= 3)
                    {
                        // EnableImplicitRegistration
                        implicitlyRegister = (bool)assemblyAttribute.ConstructorArguments[2].Value;
                    }

                    if (usedByFunction || implicitlyRegister)
                    {
                        extensions[extensionName] = extensionVersion;
                    }

                    // Only 1 extension per library
                    return true;
                }
            }

            return false;
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
            return TryGetBaseAttributeType(attribute, Constants.OutputBindingType, out _);
        }

        private static bool IsFunctionBindingType(CustomAttribute attribute)
        {
            return TryGetBaseAttributeType(attribute, Constants.BindingType, out _);
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
