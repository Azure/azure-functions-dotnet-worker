using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AspNetIntegration
{
    public class FileDownload
    {
        // Replace this with your blob container name
        private const string BlobContainer = "runtimes";

        // Replace this with your blob name
        private const string BlobName = "dotnet-sdk-8.0.100-win-x64.exe";

        [Function("FileDownload")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [BlobInput($"{BlobContainer}/{BlobName}")] Stream blobStream)
        {
            return new FileStreamResult(blobStream, "application/octet-stream")
            {
                FileDownloadName = BlobName
            };
        }
    }
}
