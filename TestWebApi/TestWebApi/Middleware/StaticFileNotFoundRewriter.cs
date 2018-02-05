using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TestWebApi.Middleware
{
    public class StaticFileNotFoundRewriter : IMiddleware // Gives Strong Type + Option to create Transient/Scoped lifetime.
    {
        public StaticFileNotFoundRewriter()
        {
            Trace.WriteLine("Item Initiated.");
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var path = context.Request.Path.Value;

            await next(context);

            if (!path.StartsWith("/api/", StringComparison.InvariantCulture) && context.Response.StatusCode == 404)
            {
                context.Request.Path = new PathString("/");
                await next(context);
            }
        }
    }
}
