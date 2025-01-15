// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public sealed class IgnoreOnNetFxTestRunTheory : TheoryAttribute
    {
        public IgnoreOnNetFxTestRunTheory()
        {
            if (IsNetFxTestRun())
            {
                Skip = "Ignore when test run is using netFx as AspNetCore is not supported.";
            }
        }

        private static bool IsNetFxTestRun()
            => string.Equals(Environment.GetEnvironmentVariable("DOTNET_VERSION"), "netfx", StringComparison.OrdinalIgnoreCase);
    }
}