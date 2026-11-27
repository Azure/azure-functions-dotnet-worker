module FSharpWorker.Program

open Microsoft.Azure.Functions.Worker.Builder
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =
    let builder = FunctionsApplication.CreateBuilder(args)
    builder.ConfigureFunctionsWebApplication() |> ignore
    builder.Build().Run()
    0
