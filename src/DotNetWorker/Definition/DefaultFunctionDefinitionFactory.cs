using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
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

            var entryPointMatch = _entryPointRegex.Match(metadata.EntryPoint);
            if (!entryPointMatch.Success)
            {
                throw new InvalidOperationException("Invalid entry point configuration. The function entry point must be defined in the format <fulltypename>.<methodname>");
            }

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(metadata.PathToAssembly);

            string typeName = entryPointMatch.Groups["typename"].Value;
            string methodName = entryPointMatch.Groups["methodname"].Value;

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

            Type? functionType = assembly.GetType(typeName);

            MethodInfo? methodInfo = functionType?.GetMethod(methodName);

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
