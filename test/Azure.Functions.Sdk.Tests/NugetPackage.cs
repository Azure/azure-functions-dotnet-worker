// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Azure.Functions.Sdk.Tests;

public record NugetPackage(string Name, string Version)
{
    public static readonly NugetPackage SystemTextJson = new(
        Name: "System.Text.Json", Version: "10.0.1");

    public static readonly WorkerPackage ServiceBus = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus",
        Version: "5.23.0",
        WebJobsPackages: [WebJobs.ServiceBus]);

    public static readonly WorkerPackage StorageQueues = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues",
        Version: "5.5.3",
        WebJobsPackages: [WebJobs.StorageQueues]);

    public static readonly WorkerPackage StorageBlobs = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs",
        Version: "6.8.0",
        WebJobsPackages: [WebJobs.StorageBlobs]);

    public static readonly WorkerPackage Storage = new(
        Name: "Microsoft.Azure.Functions.Worker.Extensions.Storage",
        Version: "6.8.0",
        WebJobsPackages: [ WebJobs.StorageQueues, WebJobs.StorageBlobs ]);

    private static class WebJobs
    {
        public static readonly WebJobsPackage ServiceBus = new(
            Name: "Microsoft.Azure.WebJobs.Extensions.ServiceBus", Version: "5.17.0")
        {
            ExtensionName = "ServiceBus",
        };

        public static readonly WebJobsPackage StorageQueues = new(
            Name: "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", Version: "5.3.6")
        {
            ExtensionName = "AzureStorageQueues",
        };

        public static readonly WebJobsPackage StorageBlobs = new(
            Name: "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", Version: "5.3.6")
        {
            ExtensionName = "AzureStorageBlobs",
        };
    }
}

public record WorkerPackage(string Name, string Version, ImmutableArray<WebJobsPackage> WebJobsPackages)
    : NugetPackage(Name, Version);

public record WebJobsPackage(string Name, string Version) : NugetPackage(Name, Version)
{
    public string? ExtensionName { get; init; }
}
