using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TestWebApi.Middleware
{
    public class RequestLogger
    {
        private readonly RequestDelegate _next;

        public RequestLogger(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context) // Use 'Invoke' for non-async methods.
        {
            var startTime = DateTime.Now;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var query = context.Request.QueryString.Value;
            var cultureQuery = context.Request.Query["culture"];

            // Call the next delegate/middleware in the pipeline
            await this._next(context);

            // Check HttpResponse.HasStarted before overriding response.
            Trace.WriteLine($"{method} - {context.Response.StatusCode} - ({(int)(DateTime.Now - startTime).TotalMilliseconds}): {path}{query}");
        }
    }
}
