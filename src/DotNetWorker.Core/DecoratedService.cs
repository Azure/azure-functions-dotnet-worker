// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

// Only needed if we need to add `CompositeFunctionMetadataProvider` in GrpcServiceCollectionExtensions
internal static class DecoratedService
{
    private static Func<IServiceProvider, object> GetFactory(ServiceDescriptor descriptor)
    {

        return descriptor.ImplementationFactory
               ?? ((p) => descriptor.ImplementationInstance ??
                          ActivatorUtilities.CreateInstance(p, descriptor.ImplementationType!));
    }

    public static IServiceCollection Decorate<TService, TImplementation>(this IServiceCollection collection)
        where TService : class
        where TImplementation : class, TService
    {
        var descriptor = collection.LastOrDefault(d => d.ServiceType == typeof(TService))
                         ?? throw new InvalidOperationException($"Service of type {typeof(TService).Name} not found in the collection.");

        collection.Remove(descriptor);

        var newDescriptor = ServiceDescriptor.Describe(typeof(Decorator<TService>), p => new Decorator<TService>(GetFactory(descriptor)), descriptor.Lifetime);
        collection.Add(newDescriptor);

        collection.AddSingleton<TService>(p =>
        {
            var decoratedType = p.GetRequiredService<Decorator<TService>>().Create(p);
            return ActivatorUtilities.CreateInstance<TImplementation>(p, decoratedType!);
        });

        return collection; ;
    }

    private class Decorator<TService>(Func<IServiceProvider, object> factory)
    {
        public TService Create(IServiceProvider serviceProvider)
        {
            return (TService)factory(serviceProvider);
        }
    }
}
