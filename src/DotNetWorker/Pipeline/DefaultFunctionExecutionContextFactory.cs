using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class DefaultFunctionExecutionContextFactory : IFunctionExecutionContextFactory
    {
        private IServiceScopeFactory _serviceScopeFactory;

        public DefaultFunctionExecutionContextFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public FunctionExecutionContext Create(InvocationRequest request)
        {
            var context = new DefaultFunctionExecutionContext(_serviceScopeFactory, request);

            return context;
        }
    }
}
