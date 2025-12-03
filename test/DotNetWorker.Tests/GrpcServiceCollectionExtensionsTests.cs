// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class GrpcServiceCollectionExtensionsTests
{
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
