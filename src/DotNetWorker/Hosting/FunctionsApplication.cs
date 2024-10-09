namespace Microsoft.Extensions.Hosting
{
    public class FunctionsApplication
    {
        public static FunctionsApplicationBuilder CreateBuilder(string[] args) =>
            new FunctionsApplicationBuilder(args);
    }
}
