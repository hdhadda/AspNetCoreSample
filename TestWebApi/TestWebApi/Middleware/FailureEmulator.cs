using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TestWebApi.Middleware
{
    public class FailureEmulator
    {
        /// <summary>
        /// In debug build, always fail 5% request.
        /// </summary>
        private const int failureRate = 3;

        /// <summary>
        /// Fail status codes.
        /// </summary>
        private HttpStatusCode[] failStatusCodes = new[]
        {
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.BadGateway,
            HttpStatusCode.NotFound
        };

        private static readonly Random random = new Random();

        private readonly RequestDelegate _next;

        public FailureEmulator(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (random.Next(0, 99) < failureRate)
            {
                // Check HttpResponse.HasStarted before overriding response.
                context.Response.Clear();
                context.Response.StatusCode = (int)this.failStatusCodes[random.Next(0, this.failStatusCodes.Length - 1)];
                await context.Response.WriteAsync("{ error: { code: 0, message: 'Intentional failure from the Gateway to test error handling.' } }").ConfigureAwait(false);
            }
            else
            {
                await this._next(context);
            }
        }
    }
}
