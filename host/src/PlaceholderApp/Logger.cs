// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using FunctionsNetHost.Shared;

internal static class Logger
{
    internal static void Log(string message)
    {
        var ts = DateTime.UtcNow.ToString(Constants.LogTimeStampFormat, CultureInfo.InvariantCulture);
        Console.WriteLine($"{Constants.DefaultLogPrefix}[{ts}] [{Constants.LogCategory}][{Constants.LogSubCategory}] {message}");
    }
}
