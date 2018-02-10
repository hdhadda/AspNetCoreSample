
namespace TestWebApi.Filters
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using System;
    using System.Security.Principal;

    public class PrivilegedAccessAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _someFilterParameter;

        public PrivilegedAccessAttribute(string someFilterParameter = "test")
        {
            _someFilterParameter = someFilterParameter;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                // it isn't needed to set unauthorized result 
                // as the base class already requires the user to be authenticated
                // this also makes redirect to a login page work properly

                // Removed base, so handle here.
                context.Result = new UnauthorizedResult();
                return;
            }

            // you can also use registered services
            var someService = context.HttpContext.RequestServices;

            var isAuthorized = someService != null;
            if (!isAuthorized)
            {
                context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden); return;
            }
            else
            {
                context.Result = new ContentResult(){Content = "Resource unavailable - header should not be set"};
            }
        }
    }
}
