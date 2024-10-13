using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.LocalSettingsJsonNotAllowedAsConfiguration, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.LocalSettingsJsonNotAllowedAsConfiguration, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sdk.Analyzers.Tests;

public class LocalSettingsJsonNotValidConfigurationTests
{
    private static readonly ReferenceAssemblies _referenceAssemblies = ReferenceAssemblies.Net.Net70.WithPackages(
            ImmutableArray.Create(new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.23.0")));
    
    [Fact]
    public async Task LocalSettingsJsonPassedToConfigurationIssuesWarning()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,
            
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
    public async Task NotLocalSettingsJsonDoesNotGenerateWarning()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,
            
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
            ReferenceAssemblies = _referenceAssemblies,

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
    
    [Fact]
    public async Task ConstLocalSettingsJsonIsCaught()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,

            TestCode = """
               using Microsoft.Extensions.Hosting;
               using Microsoft.Extensions.Configuration;
               
               public static class Program
               {
                   public static void Main()
                   {
                       const string fileName = "local.settings.json";
                       
                       var host = new HostBuilder()
                           .ConfigureFunctionsWorkerDefaults()
                           .ConfigureAppConfiguration((context, config) =>
                           {
                               config.AddJsonFile(fileName); // Should trigger a warning
                           })
                           .Build();
                       
                       host.Run();
                   }
               }
               """,
            
            ExpectedDiagnostics = {
                AnalyzerVerifier.Diagnostic()
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithSpan(14, 36, 14, 44)
            }
        }.RunAsync();
    }
    
    [Fact]
    public async Task LocalSettingsJsonAsVariableIsCaught()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,

            TestCode = """
                       using Microsoft.Extensions.Hosting;
                       using Microsoft.Extensions.Configuration;

                       public static class Program
                       {
                           public static void Main()
                           {
                               var fileName = "local.settings.json";
                               
                               var host = new HostBuilder()
                                   .ConfigureFunctionsWorkerDefaults()
                                   .ConfigureAppConfiguration((context, config) =>
                                   {
                                       config.AddJsonFile(fileName); // Should trigger a warning
                                   })
                                   .Build();
                               
                               host.Run();
                           }
                       }
                       """,
            
            ExpectedDiagnostics = {
                AnalyzerVerifier.Diagnostic()
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithSpan(14, 36, 14, 44)
            }
        }.RunAsync();
    }
    
    [Fact]
    public async Task OverloadWithOptionalBooleanIsHandled()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,
            
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
                                       config.AddJsonFile("local.settings.json", optional: true);
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
    public async Task OverloadWithReloadOnChangeIsHandled()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,
            
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
                                       config.AddJsonFile("local.settings.json", optional: false, reloadOnChange: true);
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
    public async Task LocalSettingsJsonAsVariableWithReassignment()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,

            TestCode = """
                       using Microsoft.Extensions.Hosting;
                       using Microsoft.Extensions.Configuration;

                       public static class Program
                       {
                           public static void Main()
                           {
                               var fileName = "settings.json";
                               fileName = "local.settings.json";
                               
                               var host = new HostBuilder()
                                   .ConfigureFunctionsWorkerDefaults()
                                   .ConfigureAppConfiguration((context, config) =>
                                   {
                                       config.AddJsonFile(fileName); // Should trigger a warning
                                   })
                                   .Build();
                               
                               host.Run();
                           }
                       }
                       """,
            
            ExpectedDiagnostics = {
                AnalyzerVerifier.Diagnostic()
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithSpan(15, 36, 15, 44)
            }
        }.RunAsync();
    }
    
    [Fact]
    public async Task LocalSettingsJsonAsVariableWithMultipleReassignments()
    {
        await new AnalyzerTest
        {
            ReferenceAssemblies = _referenceAssemblies,

            TestCode = """
                       using Microsoft.Extensions.Hosting;
                       using Microsoft.Extensions.Configuration;

                       public static class Program
                       {
                           public static void Main()
                           {
                               var fileName = "todo";
                               fileName = "local.settings.json";
                               fileName = "my.settings.json";
                               
                               var host = new HostBuilder()
                                   .ConfigureFunctionsWorkerDefaults()
                                   .ConfigureAppConfiguration((context, config) =>
                                   {
                                       config.AddJsonFile(fileName); // Should NOT trigger a warning
                                   })
                                   .Build();
                               
                               host.Run();
                           }
                       }
                       """,
            
            ExpectedDiagnostics = {
                // no warning expected
            }
        }.RunAsync();
    }
    
    // todo - static const variable
}
