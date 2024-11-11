// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class WorkerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFunctionsWorkerDefaults_RegistersServicesIdempotently()
    {
        static bool EvaluateServiceRegistration(Type type, ImmutableList<ServiceDescriptor> descriptors)
        {
            return type switch
            {
                Type t when t == typeof(IUserLogWriter) || t == typeof(ISystemLogWriter) || t == typeof(IUserMetricWriter)
                  => descriptors.Count == 2 && descriptors.Last().ImplementationFactory.Method.ReturnType == typeof(GrpcFunctionsHostLogWriter),
                _ => descriptors.Count == 1,
            };
        }

        ServiceCollectionExtensionsTestUtility.AssertServiceRegistrationIdempotency(services =>
        {
            services.AddFunctionsWorkerDefaults();
            services.AddFunctionsWorkerDefaults();
        }, EvaluateServiceRegistration);
    }
}
