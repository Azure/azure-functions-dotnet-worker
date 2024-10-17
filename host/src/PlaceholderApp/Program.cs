// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;

public class Program
{
    static int Main(string[] args)
    {
        return AppDomain.CurrentDomain.ExecuteAssemblyByName(Assembly.GetEntryAssembly().FullName, args);
    }
}
