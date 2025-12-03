// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

internal class ServiceCollectionExtensionsTestUtility
{
    public static void AssertServiceRegistrationIdempotency(Action<IServiceCollection> configure,
        Func<Type, ImmutableList<ServiceDescriptor>, bool> registrationValidator = null)
    {
        var services = new ServiceCollection();

        configure(services);

        AssertServiceRegistrationIdempotency(services, registrationValidator);
    }

    public static void AssertServiceRegistrationIdempotency(IServiceCollection services,
        Func<Type, ImmutableList<ServiceDescriptor>, bool> registrationValidator = null)
    {
        registrationValidator ??= (t, d) => d.Count == 1;

        var invalidServices = services.GroupBy(s => s.ServiceType)
                                        .Where(g => !registrationValidator(g.Key, g.ToImmutableList()))
                                        .ToList();

        static string FormatService(ServiceDescriptor service) => $"""
                            - {service.ImplementationType ?? service.ImplementationInstance ?? service.ImplementationFactory}
                   
                   """;

        var stringBuilder = new StringBuilder();
        foreach (var service in invalidServices)
        {
            stringBuilder.AppendLine($"""
                                      Invalid service registrations for type: {service.Key}
                                            Implementation types:
                                      {service.Aggregate(string.Empty, (a, s) => a + FormatService(s))}
                                      """);
        }

        Assert.True(invalidServices.Count == 0, stringBuilder.ToString());
    }
}
