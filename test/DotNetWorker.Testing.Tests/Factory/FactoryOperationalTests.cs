// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Testing.Protocol;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.Factory;

public class FactoryOperationalTests
{
    [Fact]
    public async Task Factory_UnavailableSerializedTransportHonorsStartupTimeout()
    {
        TimeSpan startupTimeout = TimeSpan.FromMilliseconds(250);
        await using var factory = new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithContentRoot(GetFunctionOutput())
            .WithOptions(options => options.StartupTimeout = startupTimeout)
            .WithSerializedGrpcTransport(
                new Uri("http://127.0.0.1:1"),
                _ => { });

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(
            () => Task.Run(() => _ = factory.Services));

        Assert.Contains(startupTimeout.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Factory_DisposalReleasesProtocolGraph()
    {
        WeakReference reference = await CreateDisposedProtocolReferenceAsync();

        for (int attempt = 0; attempt < 5 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(20);
        }

        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CreateDisposedProtocolReferenceAsync()
    {
        InMemoryFunctionsHost? protocol = null;
        var factory = new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithContentRoot(GetFunctionOutput())
            .WithProtocolObserver(value => protocol = value);
        _ = factory.Services;
        var reference = new WeakReference(protocol);
        protocol = null;
        await factory.DisposeAsync();
        factory = null!;
        return reference;
    }

    private static string GetFunctionOutput()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Resources",
            "Testing",
            "ModernFunctionApp",
            "bin",
            "Release",
            "net8.0"));
}
