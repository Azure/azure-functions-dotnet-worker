// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    internal sealed class DefaultFunctionMetadataManager : IFunctionMetadataManager
    {
        private readonly IFunctionMetadataProvider _functionMetadataProvider;
        private readonly ImmutableArray<IFunctionMetadataTransformer> _transformers;
        private readonly ILogger<DefaultFunctionMetadataManager> _logger;
        private readonly InputSchemaGenerator _inputSchemaGenerator;

        public DefaultFunctionMetadataManager(IFunctionMetadataProvider functionMetadataProvider,
            IEnumerable<IFunctionMetadataTransformer> transformers,
            ILogger<DefaultFunctionMetadataManager> logger)
        {
            _functionMetadataProvider = functionMetadataProvider;
            _transformers = transformers.ToImmutableArray();
            _logger = logger;
            _inputSchemaGenerator = new InputSchemaGenerator();
        }

        public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            ImmutableArray<IFunctionMetadata> functionMetadata = await _functionMetadataProvider.GetFunctionMetadataAsync(directory);

            // Apply input schema generation for MCP triggers before other transformers
            functionMetadata = ApplyInputSchemaGeneration(functionMetadata, directory);

            return ApplyTransforms(functionMetadata);
        }

        private ImmutableArray<IFunctionMetadata> ApplyInputSchemaGeneration(ImmutableArray<IFunctionMetadata> functionMetadata, string directory)
        {
            var result = functionMetadata.ToBuilder();

            for (int i = 0; i < result.Count; i++)
            {
                try
                {
                    var metadata = result[i];
                    if (metadata.RawBindings == null || metadata.RawBindings.Count == 0)
                    {
                        continue;
                    }

                    // Check if this function has an MCP tool trigger with useInputSchemaGeneration
                    if (ShouldGenerateInputSchema(metadata, out string? triggerParameterName))
                    {
                        _logger?.LogTrace("Generating input schema for function '{FunctionName}'.", metadata.Name);

                        string? inputSchema = null;

                        // First, try to generate schema from the trigger parameter type if it's a POCO
                        if (!string.IsNullOrEmpty(triggerParameterName) &&
                            TryGetParameterType(metadata, triggerParameterName, out Type? triggerParameterType) &&
                            triggerParameterType != null &&
                            !IsContextType(triggerParameterType) &&
                            IsPocoType(triggerParameterType))
                        {
                            _logger?.LogTrace("Generating input schema from POCO trigger parameter '{ParameterName}' of type '{TypeName}'.",
                                triggerParameterName, triggerParameterType.Name);

                            inputSchema = _inputSchemaGenerator.GenerateSchema(triggerParameterType);
                        }
                        // Otherwise, try to generate schema from mcpToolProperty bindings
                        else
                        {
                            _logger?.LogTrace("Generating input schema from McpToolProperty bindings for function '{FunctionName}'.",
                                metadata.Name);

                            inputSchema = GenerateSchemaFromPropertyBindings(metadata);
                        }

                        if (!string.IsNullOrEmpty(inputSchema))
                        {
                            // Update the binding with the generated schema
                            UpdateBindingWithInputSchema(metadata, inputSchema);

                            _logger?.LogTrace("Successfully generated input schema for function '{FunctionName}'.", metadata.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to generate input schema for function '{FunctionName}'.",
                        result[i].Name);
                }
            }

            return result.ToImmutable();
        }

        private bool ShouldGenerateInputSchema(IFunctionMetadata metadata, out string? triggerParameterName)
        {
            triggerParameterName = null;

            if (metadata.RawBindings == null)
            {
                return false;
            }

            // Look for an mcpToolTrigger binding with useInputSchemaGeneration
            foreach (var bindingJson in metadata.RawBindings)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(bindingJson);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("type", out var typeElement))
                    {
                        continue;
                    }

                    var bindingType = typeElement.GetString();
                    if (!string.Equals(bindingType, "mcpToolTrigger", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Check for useInputSchemaGeneration
                    bool hasInputSchemaGeneration = false;
                    if (root.TryGetProperty("useInputSchemaGeneration", out var enableSchemaElement))
                    {
                        hasInputSchemaGeneration = enableSchemaElement.ValueKind == System.Text.Json.JsonValueKind.True ||
                            (enableSchemaElement.ValueKind == System.Text.Json.JsonValueKind.String &&
                            bool.TryParse(enableSchemaElement.GetString(), out bool val) && val);
                    }

                    if (!hasInputSchemaGeneration)
                    {
                        continue;
                    }

                    // Get the trigger parameter name
                    if (root.TryGetProperty("name", out var nameElement))
                    {
                        triggerParameterName = nameElement.GetString();
                    }

                    return true;
                }
                catch (System.Text.Json.JsonException)
                {
                    // Ignore JSON parsing errors and continue
                    continue;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a JSON schema from mcpToolProperty bindings in the function metadata.
        /// </summary>
        private string GenerateSchemaFromPropertyBindings(IFunctionMetadata metadata)
        {
            var properties = new System.Text.Json.Nodes.JsonObject();
            var required = new System.Text.Json.Nodes.JsonArray();

            if (metadata.RawBindings == null)
            {
                return string.Empty;
            }

            // Collect all mcpToolProperty bindings
            foreach (var bindingJson in metadata.RawBindings)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(bindingJson);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("type", out var typeElement))
                    {
                        continue;
                    }

                    var bindingType = typeElement.GetString();
                    if (!string.Equals(bindingType, "mcpToolProperty", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get property name
                    if (!root.TryGetProperty("propertyName", out var propertyNameElement))
                    {
                        continue;
                    }

                    var propertyName = propertyNameElement.GetString();
                    if (string.IsNullOrEmpty(propertyName))
                    {
                        continue;
                    }

                    // Get the parameter name to find the type
                    if (!root.TryGetProperty("name", out var nameElement))
                    {
                        continue;
                    }

                    var parameterName = nameElement.GetString();
                    if (string.IsNullOrEmpty(parameterName))
                    {
                        continue;
                    }

                    // Get description if present
                    string? description = null;
                    if (root.TryGetProperty("description", out var descriptionElement))
                    {
                        description = descriptionElement.GetString();
                    }

                    // Try to get the parameter type
                    if (TryGetParameterType(metadata, parameterName, out Type? parameterType) && parameterType != null)
                    {
                        // Generate schema for this property
                        var propertySchema = _inputSchemaGenerator.GenerateSchema(parameterType);

                        // Parse the schema and add description if present
                        using var schemaDoc = System.Text.Json.JsonDocument.Parse(propertySchema);
                        var schemaNode = System.Text.Json.Nodes.JsonNode.Parse(propertySchema);

                        // Add description to the property schema
                        if (schemaNode is System.Text.Json.Nodes.JsonObject schemaObject && !string.IsNullOrEmpty(description))
                        {
                            schemaObject["description"] = description;
                        }

                        properties[propertyName] = schemaNode;

                        // Check if property is required
                        if (root.TryGetProperty("isRequired", out var isRequiredElement))
                        {
                            bool isRequired = isRequiredElement.ValueKind == System.Text.Json.JsonValueKind.True ||
                                (isRequiredElement.ValueKind == System.Text.Json.JsonValueKind.String &&
                                bool.TryParse(isRequiredElement.GetString(), out bool reqVal) && reqVal);

                            if (isRequired)
                            {
                                required.Add(propertyName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogTrace(ex, "Failed to process mcpToolProperty binding for function '{FunctionName}'.",
                        metadata.Name);
                }
            }

            // If no properties were found, return empty
            if (properties.Count == 0)
            {
                return string.Empty;
            }

            // Build the complete schema - always include required array even if empty
            var schema = new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            };

            return schema.ToJsonString();
        }

        /// <summary>
        /// Checks if a type is a POCO type suitable for schema generation.
        /// </summary>
        private bool IsPocoType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            // Exclude string and primitives
            if (type == typeof(string) || type.IsPrimitive)
            {
                return false;
            }

            // Exclude value types (structs, enums, etc.) unless they are nullable
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                return false;
            }

            // Must be a class
            if (!type.IsClass)
            {
                return false;
            }

            // Exclude abstract types and interfaces
            if (type.IsAbstract || type.IsInterface)
            {
                return false;
            }

            // Exclude types with generic parameters
            if (type.ContainsGenericParameters)
            {
                return false;
            }

            // Exclude collection types
            if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }

            // Check for public parameterless constructor
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a type is a context type (like ToolInvocationContext) that should not have schema generated.
        /// </summary>
        private bool IsContextType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            // Check by name to avoid referencing MCP-specific types
            if (type.Name == "ToolInvocationContext" || type.Name == "FunctionContext")
            {
                return true;
            }

            return false;
        }

        private bool TryGetParameterType(IFunctionMetadata metadata, string parameterName, out Type? parameterType)
        {
            parameterType = null;

            if (string.IsNullOrEmpty(metadata.EntryPoint))
            {
                return false;
            }

            // Parse entry point: "Namespace.ClassName.MethodName"
            var lastDotIndex = metadata.EntryPoint.LastIndexOf('.');
            if (lastDotIndex < 0)
            {
                return false;
            }

            var typeName = metadata.EntryPoint.Substring(0, lastDotIndex);
            var methodName = metadata.EntryPoint.Substring(lastDotIndex + 1);

            // Try to find the type in loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        var method = type.GetMethod(methodName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                        if (method != null)
                        {
                            var parameter = method.GetParameters()
                                .FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));

                            if (parameter != null)
                            {
                                parameterType = parameter.ParameterType;
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogTrace(ex, "Error while searching for type '{TypeName}' in assembly '{AssemblyName}'.",
                        typeName, assembly.FullName);
                }
            }

            return false;
        }

        private void UpdateBindingWithInputSchema(IFunctionMetadata metadata, string inputSchemaJson)
        {
            if (metadata.RawBindings == null)
            {
                return;
            }

            for (int i = 0; i < metadata.RawBindings.Count; i++)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(metadata.RawBindings[i]);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("type", out var typeElement))
                    {
                        continue;
                    }

                    var bindingType = typeElement.GetString();
                    if (!string.Equals(bindingType, "mcpToolTrigger", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Parse the binding as a mutable dictionary
                    var bindingDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(metadata.RawBindings[i]);
                    if (bindingDict == null)
                    {
                        continue;
                    }

                    // Add the inputSchema property
                    using var schemaDoc = System.Text.Json.JsonDocument.Parse(inputSchemaJson);
                    bindingDict["inputSchema"] = schemaDoc.RootElement.Clone();

                    // Remove toolProperties if it exists (mutual exclusivity)
                    if (bindingDict.ContainsKey("toolProperties"))
                    {
                        bindingDict.Remove("toolProperties");
                    }

                    // Serialize back to JSON
                    metadata.RawBindings[i] = System.Text.Json.JsonSerializer.Serialize(bindingDict);

                    break; // Only process the first mcpToolTrigger binding
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to update binding with input schema for function '{FunctionName}'.",
                        metadata.Name);
                }
            }
        }

        private ImmutableArray<IFunctionMetadata> ApplyTransforms(ImmutableArray<IFunctionMetadata> functionMetadata)
        {
            // Return early if there are no transformers to apply
            if (_transformers.Length == 0)
            {
                return functionMetadata;
            }

            var metadataResult = functionMetadata.ToBuilder();

            foreach (var transformer in _transformers)
            {
                try
                {
                    _logger?.LogTrace("Applying metadata transformer: {Transformer}.", transformer.Name);
                    transformer.Transform(metadataResult);
                }
                catch (Exception exc)
                {
                    _logger?.LogError(exc, "Metadata transformer '{Transformer}' failed.", transformer.Name);
                    throw;
                }
            }

            return metadataResult.ToImmutable();
        }
    }
}
