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
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
        private readonly IMethodInfoLocator _methodInfoLocator;

        public DefaultFunctionDefinitionFactory(IOutputBindingsInfoProvider outputBindingsInfoProvider, IMethodInfoLocator methodInfoLocator)
        {
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));
        }

        public FunctionDefinition Create(FunctionLoadRequest request)
        {
            FunctionDefinition definition = request.ToFunctionDefinition(_methodInfoLocator, _outputBindingsInfoProvider);

            if (definition.PathToAssembly == null)
            {
                throw new InvalidOperationException("The path to the function assembly is null.");
            }

            if (definition.EntryPoint == null)
            {
                throw new InvalidOperationException("The entry point is null.");
            }



            return definition;
        }
    }
}
