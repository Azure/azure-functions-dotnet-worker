Imports Microsoft.Azure.Functions.Worker.Builder
Imports Microsoft.Extensions.Hosting

Module Program
    Sub Main(args As String())
        Dim builder = FunctionsApplication.CreateBuilder(args)
        builder.ConfigureFunctionsWebApplication()
        builder.Build().Run()
    End Sub
End Module
