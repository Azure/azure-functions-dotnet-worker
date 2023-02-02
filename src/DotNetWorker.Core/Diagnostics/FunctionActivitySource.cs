// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class FunctionActivitySourceFactory
    {
        private static readonly ActivitySource _activitySource = new(TraceConstants.FunctionsActivitySource, TraceConstants.FunctionsActivitySourceVersion);
        private readonly string _schemaVersionUrl;
        private readonly Lazy<IReadOnlyDictionary<string, string>> _attributeMap;

        public FunctionActivitySourceFactory(IOptions<WorkerOptions> options)
        {
            _attributeMap = new Lazy<IReadOnlyDictionary<string, string>>(() => GetMapping(options.Value.OpenTelemetrySchemaVersion));
            _schemaVersionUrl = TraceConstants.OpenTelemetrySchemaMap[options.Value.OpenTelemetrySchemaVersion];
        }

        public Activity? StartInvoke(FunctionContext context)
        {
            var activity = _activitySource.StartActivity(TraceConstants.FunctionsInvokeActivityName, ActivityKind.Internal, context.TraceContext.TraceParent,
                tags: GetTags(context));

            if (activity is not null)
            {
                activity.TraceStateString = context.TraceContext.TraceState;
            }

            return activity;
        }

        /// <summary>
        /// Provides key mappings for different schema versions. For example, in early versions the invocation id may be
        /// represented by "faas.execution" and then later change to "faas.invocation". We want to allow for each of these as
        /// exporters may be relying on them.
        /// </summary>
        /// <param name="schemaVersion"></param>
        /// <returns>The mapped key name.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static IReadOnlyDictionary<string, string> GetMapping(OpenTelemetrySchemaVersion schemaVersion)
        {
            return schemaVersion switch
            {
                OpenTelemetrySchemaVersion.v1_17_0 => ImmutableDictionary<string, string>.Empty,
                _ => throw new InvalidOperationException("Schema not supported."),
            };
        }

        private IEnumerable<KeyValuePair<string, object?>> GetTags(FunctionContext context)
        {
            yield return new(TraceConstants.AttributeSchemaUrl, _schemaVersionUrl);

            string GetKeyMapping(string key) => _attributeMap.Value.GetValueOrDefault(key, key);

            // Using as an example of how to map if schemas change.
            yield return new(GetKeyMapping(TraceConstants.AttributeFaasExecution), context.InvocationId);
        }
    }
}
