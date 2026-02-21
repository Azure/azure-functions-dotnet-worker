// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class OpenTelemetryTraceConstantsTests
{
    [Theory]
    [InlineData(typeof(TraceConstants.OTelAttributes_1_17_0))]
    [InlineData(typeof(TraceConstants.OTelAttributes_1_37_0))]
    [InlineData(typeof(TraceConstants.InternalKeys))]
    public void All_Field_Contains_All_Public_Constants(Type classType)
    {
        // Get all public const string fields (excluding the "All" field itself)
        var publicConstants = classType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string) && f.Name != "All")
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != null)
            .OrderBy(v => v)
            .ToArray();

        // Get the "All" field
        var allField = classType.GetField("All", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(allField);

        var allValues = (allField.GetValue(null) as string[])?.OrderBy(v => v).ToArray();
        Assert.NotNull(allValues);

        // Verify that "All" contains exactly the same values as all public constants
        Assert.Equal(publicConstants, allValues);
    }
}
