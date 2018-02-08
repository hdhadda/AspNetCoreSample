// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebSocketOperations.cs" company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// <summary>
//   Defines the class that operates a web socket using the OWIN web socket extensions
//   http://owin.org/spec/extensions/owin-WebSocket-Extension-v0.4.0.htm
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.ManagementExperience.FrontEnd.WebSocketHandlers
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Callback that processes the close status of a web socket
    /// </summary>
    /// <param name="clientCloseStatus">The close status passed by the client</param>
    /// <param name="clientCloseDescription">The close description passed by the client that closed the connection</param>
    public delegate void WebSocketClosedHandler(WebSocketCloseStatus clientCloseStatus, string clientCloseDescription);

    /// <summary>
    /// A class that handles a web socket request and the operations that can be performed on a webSocket. 
    /// Once instantiated calling the Accept method will complete the socket connection and start the processing of data. 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Design",
        "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "statusPollingTokenSource is disposed within ProcessSocketConnection() method.")]
    public class WebSocketOperations
    {
        /// <summary>
        /// The OnClient closed handler.
        /// </summary>
        private WebSocketClosedHandler onClientClosed = null;

        private WebSocket webSocket = null;

        /// <summary>
        /// The process web socket request async.
        /// </summary>
        private Func<WebSocketOperations, Task> processWebSocketRequestAsync;

        /// <summary>
        /// The processing task.
        /// </summary>
        private Task processingTask;

        /// <summary>
        /// The status polling token sources.
        /// </summary>
        private CancellationTokenSource statusPollingTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The WebSocket status polling task.
        /// </summary>
        private Task webSocketStatusPollingTask;

        /// <summary>
        /// The context that was used to create this instance of the web socket handler
        /// </summary>
        private HttpContext context;

        /// <summary>
        /// Initializes a new instance of the WebSocketOperations class
        /// </summary>
        /// <param name="context">The Http context to extract the web socket request</param>
        public WebSocketOperations(HttpContext context)
        {
            this.context = context;
            
        }

        /// <summary>
        /// Gets a value indicating whether this is a valid web socket request.
        /// </summary>
        public static bool IsWebSocketConnection(HttpContext context)
        {
            return context.WebSockets.IsWebSocketRequest;
        }

        /// <summary>
        /// Gets a value indicating whether the current webSocket is open or not
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this.webSocket.State == WebSocketState.Open;
            }
        }

        /// <summary>
        /// Accepts the current web socket request and start processing the request with the given callback
        /// </summary>
        /// <param name="processWebSocketRequestAsync">
        /// The callback to process the web socket request. This is an async function that takes the web socket operations
        /// instance so it can interact with the web socket
        /// </param>
        /// <param name="processWebSocketRequestAsync">
        /// The callback to process the web socket request. This is an async function that takes the web socket operations
        /// instance so it can interact with the web socket
        /// </param>
        /// <param name="onWebSocketClosed">Method to call when the remote client closed the web socket</param>
        /// <returns>A Task result.</returns>
        internal async Task AcceptAndProcessAsync(Func<WebSocketOperations, Task> processWebSocketRequestAsync, WebSocketClosedHandler onWebSocketClosed)
        {
            this.onClientClosed = onWebSocketClosed;
            this.webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await this.AcceptAsync(processWebSocketRequestAsync);
        }

        /// <summary>
        /// Accepts the Web Socket Connection and calls the <paramref name="processWebSocketRequestAsync"/>
        /// once the socket connection has been established
        /// </summary>
        /// <param name="processWebSocketRequestAsync">The function to call once the socket connection has been established</param>
        /// <returns>A task result.</returns>
        public async Task AcceptAsync(Func<WebSocketOperations, Task> processWebSocketRequestAsync)
        {
            this.processWebSocketRequestAsync = processWebSocketRequestAsync;
            await this.ProcessSocketConnection();
            await this.processingTask;
        }

        /// <summary>
        /// Asynchronously sends data on the web socket
        /// </summary>
        /// <param name="data">the data to send</param>
        /// <param name="messageType">the message type</param>
        /// <param name="endOfMessage">Indicates if this is the last frame of the current message</param>
        /// <param name="cancellationToken">The token to cancel the pending operation</param>
        /// <returns>The task result.</returns>
        public async Task SendAsync(ArraySegment<byte> data, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            // T1: data to be sent
            // T2:  message type to send: 1 = Text, 2 = Binary
            // T3: Indicates if this is the end of the message and can be sent over the wire right away
            // T4: The token to cancel the pending IO operation
            await webSocket.SendAsync(data, messageType, endOfMessage, cancellationToken);
        }

        /// <summary>
        /// Asynchronously receive data from the web socket
        /// </summary>
        /// <param name="buffer">The buffer to write the read data</param>
        /// <param name="cancellationToken">The token to cancel the pending operation</param>
        /// <returns>
        /// The result of the read operation once it completes
        /// </returns>
        public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return await webSocket.ReceiveAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Asynchronously closes the websocket
        /// </summary>
        /// <param name="closeStatus">The status of the closure</param>
        /// <param name="closeReason">The reason for closing the socket</param>
        /// <param name="cancellationToken">The token to cancel the pending operation</param>
        /// <returns>The task result.</returns>
        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string closeReason, CancellationToken cancellationToken)
        {
            if (closeStatus != WebSocketCloseStatus.NormalClosure)
            {
                closeReason = string.IsNullOrWhiteSpace(closeReason) ? "<No close reason provided>" : closeReason;
                Trace.TraceWarning(
                    $"Closing web socket connection with status {Enum.GetName(typeof(WebSocketCloseStatus), closeStatus)}: {closeReason}");
            }

            this.statusPollingTokenSource.Cancel();

            if (this.IsOpen)
            {
                // The status for closing the socket see System.Net.WebSockets.WebSocketCloseStatus
                // The reason for closing the socket
                // The token to cancel the pending IO operation
                await webSocket.CloseAsync(closeStatus, closeReason, cancellationToken);
            }
        }

        /// <summary>
        /// Processes the socket connection once it is accepted
        /// </summary>
        /// <param name="environment">The environment objects.</param>
        /// <returns>The task that is processing the web socket connection</returns>
        private async Task ProcessSocketConnection()
        {
            this.processingTask = this.processWebSocketRequestAsync(this);

            // Start a task to check the web socket status and if it is closed
            this.webSocketStatusPollingTask = Task.Run(
                async () =>
                {
                    while (true)
                    {
                        if (!this.IsOpen && this.onClientClosed != null)
                        {

                            WebSocketCloseStatus closeStatus = WebSocketCloseStatus.Empty;
                            string closeDescription = string.Empty;
                            this.onClientClosed(closeStatus, closeDescription);
                            break;
                        }
                        await Task.Delay(2000);
                    }
                },
                this.statusPollingTokenSource.Token)
                    .ContinueWith((task) => this.statusPollingTokenSource.Dispose());

            await this.processingTask;
        }
    }
}
