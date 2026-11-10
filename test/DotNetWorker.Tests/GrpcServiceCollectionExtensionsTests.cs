// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Core.Configuration;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class GrpcServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGrpc_DoesNotRegisterApplicationConfiguration()
    {
        var services = new ServiceCollection();

        services.AddGrpc();

        Assert.DoesNotContain(services, s => s.ServiceType == typeof(HostConfigurationSnapshotProvider));
    }

    [Fact]
    public void AddGrpc_PayloadProviderResolvesWithoutTransportOptions()
    {
        ServiceCollection services = new();
        services.AddGrpc();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<ExtensionPayloadProvider>(provider.GetRequiredService<IExtensionPayloadProvider>());
    }

    [Fact]
    public void AddGrpc_RegistersServicesIdempotently()
    {
        ServiceCollectionExtensionsTestUtility.AssertServiceRegistrationIdempotency(services =>
        {
            services.AddGrpc();
            services.AddGrpc();
        });
    }
}
