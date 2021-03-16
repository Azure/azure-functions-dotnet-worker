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
        public async Task<HttpResponseData> GetPosts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts")] HttpRequestData req,
            FunctionContext context)
        {
           var logger = context.GetLogger("GetPosts");
           logger.LogInformation("Get Posts HTTP trigger function processed a request.");

           var postsArray = _context.Posts.OrderBy(p => p.Title).ToArray();
           var response = req.CreateResponse(HttpStatusCode.OK);
           await response.WriteAsJsonAsync(postsArray);
           return response;
        }

        [Function("CreateBlog")]
        public async Task<HttpResponseData> CreateBlogAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CreateBlog");
            logger.LogInformation("Create Blog HTTP trigger function processed a request.");

            var blog = await req.ReadFromJsonAsync<Blog>();

            var entity = await _context.Blogs.AddAsync(blog, CancellationToken.None);
            await _context.SaveChangesAsync(CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(entity.Entity);
            return response;
        }

        [Function("CreatePost")]
        public async Task<HttpResponseData> CreatePostAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/{id}/post")] HttpRequestData req,
            int id,
            FunctionContext context)
        {
            var logger = context.GetLogger("CreatePost");
            logger.LogInformation("Create Post HTTP trigger function processed a request.");
               
            var post = await req.ReadFromJsonAsync<Post>();
            post.BlogId = id;
            
            var entity = await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync(CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(entity.Entity);
            return response;
        }
    }
}
