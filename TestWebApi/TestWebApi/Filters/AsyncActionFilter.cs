using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestWebApi.Controllers;

namespace TestWebApi.Filters
{
    public class AsyncActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controllerType = context.Controller.GetType();
            if (controllerType != typeof(EchoController))
            {
                throw new Exception();
            }
            var request = context.HttpContext.Request.Headers;

            var controller = context.Controller.ToString();
            var action = context.ActionDescriptor.Id;
            //Handle the request
            await next();
        }
    }
}
