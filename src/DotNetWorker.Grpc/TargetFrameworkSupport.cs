// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal static class TargetFrameworkSupport
    {
        private static readonly Dictionary<string, DateTimeOffset> _knownEndOfLifeDates = new()
        {
            [".NETCoreApp,Version=v1.0"] = new DateTimeOffset(2019, 6, 27, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v1.1"] = new DateTimeOffset(2019, 6, 27, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v2.0"] = new DateTimeOffset(2020, 8, 10, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v2.1"] = new DateTimeOffset(2021, 8, 21, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v2.2"] = new DateTimeOffset(2019, 12, 23, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v3.0"] = new DateTimeOffset(2020, 3, 3, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v3.1"] = new DateTimeOffset(2022, 12, 3, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v5.0"] = new DateTimeOffset(2022, 5, 10, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v6.0"] = new DateTimeOffset(2024, 11, 12, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v7.0"] = new DateTimeOffset(2024, 5, 14, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v8.0"] = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
            [".NETCoreApp,Version=v9.0"] = new DateTimeOffset(2026, 05, 12, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.0"] = new DateTimeOffset(2016, 1, 12, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.5"] = new DateTimeOffset(2016, 1, 12, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.5.1"] = new DateTimeOffset(2016, 1, 12, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.5.2"] = new DateTimeOffset(2022, 4, 26, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.6"] = new DateTimeOffset(2022, 4, 26, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.6.1"] = new DateTimeOffset(2022, 4, 26, 0, 0, 0, TimeSpan.Zero),
            [".NETFramework,Version=v4.6.2"] = new DateTimeOffset(2027, 1, 12, 0, 0, 0, TimeSpan.Zero),
        };

        public static bool HasWarning(out string? warning)
        {
            DateTimeOffset eol = GetEndOfLifeDate(out string tfm);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string link = tfm.StartsWith(".NETFramework") ?
                "https://dotnet.microsoft.com/platform/support/policy/dotnet-framework" :
                "https://dotnet.microsoft.com/platform/support/policy/dotnet-core";

            warning = null;
            if (now > eol)
            {
                warning = $"The target framework {tfm} is past its end-of-life date of {eol}. "
                        + $"See {link} for more information.";
            }
            else if ((now - TimeSpan.FromDays(180)) > eol)
            {
                warning = $"The target framework {tfm} will be end-of-life on {eol}. "
                        + $"See {link} for more information.";
            }

            return warning is not null;
        }

        public static DateTimeOffset GetEndOfLifeDate(out string targetFramework)
        {
            TargetFrameworkAttribute? attribute = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<TargetFrameworkAttribute>();
            if (attribute is null)
            {
                throw new InvalidOperationException("Entry assembly does not have a TargetFrameworkAttribute");
            }

            targetFramework = attribute.FrameworkName;
            return GetEndOfLifeDate(attribute!);
        }

        public static DateTimeOffset GetEndOfLifeDate(TargetFrameworkAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            if (attribute?.FrameworkName is null)
            {
                throw new ArgumentException("Target framework attribute has no framework name", nameof(attribute));
            }

            if (_knownEndOfLifeDates.TryGetValue(attribute.FrameworkName, out DateTimeOffset knownEol))
            {
                return knownEol;
            }

            return DateTimeOffset.MaxValue;
        }
    }
}
