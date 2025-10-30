// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using Xunit.Sdk;

[assembly: Azure.Functions.Sdk.Tests.CurrentTest]

namespace Azure.Functions.Sdk.Tests;

/// <summary>
/// A hook to get the current test method info.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class CurrentTestAttribute :
    BeforeAfterTestAttribute
{
    private static readonly AsyncLocal<MethodInfo?> _local = new();

    public override void Before(MethodInfo info) =>
        _local.Value = info;

    public override void After(MethodInfo info) =>
        _local.Value = null;

    public static MethodInfo GetMethod()
    {
        if (_local.Value is { } method)
        {
            return method;
        }

        throw new InvalidOperationException(
            "Could not resolve the current test info. This attribute needs to be registered on the assembly.");
    }

    public static string GetTestName()
    {
        MethodInfo method = GetMethod();
        Type type = method.ReflectedType!;

        string className = type.IsNested
            ? type.ReflectedType!.Name + "." + type.Name
            : type.Name;

        return className + "." + method.Name;
    }
}
