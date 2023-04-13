using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Core.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSetServiceProviderMiddleware(this IApplicationBuilder builder) =>
            builder.UseMiddleware<SetServiceProviderMiddleware>();

        public static IApplicationBuilder UseInvokeFunctionMiddleware(this IApplicationBuilder builder) =>
            builder.UseMiddleware<InvokeFunctionMiddleware>();
    }
}
