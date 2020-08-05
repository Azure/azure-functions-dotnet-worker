using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionDescriptor
{
    class DefaultFunctionDescriptorFactory : IFunctionDescriptorFactory
    {
        private IServiceScopeFactory _serviceScopeFactory;

        public DefaultFunctionDescriptorFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public IFunctionDescriptor Create(FunctionLoadRequest request)
        {
            return new DefaultFunctionDescriptor(request);
        }
    }
}
