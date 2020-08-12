using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Azure.Functions.DotNetWorker.Invocation;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class DefaultFunctionDefinitionFactory : IFunctionDefinitionFactory
    {
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

            string? assemblyName = AssemblyName.GetAssemblyName(metadata.PathToAssembly).Name;
            string typeName = $"{assemblyName}.{metadata.FuncName}";

            Type? functionType = Assembly.GetEntryAssembly()?.GetType(typeName);
            MethodInfo? methodInfo = functionType?.GetMethod("Run");

            IFunctionInvoker invoker = _functionInvokerFactory.Create(methodInfo);

            metadata.FuncParamInfo = methodInfo.GetParameters().ToImmutableArray();

            return new FunctionDefinition
            {
                Metadata = metadata,
                Invoker = invoker
            };
        }
    }
}
