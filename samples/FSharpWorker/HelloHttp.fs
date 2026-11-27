namespace FSharpWorker

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging

type HelloHttp(logger: ILogger<HelloHttp>) =

    [<Function("HelloHttp")>]
    member _.Run([<HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")>] req: HttpRequest) : IActionResult =
        logger.LogInformation("F# HTTP trigger function processed a request.")
        OkObjectResult("Welcome to Azure Functions, running F#!") :> IActionResult
