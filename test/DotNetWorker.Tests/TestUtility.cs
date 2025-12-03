// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal static class TestUtility
    {
        public static string DefaultPropertyName = "input";

        public static DefaultInputConversionFeature GetDefaultInputConversionFeature(Action<WorkerOptions> configure = null)
        {
            return new ServiceCollection()
                .AddSingleton<IInputConverterProvider, DefaultInputConverterProvider>()
                .Configure<WorkerOptions>(o => configure?.Invoke(o))
                .AddSingleton<DefaultInputConversionFeature>()
                .RegisterOutputChannel()
                .AddDefaultInputConvertersToWorkerOptions()
                .BuildServiceProvider()
                .GetService<DefaultInputConversionFeature>();
        }

        public static ServiceProvider GetServiceProviderWithInputBindingServices(Action<WorkerOptions> configure = null)
        {
            return new ServiceCollection()
                .AddSingleton<IConverterContextFactory, DefaultConverterContextFactory>()
                .AddSingleton<IInputConverterProvider, DefaultInputConverterProvider>()
                .AddScoped<IBindingCache<ConversionResult>, DefaultBindingCache<ConversionResult>>()
                .AddSingleton<IInputConversionFeature, DefaultInputConversionFeature>()
                .Configure<WorkerOptions>(o => configure?.Invoke(o))
                .AddSingleton<DefaultFunctionInputBindingFeature>()
                .AddDefaultInputConvertersToWorkerOptions()
                .BuildServiceProvider();
        }

        public static T AssertIsTypeAndConvert<T>(object target)
        {
            if (target is not T converted)
            {
                throw new XunitException($"The type {typeof(T).FullName} does no, match expected {target?.GetType().FullName}");
            }

            return converted;
        }

        public static IOptions<TOptions> WrapOptions<TOptions>(TOptions options = null) where TOptions : class, new()
        {
            options ??= new TOptions();
            return new OptionsWrapper<TOptions>(options);
        }

        public static InvocationRequest CreateInvocationRequest(string invocationId = "")
        {
            return new InvocationRequest
            {
                InvocationId = invocationId,
                TraceContext = new RpcTraceContext
                {
                    TraceParent = Guid.NewGuid().ToString(),
                    TraceState = Guid.NewGuid().ToString()
                },
                RetryContext = new Grpc.Messages.RetryContext
                {
                    MaxRetryCount = 3,
                    RetryCount = 2
                }
            };
        }

        public static InvocationRequest CreateInvocationRequestWithNullRetryContext(string invocationId = "")
        {
            return new InvocationRequest
            {
                InvocationId = invocationId,
                TraceContext = new RpcTraceContext
                {
                    TraceParent = Guid.NewGuid().ToString(),
                    TraceState = Guid.NewGuid().ToString()
                },
                RetryContext = null
            };
        }

        internal static RpcHttp CreateRpcHttp()
        {
            var rpcHttp = new RpcHttp
            {
                Url = "https://m.sn"
            };
            rpcHttp.NullableHeaders["Accept-Encoding"] = new NullableString() { Value = "gzip, deflate" };
            rpcHttp.NullableHeaders["Cookie"] = new NullableString() { Value = "theme=light; x-token=foo" };

            return rpcHttp;
        }
    }
}
