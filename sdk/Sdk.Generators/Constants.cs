﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class Constants
    {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        // Suppressing warning based on https://github.com/dotnet/roslyn-analyzers/issues/6467
        internal static readonly string NewLine = Environment.NewLine;
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

        internal static readonly string GeneratedCodeAttribute =
            $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(" +
            $"\"{typeof(Constants).Assembly.GetName().Name}\", " +
            $"\"{typeof(Constants).Assembly.GetName().Version}\")]";

        internal static class ExecutionModel
        {
            internal const string Isolated = "isolated";
        }

        internal static class Languages
        {
            internal const string DotnetIsolated = "dotnet-isolated";
        }

        internal static class BuildProperties
        {
            internal const string FunctionsExecutionModel = "build_property.FunctionsExecutionModel";
            internal const string MSBuildTargetFrameworkIdentifier = "build_property.TargetFrameworkIdentifier";
            internal const string GeneratedCodeNamespace = "build_property.FunctionsGeneratedCodeNamespace";
            internal const string EnableMetadataSourceGen = "build_property.FunctionsEnableMetadataSourceGen";
            internal const string EnableExecutorSourceGen = "build_property.FunctionsEnableExecutorSourceGen";
            internal const string AutoRegisterGeneratedFunctionsExecutor = "build_property.FunctionsAutoRegisterGeneratedFunctionsExecutor";
            internal const string AutoRegisterGeneratedMetadataProvider = "build_property.FunctionsAutoRegisterGeneratedMetadataProvider";
        }

        internal static class FileNames
        {
            internal const string GeneratedFunctionMetadata = "GeneratedFunctionMetadataProvider.g.cs";
            internal const string GeneratedFunctionExecutor = "GeneratedFunctionExecutor.g.cs";
        }

        internal static class Types
        {
            // Azure Functions worker types
            internal const string FunctionName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
            internal const string BindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingAttribute";
            internal const string OutputBindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.OutputBindingAttribute";
            internal const string BindingPropertyNameAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingPropertyNameAttribute";
            internal const string DefaultValue = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.DefaultValueAttribute";

            internal const string HttpResponseData = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";
            internal const string HttpResultAttribute = "Microsoft.Azure.Functions.Worker.HttpResultAttribute";
            internal const string HttpTriggerBinding = "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute";

            internal const string BindingCapabilitiesAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingCapabilitiesAttribute";
            internal const string RetryAttribute = "Microsoft.Azure.Functions.Worker.RetryAttribute";
            internal const string FixedDelayRetryAttribute = "Microsoft.Azure.Functions.Worker.FixedDelayRetryAttribute";
            internal const string ExponentialBackoffRetryAttribute = "Microsoft.Azure.Functions.Worker.ExponentialBackoffRetryAttribute";
            internal const string InputConverterAttributeType = "Microsoft.Azure.Functions.Worker.Converters.InputConverterAttribute";
            internal const string SupportedTargetTypeAttributeType = "Microsoft.Azure.Functions.Worker.Converters.SupportedTargetTypeAttribute";
            internal const string SupportsDeferredBindingAttributeType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.SupportsDeferredBindingAttribute";

            // System types
            internal const string IEnumerableOfKeyValuePair = "System.Collections.Generic.IEnumerable`1<System.Collections.Generic.KeyValuePair`2<TKey,TValue>>";
        }

        internal static class FunctionMetadataBindingProps {
            internal const string ReturnBindingName = "$return";
            internal const string HttpResponseBindingName = "HttpResponse";
            internal const string IsBatchedKey = "IsBatched";
        }

        internal static class RetryConstants
        {
            internal const string MaxRetryCountKey = "maxRetryCount";
            internal const string MinimumIntervalKey = "minimumInterval";
            internal const string MaximumIntervalKey = "maximumInterval";
            internal const string DelayIntervalKey = "delayInterval";
        }

        internal static class BindingCapabilities
        {
            public const string FunctionLevelRetry = "FunctionLevelRetry";
        }
    }
}
