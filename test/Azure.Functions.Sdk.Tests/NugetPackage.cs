// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Azure.Functions.Sdk.Tests;

public record NugetPackage(string Name, string Version)
{
    public static readonly NugetPackage SystemTextJson = new(
        Name: "System.Text.Json", Version: "8.0.6");

    public static readonly WorkerPackage ServiceBus = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus",
        Version: "5.23.0",
        WebJobsPackages: [new("Microsoft.Azure.WebJobs.Extensions.ServiceBus", "5.17.0")]);

    public static readonly WorkerPackage StorageQueues = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues",
        Version: "5.5.3",
        WebJobsPackages: [new("Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.6")]);

    public static readonly WorkerPackage StorageBlobs = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs",
        Version: "6.8.0",
        WebJobsPackages: [new("Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", "5.3.6")]);

    public static readonly WorkerPackage Storage = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.Storage",
        Version: "6.8.0",
        WebJobsPackages:
        [
            new("Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.6"),
            new("Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", "5.3.6")
        ]);
}

public record WorkerPackage(string Name, string Version, ImmutableArray<NugetPackage> WebJobsPackages)
    : NugetPackage(Name, Version);
