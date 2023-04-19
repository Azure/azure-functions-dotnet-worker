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

            if (inputConverterAttribute != null && inputConverterAttribute.ConverterTypes != null && inputConverterAttribute.ConverterTypes.Any())
            {
                return new Dictionary<string, object>()
                {
                    { PropertyBagKeys.ConverterType, inputConverterAttribute.ConverterTypes.FirstOrDefault().AssemblyQualifiedName! }
                }.ToImmutableDictionary();
            }
            else {
                var inputAttribute = parameterInfo?.GetCustomAttribute<InputBindingAttribute>();
                var triggerAttribute = parameterInfo?.GetCustomAttribute<TriggerBindingAttribute>();

                if (inputAttribute != null)
                {
                    return GetBindingAttributePropertiesDictionary(inputAttribute.GetType().GetCustomAttributes());
                }
                else if (triggerAttribute != null)
                {
                    return GetBindingAttributePropertiesDictionary(triggerAttribute.GetType().GetCustomAttributes());
                }
            }

            return ImmutableDictionary<string, object>.Empty;
        }

        private ImmutableDictionary<string, object> GetBindingAttributePropertiesDictionary(IEnumerable<Attribute> customAttributes)
        {
            var result = new Dictionary<string, object>();
            var converterTypesDictionary = new Dictionary<Type, Tuple<bool, List<Type>>>();

            foreach (var element in customAttributes)
            {
                if (element.GetType() == typeof(InputConverterAttribute))
                {
                    var attribute = (InputConverterAttribute)element;

                    foreach (var converter in attribute.ConverterTypes)
                    {
                        var types = GetTypesSupportedByConverter(converter.CustomAttributes);
                        converterTypesDictionary.Add(converter, types);
                    }

                    result.Add(PropertyBagKeys.DisableConverterFallback, attribute.DisableConverterFallback);
                    result.Add(PropertyBagKeys.BindingAttributeConverters, converterTypesDictionary);
                }
            }

            return result.ToImmutableDictionary();
        }

        private Tuple<bool, List<Type>> GetTypesSupportedByConverter(IEnumerable<CustomAttributeData> converterAttributes)
        {
            var types = new List<Type>();
            bool supportsJsonDeserialization = false;

            foreach (var converterAttribute in converterAttributes)
            {
                if (converterAttribute.AttributeType == typeof(SupportedConverterTypesAttribute))
                {
                    foreach (var supportedTypes in converterAttribute.ConstructorArguments)
                    {
                        if (supportedTypes.ArgumentType == typeof(Type))
                        {
                            var b = supportedTypes.Value as Type;
                            if (b is not null)
                            {
                                types.Add(b);
                            }
                        }
                    }
                }
                else if (converterAttribute.AttributeType == typeof(SupportsJsonDeserialization))
                {
                    supportsJsonDeserialization = true;
                }
            }

            return new Tuple<bool, List<Type>>(supportsJsonDeserialization, types);
        }
    }
}
