// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Generates JSON schema for input parameters based on complex types.
    /// </summary>
    internal class InputSchemaGenerator
    {
        private readonly HashSet<Type> _processedTypes = new HashSet<Type>();
        private readonly JsonSerializerOptions _jsonOptions;

        public InputSchemaGenerator()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Generates a JSON schema string from a CLR type by analyzing its properties.
        /// This generates a schema for a POCO type used as a trigger parameter.
        /// </summary>
        /// <param name="type">The type to generate schema for.</param>
        /// <returns>JSON schema as a string.</returns>
        public string GenerateSchema(Type type)
        {
            _processedTypes.Clear();

            // For POCO types, generate an object schema with properties
            if (IsPocoType(type))
            {
                var schemaObject = GenerateObjectSchemaFromType(type);
                return JsonSerializer.Serialize(schemaObject, _jsonOptions);
            }

            // For non-POCO types, generate a basic schema
            var basicSchema = GenerateSchemaObject(type);
            return JsonSerializer.Serialize(basicSchema, _jsonOptions);
        }

        /// <summary>
        /// Checks if a type is a POCO type suitable for schema generation.
        /// </summary>
        private bool IsPocoType(Type type)
        {
            if (type == typeof(string) || !type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                return false;
            }

            // Check if it's a collection type
            if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }

            // Check for public parameterless constructor
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>
        /// Generates an object schema from a POCO type by analyzing its public properties.
        /// </summary>
        private JsonObject GenerateObjectSchemaFromType(Type type)
        {
            var schemaObj = new JsonObject
            {
                ["type"] = "object"
            };

            var properties = new JsonObject();
            var required = new JsonArray();

            // Get all public instance properties that can be read and written
            var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in publicProperties)
            {
                var propertyName = GetPropertyName(property);

                // Generate schema for the property type
                var propertySchema = GenerateSchemaObject(property.PropertyType);
                properties[propertyName] = propertySchema;

                // Check if property is required
                if (IsPropertyRequired(property))
                {
                    required.Add(propertyName);
                }
            }

            if (properties.Count > 0)
            {
                schemaObj["properties"] = properties;
            }

            // Always include required array, even if empty
            if (required.Count >= 0)
            {
                schemaObj["required"] = required;
            }

            return schemaObj;
        }

        /// <summary>
        /// Generates a schema object (as JsonObject) from a CLR type.
        /// </summary>
        private JsonObject GenerateSchemaObject(Type type)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Detect circular references
            if (_processedTypes.Contains(underlyingType))
            {
                throw new InvalidOperationException($"Circular reference detected for type '{underlyingType.Name}'.");
            }

            _processedTypes.Add(underlyingType);

            try
            {
                var schemaObj = new JsonObject();

                // Handle primitive types
                if (IsPrimitiveType(underlyingType))
                {
                    schemaObj["type"] = GetJsonSchemaType(underlyingType);
                    return schemaObj;
                }

                // Handle arrays and collections
                if (IsArrayOrCollection(underlyingType, out Type? elementType) && elementType != null)
                {
                    schemaObj["type"] = "array";
                    schemaObj["items"] = GenerateSchemaObject(elementType);
                    return schemaObj;
                }

                // Handle enums
                if (underlyingType.IsEnum)
                {
                    schemaObj["type"] = "string";
                    var enumValues = new JsonArray();
                    foreach (var value in Enum.GetNames(underlyingType))
                    {
                        enumValues.Add(value);
                    }
                    schemaObj["enum"] = enumValues;
                    return schemaObj;
                }

                // Handle complex objects (nested POCOs)
                if (underlyingType.IsClass || (underlyingType.IsValueType && !underlyingType.IsPrimitive))
                {
                    return GenerateObjectSchemaFromType(underlyingType);
                }

                // Default fallback
                schemaObj["type"] = "object";
                return schemaObj;
            }
            finally
            {
                _processedTypes.Remove(underlyingType);
            }
        }

        /// <summary>
        /// Gets the property name, respecting JSON property name attributes if present.
        /// </summary>
        private string GetPropertyName(PropertyInfo property)
        {
            // Check for JsonPropertyName attribute
            var jsonPropertyAttr = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
            if (jsonPropertyAttr != null)
            {
                return jsonPropertyAttr.Name;
            }

            // Default to camelCase
            var name = property.Name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Determines if a property is required based on its type and attributes.
        /// </summary>
        private bool IsPropertyRequired(PropertyInfo property)
        {
            // Check for Required attribute
            var requiredAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();
            if (requiredAttr != null)
            {
                return true;
            }

            // Check if type is nullable
            var propertyType = property.PropertyType;
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                return false;
            }

            // Reference types are nullable by default in older C#, so we consider them optional
            // unless marked with Required attribute
            if (!propertyType.IsValueType)
            {
                return false;
            }

            // Value types (int, bool, etc.) are required by default
            return true;
        }

        /// <summary>
        /// Checks if a type is a primitive type for JSON schema purposes.
        /// </summary>
        private bool IsPrimitiveType(Type type)
        {
            return type == typeof(string) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(short) ||
                type == typeof(byte) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(ushort) ||
                type == typeof(sbyte) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(decimal) ||
                type == typeof(bool) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(Guid) ||
                type == typeof(TimeSpan) ||
                type == typeof(char);
        }

        /// <summary>
        /// Gets the JSON schema type string for a CLR type.
        /// </summary>
        private string GetJsonSchemaType(Type type)
        {
            if (type == typeof(string) || type == typeof(char))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                type == typeof(ushort) || type == typeof(sbyte))
                return "integer";
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(Guid) || type == typeof(TimeSpan))
                return "string";

            return "string"; // Default fallback
        }

        /// <summary>
        /// Checks if a type is an array or collection and returns the element type.
        /// </summary>
        private bool IsArrayOrCollection(Type type, out Type? elementType)
        {
            elementType = null;

            // Handle arrays
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            // Handle generic collections (List<T>, IEnumerable<T>, etc.)
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(IEnumerable<>) ||
                    genericTypeDefinition == typeof(ICollection<>) ||
                    genericTypeDefinition == typeof(IList<>) ||
                    genericTypeDefinition == typeof(List<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            // Handle interfaces that implement IEnumerable<T>
            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
            {
                elementType = enumerableInterface.GetGenericArguments()[0];
                return true;
            }

            return false;
        }
    }
}
