using Microsoft.Azure.Functions.Worker.Builder;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

IHost host = builder.Build();

await host.RunAsync();
