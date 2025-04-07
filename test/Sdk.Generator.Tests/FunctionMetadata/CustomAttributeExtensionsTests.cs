// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Sdk;
using Mono.Cecil;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public class CustomAttributeExtensionsTests
    {
        [Fact]
        public void GetAllDefinedProperties_GetsJustConstructorParams()
        {
            ModuleDefinition module = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            MethodDefinition method = GetMethod(module, "Use_JustConstructor");
            CustomAttribute attribute = GetAttribute(method, "JustConstructorAttribute");

            IDictionary<string, object> props = attribute.GetAllDefinedProperties();
            IDictionary<string, object> expected = new Dictionary<string, object>
            {
                { "name", "Someone1" },
                { "number", 25 },
                { "ch", 'y' },
            };

            AssertDictionary(expected, props);
        }

        [Fact]
        public void GetAllDefinedProperties_GetsConstructorParamsAndProperties_Overridden()
        {
            ModuleDefinition module = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            MethodDefinition method = GetMethod(module, "Use_ConstructorAndProperties");
            CustomAttribute attribute = GetAttribute(method, "ConstructorAndPropertiesAttribute");

            IDictionary<string, object> props = attribute.GetAllDefinedProperties();
            IDictionary<string, object> expected = new Dictionary<string, object>
            {
                { "name", "Someone2" },
                { "number", 26 },
                { "ch", 'n' },
                { "Value", "Overridden1" }
            };

            AssertDictionary(expected, props);
        }

        [Fact]
        public void GetAllDefinedProperties_GetsConstructorParamsAndProperties_Default_Ignored()
        {
            ModuleDefinition module = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            MethodDefinition method = GetMethod(module, "Use_ConstructorAndProperties_DefaultProperty");
            CustomAttribute attribute = GetAttribute(method, "ConstructorAndPropertiesAttribute");

            IDictionary<string, object> props = attribute.GetAllDefinedProperties();
            IDictionary<string, object> expected = new Dictionary<string, object>
            {
                { "name", "Someone2" },
                { "number", 26 },
                { "ch", 'n' }
            };

            AssertDictionary(expected, props);
        }

        [Fact]
        public void GetAllDefinedProperties_GetsProperties()
        {
            ModuleDefinition module = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            MethodDefinition method = GetMethod(module, "Use_JustProperties");
            CustomAttribute attribute = GetAttribute(method, "JustPropertiesAttribute");

            IDictionary<string, object> props = attribute.GetAllDefinedProperties();
            IDictionary<string, object> expected = new Dictionary<string, object>
            {
                { "Value", "Overridden2" }
            };

            AssertDictionary(expected, props);
        }

        [Fact]
        public void GetAllDefinedProperties_GetsProperties_Null_Ignored()
        {
            ModuleDefinition module = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            MethodDefinition method = GetMethod(module, "Use_JustProperties_Null");
            CustomAttribute attribute = GetAttribute(method, "JustPropertiesAttribute");

            IDictionary<string, object> props = attribute.GetAllDefinedProperties();
            IDictionary<string, object> expected = new Dictionary<string, object> ();

            AssertDictionary(expected, props);
        }

        private static void AssertDictionary<K, V>(IDictionary<K, V> dict, IDictionary<K, V> expected)
        {
            Assert.Equal(expected.Count, dict.Count);

            foreach (var kvp in expected)
            {
                Assert.Equal(kvp.Value, dict[kvp.Key]);
            }
        }

        private MethodDefinition GetMethod(ModuleDefinition module, string methodName)
        {
            foreach (var type in module.Types)
            {
                foreach (MethodDefinition m in type.Methods)
                {
                    if (m.Name == methodName)
                    {
                        return m;
                    }
                }
            }
            
            throw new Exception($"Unable to load '{methodName}' method definition for testing.");
        }

        private CustomAttribute GetAttribute(MethodDefinition method, string attributeName)
        {
            foreach (var a in method.CustomAttributes)
            {
                if (a.AttributeType.Name == attributeName)
                {
                    return a;
                }
            }

            throw new Exception($"Unable to load '{attributeName}' attribute for testing.");
        }
    }

    public class Methods
    {
        [JustConstructor("Someone1", 25, 'y')]
        public void Use_JustConstructor()
        {
        }

        [ConstructorAndProperties("Someone2", 26, 'n', Value = "Overridden1")]
        public void Use_ConstructorAndProperties()
        {
        }

        [ConstructorAndProperties("Someone2", 26, 'n')]
        public void Use_ConstructorAndProperties_DefaultProperty()
        {
        }

        [JustProperties(Value = "Overridden2")]
        public void Use_JustProperties()
        {
        }

        [JustProperties(Value = null!)]
        public void Use_JustProperties_Null()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class JustConstructorAttribute : Attribute
    {
        public JustConstructorAttribute(string name, int number, char ch)
        {
        }
    }

    public class ConstructorAndPropertiesAttribute : Attribute
    {
        public ConstructorAndPropertiesAttribute(string name, int number, char ch)
        {
        }

        public string Value { get; set; } = "DefaultValue";
    }

    public class JustPropertiesAttribute : Attribute
    {
        public JustPropertiesAttribute()
        {
        }

        public string Value { get; set; } = "DefaultValue";
    }
}
