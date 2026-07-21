// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// A realistic extension: a single WebJobs startup plus extension information. Exercises both
// GetWebJobsReferences (single startup detection) and TryGetExtensionReference.
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(TestExtension.FooWebJobsStartup))]
[assembly: ExtensionInformation("MyExtension", "1.2.3")]

namespace TestExtension
{
    public class FooWebJobsStartup
    {
    }
}
