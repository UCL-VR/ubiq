using System.Net.WebSockets;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

namespace Ubiq.Networking
{
    public class WebSocketConnection : INetworkConnection
    {
        public string uri = "ws://localhost:8080";

        private ClientWebSocket websocket;
        private BlockingCollection<ReferenceCountedMessage> messagesToSend;
        private JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage> messagesReceived = new JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage>();

        public WebSocketConnection(ConnectionDefinition def)
        {
            websocket = new ClientWebSocket();
            websocket.Options.SetBuffer(10000, 256);
            messagesToSend = new BlockingCollection<ReferenceCountedMessage>();
            uri = string.Format("ws://{0}:{1}", def.send_to_ip, def.send_to_port);
            Task.Run(WebsocketConnect);
        }

        private async void WebsocketConnect()
        {
            try
            {
                await websocket.ConnectAsync(new Uri(uri), CancellationToken.None);
#pragma warning disable CS4014 // (Because this call is not awaited, execution of the current method continues before the call is completed)
                Task.Run(WebsocketReceiver);
                Task.Run(WebsocketSender);
#pragma warning restore CS4014
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Send(ReferenceCountedMessage message)
        {
            messagesToSend.Add(message);
        }

        private async void WebsocketSender()
        {
            try
            {
                while (true)
                {
                    var message = messagesToSend.Take();
                    await websocket.SendAsync(new ArraySegment<byte>(BitConverter.GetBytes(message.length)), WebSocketMessageType.Binary, false, CancellationToken.None);
                    await websocket.SendAsync(new ArraySegment<byte>(message.bytes, message.start, message.length), WebSocketMessageType.Binary, true, CancellationToken.None);
                    message.Release();
                }
            }
            catch (InvalidOperationException)
            {
                return; // An InvalidOperationException means that Take() was called on a completed collection
            }
            catch(Exception e)
            {
                Debug.LogException(e); // Otherwise Unity eats it.
            }
        }

        private async void WebsocketReceiver()
        {
            try
            {
                while (true)
                {
                    var buffer = new byte[4];
                    var array = new ArraySegment<byte>(buffer);
                    var receive = await websocket.ReceiveAsync(array, CancellationToken.None);

                    if (receive.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    int len = BitConverter.ToInt32(buffer, 0);
                    var message = MessagePool.Shared.Rent(len);
                    int received = 0;
                    do
                    {
                        receive = await websocket.ReceiveAsync(new ArraySegment<byte>(message.bytes, message.start + received, message.length - received), CancellationToken.None);
                        received += receive.Count;

                        if (receive.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }
                    }
                    while ((message.length - received) > 0);

                    if (websocket.CloseStatus != null)
                    {
                        return;
                    }

                    if(!receive.EndOfMessage)
                    {
                        Debug.Log("Unexpected message fragmentation across WebSocket frames.");
                    }

                    messagesReceived.Enqueue(message);
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e); // Otherwise Unity eats this.
            }
        }

        public void Dispose()
        {
            messagesToSend.CompleteAdding();
            websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
        }

        public ReferenceCountedMessage Receive()
        {
            return messagesReceived.Dequeue();
        }
    }
}