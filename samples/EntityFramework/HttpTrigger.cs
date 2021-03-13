// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Function
{
    public class HttpTrigger
    {
        private readonly BloggingContext _context;
        public HttpTrigger(BloggingContext context)
        {
            _context = context;
        }

        [Function("GetPosts")]
        public HttpResponseData GetPosts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts")] HttpRequestData req,
            FunctionContext context)
        {
           var logger = context.GetLogger("GetPosts");
           logger.LogInformation("C# HTTP GET/posts trigger function processed a request.");

           var postsArray = _context.Posts.OrderBy(p => p.Title).ToArray();
           var response = req.CreateResponse(HttpStatusCode.OK);
           response.WriteAsJsonAsync()
           return response;
        }

        [Function("CreateBlog")]
        public async Task<HttpResponseData> CreateBlogAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CreateBlog");
            logger.LogInformation("C# HTTP POST/blog trigger function processed a request.");

            var blog = JsonConvert.DeserializeObject<Blog>(await new StreamReader(req.Body).ReadToEndAsync());
            logger.LogInformation(JsonConvert.SerializeObject(blog));

            var entity = await _context.Blogs.AddAsync(blog, CancellationToken.None);
            await _context.SaveChangesAsync(CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(JsonConvert.SerializeObject(entity.Entity));
            return response;
        }

        [Function("CreatePost")]
        public async Task<HttpResponseData> CreatePostAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/{id}/post")] HttpRequestData req,
            int id,
            FunctionContext context)
        {
            var logger = context.GetLogger("CreatePost");
            logger.LogInformation("C# HTTP POST/blog trigger function processed a request.");
               
            var post = JsonConvert.DeserializeObject<Post>(await new StreamReader(req.Body).ReadToEndAsync());
            post.BlogId = id;
            
            var entity = await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync(CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(JsonConvert.SerializeObject(entity.Entity));
            return response;
        }
    }
}
