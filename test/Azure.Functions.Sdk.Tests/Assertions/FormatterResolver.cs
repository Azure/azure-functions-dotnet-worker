// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using AwesomeAssertions.Formatting;

namespace AwesomeAssertions;

interface IProvidesFormatter
{
}

/// <summary>
/// Ensures all <see cref="IProvidesFormatter"/> implementations in the assembly are registered to
/// <see cref="Formatter"/>.
/// </summary>
internal static class FormatterResolver
{
    public static void Initialize()
    {
        foreach (Type type in typeof(FormatterResolver).Assembly.GetTypes())
        {
            if (typeof(IProvidesFormatter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                IValueFormatter formatter = (IValueFormatter)type.GetMethod(
                    "CreateFormatter", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
                Formatter.AddFormatter(formatter);
            }
        }
    }
}
