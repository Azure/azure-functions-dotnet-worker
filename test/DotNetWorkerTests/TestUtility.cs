// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal static class TestUtility
    {
        public static string DefaultPropertyName = "input";

        public static ConverterMiddleware GetDefaultConverterMiddleware(Action<JsonSerializerOptions> configure = null)
        {
            return new ServiceCollection()
                .Configure<JsonSerializerOptions>(o => configure?.Invoke(o))
                .AddSingleton<ConverterMiddleware>()
                .RegisterOutputChannel()
                .RegisterDefaultConverters()
                .BuildServiceProvider()
                .GetService<ConverterMiddleware>();
        }

        public static T AssertIsTypeAndConvert<T>(object target)
            where T : class
        {
            if (target is not T converted)
            {
                throw new AssertActualExpectedException(typeof(T), target?.GetType(), string.Empty);
            }

            return converted;
        }
    }
}
