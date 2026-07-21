// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// A single WebJobs startup. Exercises GetWebJobsReferences (single startup detection).
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(TestExtension.FooWebJobsStartup))]

namespace TestExtension
{
    public class FooWebJobsStartup
    {
    }
}
