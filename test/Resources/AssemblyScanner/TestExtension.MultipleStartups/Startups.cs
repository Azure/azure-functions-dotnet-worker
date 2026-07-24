// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Multiple WebJobs startups on a single assembly; the scanner should return all of them.
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(TestExtension.FirstWebJobsStartup))]
[assembly: WebJobsStartup(typeof(TestExtension.SecondWebJobsStartup))]

namespace TestExtension
{
    public class FirstWebJobsStartup
    {
    }

    public class SecondWebJobsStartup
    {
    }
}
