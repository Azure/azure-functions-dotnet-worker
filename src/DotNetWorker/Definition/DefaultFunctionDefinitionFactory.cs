// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionDefinitionFactory : IFunctionDefinitionFactory
    {
        private readonly IFunctionInvokerFactory _functionInvokerFactory;
        private readonly IFunctionActivator _functionActivator;
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
        private readonly IMethodInfoLocator _methodInfoLocator;

        public DefaultFunctionDefinitionFactory(IFunctionInvokerFactory functionInvokerFactory, IFunctionActivator functionActivator,
            IOutputBindingsInfoProvider outputBindingsInfoProvider, IMethodInfoLocator methodInfoLocator)
        {
            _functionInvokerFactory = functionInvokerFactory ?? throw new ArgumentNullException(nameof(functionInvokerFactory));
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));
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

            IEnumerable<FunctionParameter> parameters = _methodInfoLocator.GetMethod(metadata.PathToAssembly, metadata.EntryPoint)
                .GetParameters()
                .Where(p => p.Name != null)
                .Select(p => new FunctionParameter(p.Name!, p.ParameterType));

            OutputBindingsInfo outputBindings = _outputBindingsInfoProvider.GetBindingsInfo(metadata);

            var definition = new DefaultFunctionDefinition(metadata, parameters, outputBindings);

            return definition;
        }
    }
}
