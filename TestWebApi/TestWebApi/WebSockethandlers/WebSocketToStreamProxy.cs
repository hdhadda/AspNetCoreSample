// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebSocketToStreamProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
// <summary>
// Defines the class that takes request from socket and dispatch the request to CIM and PowerShell stream.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.ManagementExperience.FrontEnd.WebSocketHandlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// This class sends the data read from a web socket into a CIM Stream.
    /// </summary>
    public class WebSocketToStreamProxy : IDisposable
    {
        /// <summary>
        /// Disposed flag.
        /// </summary>
        private bool disposed = false;
    
        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Operations object of websocket.
        /// </summary>
        private WebSocketOperations webSocketOperations;

        /// <summary>
        /// The transmit queue.
        /// </summary>
        private ConcurrentQueue<WriteDataObject> transmitQueue;

        /// <summary>
        /// The transmit request event.
        /// </summary>
        private AutoResetEvent transmitRequestEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketToStreamProxy"/> class.
        /// </summary>
        /// <param name="cimProxy">The CIM Proxy object.</param>
        public WebSocketToStreamProxy()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.transmitQueue = new ConcurrentQueue<WriteDataObject>();
            this.transmitRequestEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Send data back stream data to Websocket.
        /// </summary>
        /// <param name="completed">result state.</param>
        /// <param name="streamName">CIM | PowerShell</param>
        /// <param name="state">state of stream: data | error</param>
        /// <param name="data">data to transmit.</param>
        /// <param name="options">Reserved for future use.</param>
        public void TransmitData(bool completed, string streamName, WebSocketState state, object data, object options = null)
        {
            if (this.disposed)
            {
                // if it's already disposed, ignore data since WebSocket is not longer connected.
                return;
            }

            // wrap data in socket understood packet.
            var dataToTransmit = new
            {
                StreamName = streamName,
                State = (int)state,
                Data = data
            };
            var packet = new WriteDataObject { Completed = completed, Data = dataToTransmit };
            this.transmitQueue.Enqueue(packet);
            this.transmitRequestEvent.Set();
        }

        /// <summary>
        /// Processes the web socket context by establishing a two way directional communication
        /// with the target machine in the given node.
        /// </summary>
        /// <param name="socketOperations">The object to use operate the web socket.</param>
        /// <returns>A Task that is running while the web socket are open.</returns>
        public async Task ProcessWebSocketRequestAsync(WebSocketOperations socketOperations)
        {
            if (this.webSocketOperations != null)
            {
                throw new InvalidOperationException("Cannot process a socket context when another has already being processed. Create a new instance of the WebSocketToCimStreamHandler");
            }

            this.webSocketOperations = socketOperations;
            Exception exception = null;

            try
            {
                // start a task to listen and transmit on the webSocket
                Task webSocketReadTask = Task.Run(this.ReadFromWebSocket);
                Task webSocketWriteTask = Task.Run(this.WriteToWebSocket);

                // Wait for both tasks to complete
                await Task.WhenAll(webSocketReadTask, webSocketWriteTask);
            }
            catch (SocketException e)
            {
                // TODO: Rethink on the Exception handling story, specifically as we are not waiting on response Tasks to finish.
                exception = e;
                Trace.TraceError(string.Format("Socket Exception. Details: {0}", e.ToString()));
            }
            catch (Exception e)
            {
                exception = e;
                Trace.TraceError(string.Format("Unexpected error processing data during a connection to node: {0}", e.ToString()));
            }
            finally
            {
                // In any case, we always dispose this handler and send a cancel request to all the running tasks
                if (exception != null)
                {
                    exception = exception.GetBaseException();
                    this.CloseWebSocket(
                        WebSocketCloseStatus.InternalServerError,
                        "UnexpectedTcpErrorWebSocketCloseDescription");
                }

                this.CancelOperations();
            }
        }

        /// <summary>
        /// Transmit data back to WEBSOCKET.
        /// </summary>
        /// <summary>
        /// Method that handles when the remote client closes the web socket
        /// </summary>
        /// <param name="clientCloseStatus">
        /// The close status
        /// </param>
        /// <param name="clientCloseDescription">
        /// The description for closing the socket
        /// </param>
        internal void OnWebSocketClosed(WebSocketCloseStatus clientCloseStatus, string clientCloseDescription)
        {
            this.CloseWebSocket(clientCloseStatus, clientCloseDescription);
            this.CancelOperations();
        }

        /// <summary>
        /// Protected version of dispose method.
        /// </summary>
        /// <param name="disposing">The disposing state.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.cancellationTokenSource.Dispose();
                this.transmitRequestEvent.Dispose();
                this.CloseWebSocket(WebSocketCloseStatus.NormalClosure, "dispose");
            }

            this.disposed = true;
        }

        /// <summary>
        /// Closes the web socket with the given reason on the web socket closure
        /// </summary>
        /// <param name="closeStatus">the reason to close the socket</param>
        /// <param name="reason">The reason to close the socket</param>
        private void CloseWebSocket(WebSocketCloseStatus closeStatus, string reason)
        {
            this.webSocketOperations?.CloseAsync(closeStatus, reason, CancellationToken.None);
        }

        /// <summary>
        /// Write data back to Websocket.
        /// </summary>
        /// <param name="data">The data object to send.</param>
        /// <returns>Async task object.</returns>
        private async Task WriteData(object data)
        {
            var strings = JsonConvert.SerializeObject(data);
            var utf8buffer = Encoding.UTF8.GetBytes(strings);
            var buffer = new ArraySegment<byte>(utf8buffer);
            await this.webSocketOperations.SendAsync(buffer, WebSocketMessageType.Text, true, this.cancellationTokenSource.Token);
        }

        /// <summary>
        /// Task that reads from the web socket and writes to the TCP stream
        /// </summary>
        /// <returns>
        /// The task that is processing data that will terminate when either the TCP 
        /// connection or the web socket connection is completed
        /// </returns>
        private async Task ReadFromWebSocket()
        {
            // 4KB for a piece of packet.
            // 32KB for request packet size.
            const int MaxPacketSize = 4 * 1024;
            const int MaxTotalPacketSize = 32 * 1024;
            var buffer = new ArraySegment<byte>(new byte[MaxPacketSize]);
            var finalBuffer = new byte[MaxTotalPacketSize];
            var converter = new ExpandoObjectConverter();
            int position = 0;
            try
            {
                // !

                while (!this.cancellationTokenSource.IsCancellationRequested && this.webSocketOperations.IsOpen)
                {
                    WebSocketReceiveResult result = await this.webSocketOperations.ReceiveAsync(buffer, this.cancellationTokenSource.Token);

                    // ToDo: Check state of received Item.!!!!
                    if(result.CloseStatus.HasValue)
                    {
                        this.OnWebSocketClosed(result.CloseStatus.Value, result.CloseStatusDescription);
                        break;
                    }

                    // If the packet is not text type(result.Item1 != 1), or the size is 0, just log and ignore it.
                    if (result.MessageType != WebSocketMessageType.Text || result.Count == 0)
                    {
                        Trace.TraceWarning($"Unexpected or empty packet received. Type: {result.MessageType}. Size: {result.Count}");
                        continue;
                    }

                    if (position + result.Count > MaxTotalPacketSize)
                    {
                        throw new ApplicationException("Exceeded expected max packet size.");
                    }

                    Array.Copy(buffer.Array, 0, finalBuffer, position, result.Count);
                    position += result.Count;
                    if (result.EndOfMessage) // Indicates if the message received is the end of the message
                    {
                        string requestPacket = Encoding.UTF8.GetString(finalBuffer, 0, position);

                        Array.Clear(finalBuffer, 0, MaxTotalPacketSize);
                        position = 0;
                        dynamic request = null;

                        // Wrap JSON Deserealization in try-catch.
                        try
                        {
                            request = JsonConvert.DeserializeObject<ExpandoObject>(requestPacket, converter);
                        }
                        catch (JsonException ex)
                        {
                            Trace.TraceError($"Unexpected error parsing json data : {requestPacket}. Faild with error: {ex.Message}");

                            // Don't close the socket on bad data.
                            continue;
                        }

                        if (request != null)
                        {
                            // Extract info from request object.
                            // Request Packet structure:
                            // {
                            //  streamName: 'SME-CIM' | 'SME-PowerShell' | 'System';
                            //  state: enum { Noop = 1, Data = 2, Error = 3 }
                            //  data: { id: string, target: { nodeName: string, headers: object }, request: object }
                            //  options ?: any;
                            // }

                            if (request != null)
                            {
                                Trace.TraceError($"Reached Fine : ");
                            }
                            else
                            {
                                throw new ApplicationException("Unsupported request packet received.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Unexpected error processing data : {e.ToString()}");
            }
            finally
            {
                this.CancelOperations();
            }
        }

        /// <summary>
        /// Task that writes to the web socket.
        /// </summary>
        /// <returns>
        /// The task that is responding data that will terminate when the web socket connection is completed.
        /// </returns>
        private async Task WriteToWebSocket()
        {
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                if (!this.webSocketOperations.IsOpen)
                {
                    break;
                }

                WriteDataObject packet;
                if (this.transmitQueue.TryDequeue(out packet))
                {
                    await this.WriteData(packet.Data);
                }
                else
                {
                    this.transmitRequestEvent.WaitOne(1000);
                }
            }
        }

        /// <summary>
        /// Cancels all pending operations by triggering the Cancellation Token
        /// </summary>
        private void CancelOperations()
        {
            if (!this.cancellationTokenSource.IsCancellationRequested)
            {
                this.cancellationTokenSource.Cancel(false);
            }
        }

        /// <summary>
        /// Data object to send back to Websocket.
        /// </summary>
        private class WriteDataObject
        {
            /// <summary>
            /// Gets or sets a value indicating whether completed.
            /// </summary>
            public bool Completed { get; set; }

            /// <summary>
            /// Gets or sets the data to sending back.
            /// </summary>
            public object Data { get; set; }
        }
    }
}
