using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class DefaultTestWorkerClient : TestWorkerClient
    {
        private readonly IFunctionsApplication _application;
        private readonly IEnumerable<IInvocationFeatureProvider> _featureProviders;
        private readonly TestFunctionMap _functionMap;
        private readonly IOutputBindingsInfoProvider _outputProvider;

        public DefaultTestWorkerClient(IFunctionsApplication application, IEnumerable<IInvocationFeatureProvider> featureProviders,
            IOutputBindingsInfoProvider outputProvider, TestFunctionMap functionMap)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _outputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
            _featureProviders = featureProviders ?? throw new ArgumentNullException(nameof(featureProviders));
            _functionMap = functionMap ?? throw new ArgumentNullException(nameof(functionMap));
        }

        public override async Task<InvocationResult> InvokeAsync(string functionName, InvocationContext invocationContext)
        {
            var result = new InvocationResult();

            var features = new InvocationFeatures(_featureProviders);
            var functionInvocation = new TestFunctionInvocation(_functionMap.GetFunctionId(functionName));
            features.Set<FunctionInvocation>(functionInvocation);

            FunctionContext context = _application.CreateContext(features);

            OutputBindingsInfo bindingsInfo = _outputProvider.GetBindingsInfo(context.FunctionDefinition);
            var bindingsFeature = new TestFunctionBindingFeature(invocationContext.TriggerMetadata, invocationContext.InputData, bindingsInfo, context);
            features.Set<IFunctionBindingsFeature>(bindingsFeature);

            try
            {
                await _application.InvokeFunctionAsync(context);
                result.OutputBindings = bindingsFeature.OutputBindingData;
                _application.DisposeContext(context, null);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                _application.DisposeContext(context, ex);
            }

            return result;
        }

        public override InvocationContext CreateContext() => new();
    }
}
