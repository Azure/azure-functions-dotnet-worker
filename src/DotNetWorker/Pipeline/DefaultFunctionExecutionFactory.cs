using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    internal class DefaultFunctionExecutionFactory : IFunctionExecutionContextFactory
    {
        private IServiceScopeFactory _serviceScopeFactory;

        public DefaultFunctionExecutionFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public FunctionExecutionContext Create(InvocationRequest request)
        {
            var context = new FunctionExecutionContext(_serviceScopeFactory, request);

            return context;
        }
    }
}
