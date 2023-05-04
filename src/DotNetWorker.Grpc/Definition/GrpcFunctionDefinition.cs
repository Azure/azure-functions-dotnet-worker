// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Core;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.Azure.Functions.Worker.Definition
{
    internal class GrpcFunctionDefinition : FunctionDefinition
    {
        public GrpcFunctionDefinition(FunctionLoadRequest loadRequest, IMethodInfoLocator methodInfoLocator)
        {
            EntryPoint = loadRequest.Metadata.EntryPoint;
            Name = loadRequest.Metadata.Name;
            Id = loadRequest.FunctionId;

            string? scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY");
            if (string.IsNullOrWhiteSpace(scriptRoot))
            {
                throw new InvalidOperationException("The 'FUNCTIONS_WORKER_DIRECTORY' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
            }

            if (string.IsNullOrWhiteSpace(loadRequest.Metadata.ScriptFile))
            {
                throw new InvalidOperationException($"Metadata for function '{loadRequest.Metadata.Name} ({loadRequest.Metadata.FunctionId})' does not specify a 'ScriptFile'.");
            }

            string scriptFile = Path.Combine(scriptRoot, loadRequest.Metadata.ScriptFile);
            PathToAssembly = Path.GetFullPath(scriptFile);

            var grpcBindingsGroup = loadRequest.Metadata.Bindings.GroupBy(kv => kv.Value.Direction);
            var grpcInputBindings = grpcBindingsGroup.Where(kv => kv.Key == BindingInfo.Types.Direction.In).FirstOrDefault();
            var grpcOutputBindings = grpcBindingsGroup.Where(kv => kv.Key != BindingInfo.Types.Direction.In).FirstOrDefault();
            var infoToMetadataLambda = new Func<KeyValuePair<string, BindingInfo>, BindingMetadata>(kv => new GrpcBindingMetadata(kv.Key, kv.Value));

            InputBindings = grpcInputBindings?.ToImmutableDictionary(kv => kv.Key, infoToMetadataLambda)
                ?? ImmutableDictionary<string, BindingMetadata>.Empty;

            OutputBindings = grpcOutputBindings?.ToImmutableDictionary(kv => kv.Key, infoToMetadataLambda)
                ?? ImmutableDictionary<string, BindingMetadata>.Empty;

            Parameters = methodInfoLocator.GetMethod(PathToAssembly, EntryPoint)
                .GetParameters()
                .Where(p => p.Name != null)
                .Select(p => new FunctionParameter(p.Name!, p.ParameterType, GetAdditionalPropertiesDictionary(p)))
                .ToImmutableArray();
        }

        public override string PathToAssembly { get; }

        public override string EntryPoint { get; }

        public override string Id { get; }

        public override string Name { get; }

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        private ImmutableDictionary<string, object> GetAdditionalPropertiesDictionary(ParameterInfo parameterInfo)
        {
            // Get the input converter attribute information, if present on the parameter.
            var inputConverterAttribute = parameterInfo?.GetCustomAttribute<InputConverterAttribute>();
            if (inputConverterAttribute != null)
            {
                return new Dictionary<string, object>()
                {
                    { PropertyBagKeys.ConverterType, inputConverterAttribute.ConverterType.AssemblyQualifiedName! }
                }.ToImmutableDictionary();
            }
            else
            {
                var inputAttribute = parameterInfo?.GetCustomAttribute<InputBindingAttribute>();
                var triggerAttribute = parameterInfo?.GetCustomAttribute<TriggerBindingAttribute>();

                return GetBindingAttributePropertiesDictionary(inputAttribute) ??
                        GetBindingAttributePropertiesDictionary(triggerAttribute) ??
                        ImmutableDictionary<string, object>.Empty;
            }
        }

        private ImmutableDictionary<string, object>? GetBindingAttributePropertiesDictionary(BindingAttribute? bindingAttribute)
        {
            if (bindingAttribute is null)
            {
                return null;
            }

            var output = new Dictionary<string, object>();
            bool isInputConverterAttributeAdvertised = false;

            // ConverterTypesDictionary will be "object" part of the return value of this method - ImmutableDictionary<string, object>
            // The dictionary has key of type IInputConverter and value as Properties of that converter (specifies supported types and support for Json Deserialization)
            var converterTypesDictionary = new Dictionary<Type, List<Type>>();

            IEnumerable<Attribute> customAttributes = bindingAttribute.GetType().GetCustomAttributes();

            foreach (Attribute element in customAttributes)
            {
                if (element.GetType() == typeof(InputConverterAttribute))
                {
                    var attribute = element as InputConverterAttribute;

                    if (attribute is not null)
                    {
                        isInputConverterAttributeAdvertised = true;
                        Type converter = attribute.ConverterType;
                        List<Type> supportedTypes = GetTypesSupportedByConverter(converter);
                        converterTypesDictionary.Add(converter, supportedTypes);
                    }
                }
            }

            output.Add(PropertyBagKeys.BindingAttributeSupportedConverters, converterTypesDictionary);

            if (isInputConverterAttributeAdvertised)
            {
                output.Add(PropertyBagKeys.AllowConverterFallback, false);

                foreach (Attribute element in customAttributes)
                {
                    if (element.GetType() == typeof(AllowConverterFallbackAttribute))
                    {
                        var attribute = element as AllowConverterFallbackAttribute;

                        if (attribute is not null)
                        {
                            output[PropertyBagKeys.AllowConverterFallback] = attribute.AllowConverterFallback;
                        }
                    }
                }
            }

            return output.ToImmutableDictionary();
        }

        private List<Type> GetTypesSupportedByConverter(Type converter)
        {
            var types = new List<Type>();

            foreach (var converterAttribute in converter.CustomAttributes)
            {
                if (converterAttribute.AttributeType == typeof(SupportedConverterTypeAttribute))
                {
                    Type? supportedTypeValue = null;

                    foreach (var supportedType in converterAttribute.ConstructorArguments)
                    {
                        if (supportedType.ArgumentType is not null
                            && supportedType.Value is not null
                            && supportedType.ArgumentType == typeof(Type))
                        {
                            supportedTypeValue = supportedType.Value as Type;

                            if (supportedTypeValue is not null)
                            {
                                types.Add(supportedTypeValue);
                            }
                        }
                    }
                }
            }

            return types;
        }
    }
}
