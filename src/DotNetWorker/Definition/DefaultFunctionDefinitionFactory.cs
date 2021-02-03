// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionDefinitionFactory : IFunctionDefinitionFactory
    {
        private static readonly Regex _entryPointRegex = new Regex("^(?<typename>.*)\\.(?<methodname>\\S*)$");
        private readonly IFunctionInvokerFactory _functionInvokerFactory;
        private readonly IFunctionActivator _functionActivator;

        public DefaultFunctionDefinitionFactory(IFunctionInvokerFactory functionInvokerFactory, IFunctionActivator functionActivator)
        {
            _functionInvokerFactory = functionInvokerFactory ?? throw new ArgumentNullException(nameof(functionInvokerFactory));
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }

        public FunctionDefinition Create(FunctionLoadRequest request)
        {
            FunctionMetadata metadata = request.ToFunctionMetadata();

            if (metadata.PathToAssembly == null)
            {
                throw new InvalidOperationException("The path to the function assembly is null.");
            }

            if (metadata.EntryPoint == null)
            {
                throw new InvalidOperationException("The entry point is null.");
            }

            var entryPointMatch = _entryPointRegex.Match(metadata.EntryPoint);
            if (!entryPointMatch.Success)
            {
                throw new InvalidOperationException("Invalid entry point configuration. The function entry point must be defined in the format <fulltypename>.<methodname>");
            }

            string typeName = entryPointMatch.Groups["typename"].Value;
            string methodName = entryPointMatch.Groups["methodname"].Value;

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(metadata.PathToAssembly);

            Type? functionType = assembly.GetType(typeName);

            MethodInfo? methodInfo = functionType?.GetMethod(methodName);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' specified in {nameof(FunctionMetadata.EntryPoint)} was not found. This function cannot be created.");
            }

            IFunctionInvoker invoker = _functionInvokerFactory.Create(methodInfo);

            IEnumerable<FunctionParameter> parameters = methodInfo.GetParameters()
                .Where(p => p.Name != null)
                .Select(p => new FunctionParameter(p.Name!, p.ParameterType));

            return new DefaultFunctionDefinition(metadata, invoker, parameters);
        }
    }
}
