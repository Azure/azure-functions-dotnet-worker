using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests.WorkerExtensionsSample;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// A sample extension used for extension startup hook source generation tests.

[assembly: WorkerExtensionStartup(typeof(SampleExtensionStartup))]

namespace Microsoft.Azure.Functions.Tests.WorkerExtensionsSample
{
    public sealed class SampleExtensionStartup : WorkerExtensionStartup
    {
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<StampHttpHeadersMiddleware>();
            applicationBuilder.Services.AddSingleton<IMyFooService, MyFooService>();
        }
    }

    public sealed class StampHttpHeadersMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IMyFooService _myFooService;

        public StampHttpHeadersMiddleware(IMyFooService myFooService)
        {
            _myFooService = myFooService ?? throw new ArgumentNullException(nameof(myFooService));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var logger = context.GetLogger<StampHttpHeadersMiddleware>();
            logger.LogInformation($"Inside middleware {_myFooService.GetMessage()}");

            await next(context);
        }
    }

    public class MyFooService : IMyFooService
    {
        public string GetMessage() => $"Foo";
    }

    public interface IMyFooService
    {
        string GetMessage();
    }
}
