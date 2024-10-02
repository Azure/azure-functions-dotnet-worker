namespace Microsoft.Extensions.Hosting
{
    public class FunctionsApplication
    {
        public static FunctionsApplicationBuilder CreateBuilder(string[] args)
        {
            return new FunctionsApplicationBuilder(hostBuilder =>
            {
                hostBuilder
                    .ConfigureDefaults(args)
                    .ConfigureFunctionsWorkerDefaults();
            });
        }
    }
}
