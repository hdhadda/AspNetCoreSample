namespace TestWebApi.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ManagementExperience.FrontEnd.WebSocketHandlers;
    using System.Threading.Tasks;

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
        public async Task Get(string message)
        {
            var context = this.HttpContext;
            if (context.WebSockets.IsWebSocketRequest)
            {
                var handler = new WebSocketOperations(context);
                var webSocketStreamProxy = new WebSocketToStreamProxy();
                await handler.AcceptAndProcessAsync(webSocketStreamProxy.ProcessWebSocketRequestAsync, webSocketStreamProxy.OnWebSocketClosed);

                if(!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                }                
            }
            else
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
        }
    }
}
