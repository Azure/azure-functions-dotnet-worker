// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace AspNetIntegration
{
    public class BodyBindingHttpTrigger
    {
        [Function(nameof(BodyBindingHttpTrigger))]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [FromBody] Person person)
        {
            return new OkObjectResult(person);
        }
    }

    public record Person(string Name, int Age);
}
