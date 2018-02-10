﻿namespace TestWebApi.Controllers
{
    using System.Globalization;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using TestWebApi.Extensions;
    using TestWebApi.Filters;
    using TestWebApi.Models;

    /// <summary>
    /// The echo controller.
    /// </summary>
    [Route("api/[controller]")]
    public class EchoController : ControllerBase
    {
        public EchoController()
        {
        }

        /// <summary>
        /// The echo test.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        [HttpGet("{*message}")]
        [PrivilegedAccess]
        // [Authorize(Policy = "BadgeEntry")]
        public string Get(string message)
        {
            var principal = this.User.getUserId();

            if (principal != null)
            {
                return "test failed";
            }

            return message ?? System.DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }
    }
}
