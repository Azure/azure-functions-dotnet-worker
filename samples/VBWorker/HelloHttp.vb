Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Mvc
Imports Microsoft.Azure.Functions.Worker
Imports Microsoft.Extensions.Logging

Public Class HelloHttp
    Private ReadOnly _logger As ILogger(Of HelloHttp)

    Public Sub New(logger As ILogger(Of HelloHttp))
        _logger = logger
    End Sub

    <[Function]("HelloHttp")>
    Public Function Run(<HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")> req As HttpRequest) As IActionResult
        _logger.LogInformation("Visual Basic HTTP trigger function processed a request.")
        Return New OkObjectResult("Welcome to Azure Functions, running Visual Basic!")
    End Function
End Class
