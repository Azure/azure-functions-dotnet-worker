// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using AwesomeAssertions.Formatting;

[assembly: AssemblyFixture(typeof(AwesomeAssertions.FormatterFixture))]

namespace AwesomeAssertions;

interface IProvidesFormatter
{
    static abstract IValueFormatter CreateFormatter();
}

/// <summary>
/// Ensures all <see cref="IProvidesFormatter"/> implementations in the assembly are registered to
/// <see cref="Formatter"/>.
/// </summary>
internal class FormatterFixture
{
    public FormatterFixture()
    {
        MethodInfo method = typeof(FormatterFixture)
            .GetMethod(nameof(GetFormatter), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (Type type in typeof(FormatterFixture).Assembly.GetTypes())
        {
            if (typeof(IProvidesFormatter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                IValueFormatter formatter = (IValueFormatter)method.MakeGenericMethod(type).Invoke(null, null)!;
                Formatter.AddFormatter(formatter);
            }
        }
    }

    private static IValueFormatter GetFormatter<T>()
        where T : IProvidesFormatter
        => T.CreateFormatter();
}
