namespace Microsoft.Extensions.Hosting;

public class FunctionsWebApplication
{
    public static FunctionsApplicationBuilder CreateBuilder(string[] args) =>
        new(hostBuilder => hostBuilder.ConfigureFunctionsWebApplication());
}
