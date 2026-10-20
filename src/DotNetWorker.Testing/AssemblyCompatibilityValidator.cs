// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Testing;

internal static class AssemblyCompatibilityValidator
{
    public static void ValidateReferencedAssembly(Assembly consumer, Assembly dependency)
    {
        AssemblyName actual = dependency.GetName();
        AssemblyName? expected = consumer.GetReferencedAssemblies()
            .SingleOrDefault(reference => string.Equals(reference.Name, actual.Name, StringComparison.Ordinal));

        if (expected is null)
        {
            throw new InvalidOperationException(
                $"Testing assembly '{consumer.GetName().Name}' does not declare its required reference to '{actual.Name}'. "
                + "Restore the exact compatible testing package set.");
        }

        ValidateReference(consumer.GetName().Name!, expected, actual);
    }

    internal static void ValidateReference(string consumerName, AssemblyName expected, AssemblyName actual)
    {
        if (expected.Version == actual.Version)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Testing package assembly mismatch: '{consumerName}' requires '{expected.Name}' assembly version "
            + $"'{expected.Version}', but version '{actual.Version}' is loaded. Restore the exact package versions "
            + "recorded by the testing package compatibility manifest.");
    }
}
