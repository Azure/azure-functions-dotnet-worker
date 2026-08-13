// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// A WebJobs startup with an explicit name argument, which should override the derived name.
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(TestExtension.Startup), "MyExplicitName")]

namespace TestExtension
{
    public class Startup
    {
    }
}
