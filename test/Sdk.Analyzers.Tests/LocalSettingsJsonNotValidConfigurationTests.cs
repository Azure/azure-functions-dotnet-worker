﻿using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.LocalSettingsJsonNotAllowedAsConfiguration, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.LocalSettingsJsonNotAllowedAsConfiguration, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sdk.Analyzers.Tests;

public class LocalSettingsJsonNotValidConfigurationTests
{
    [Fact]
    public async Task LocalSettingsJsonPassedToConfigurationIssuesWarning()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.23.0"))),
            
            TestCode = """
               using Microsoft.Extensions.Hosting;
               using Microsoft.Extensions.Configuration;

               public static class Program
               {
                   public static void Main()
                   {
                       var host = new HostBuilder()
                           .ConfigureFunctionsWorkerDefaults()
                           .ConfigureAppConfiguration((context, config) =>
                           {
                               config.AddJsonFile("local.settings.json");
                           })
                           .Build();
                   
                       host.Run();
                   }
               }
               """,
            
            ExpectedDiagnostics = {
                AnalyzerVerifier.Diagnostic()
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithSpan(12, 36, 12, 57)
            }
        }.RunAsync();
    }
    
    [Fact]
    public async Task NotLocalSettingsJsonDoesntGenerateWarning()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.23.0"))),
            
            TestCode = """
               using Microsoft.Extensions.Hosting;
               using Microsoft.Extensions.Configuration;
               
               public static class Program
               {
                   public static void Main()
                   {
                       var host = new HostBuilder()
                           .ConfigureFunctionsWorkerDefaults()
                           .ConfigureAppConfiguration((context, config) =>
                           {
                               config.AddJsonFile("settings.json");
                           })
                           .Build();
                   
                       host.Run();
                    }
               }
               """,
            
            ExpectedDiagnostics = {
                // No diagnostics expected
            }
        }.RunAsync();
    }
    
    [Fact]
    public async Task CustomAddJsonFileMethodDoesNotGenerateWarning()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.23.0"))),

            TestCode = """
               public class MyCustomConfig
               {
                   public void AddJsonFile(string fileName)
                   {
                       // Custom implementation
                   }
               }

               public static class Program
               {
                   public static void Main()
                   {
                       var config = new MyCustomConfig();
                       config.AddJsonFile("local.settings.json"); // Should not trigger warning
                   }
               }
               """,
            
            ExpectedDiagnostics = {
                // No diagnostics expected
            }
        }.RunAsync();
    }
}