using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Ubiq.Networking
{
#if UNITY_WEBGL && !UNITY_EDITOR
    public class WebSocketConnection : JsWebSocketConnection
    {
        public WebSocketConnection(ConnectionDefinition definition):base(definition)
        {
        }
    }
#else
    public class WebSocketConnection : NativeWebSocketConnection
    {
        public WebSocketConnection(ConnectionDefinition definition):base(definition)
        {
        }
    }
#endif

#if UNITY_WEBGL
    public class JsWebSocketConnection : INetworkConnection
    {
        [DllImport("__Internal")]
        public static extern bool JsWebSocketPlugin_TryConnect(string url);

        [DllImport("__Internal")]
        public static extern bool JsWebSocketPlugin_IsConnecting();

        [DllImport("__Internal")]
        private static extern bool JsWebSocketPlugin_IsOpen();

        [DllImport("__Internal")]
        private static extern bool JsWebSocketPlugin_IsClosing();

        [DllImport("__Internal")]
        private static extern bool JsWebSocketPlugin_IsClosed();

        [DllImport("__Internal")]
        public static extern int JsWebSocketPlugin_Send(byte[] data, int start, int length);

        [DllImport("__Internal")]
        public static extern int JsWebSocketPlugin_Receive(byte[] data, int offset, int length);

        [DllImport("__Internal")]
        public static extern void JsWebSocketPlugin_Close();

        public string uri = "wss://localhost:8080";

        private Queue<ReferenceCountedMessage> messagesToSend = new Queue<ReferenceCountedMessage>();
        private Queue<ReferenceCountedMessage> messagesReceived = new Queue<ReferenceCountedMessage>();

        private System.Collections.IEnumerator sendEnumerator;
        private System.Collections.IEnumerator recvEnumerator;

        public JsWebSocketConnection(ConnectionDefinition def)
        {
            uri = string.Format("wss://{0}:{1}", def.sendToIp, def.sendToPort);

            if (!JsWebSocketPlugin_TryConnect(uri))
            {
                Debug.Log("WS connection failed");
            }
            else
            {
                Debug.Log("WS connected successful");
            }

            recvEnumerator = Recv();
            sendEnumerator = Send();
        }

        int _lastSendFrameCount = -1;

        public void Send(ReferenceCountedMessage message)
        {
            messagesToSend.Enqueue(message);

            if (Time.frameCount > _lastSendFrameCount)
            {
                sendEnumerator.MoveNext();
                _lastSendFrameCount = Time.frameCount;
            }
        }

        public ReferenceCountedMessage Receive()
        {
            recvEnumerator.MoveNext();
            return messagesReceived.Count > 0
                ? messagesReceived.Dequeue()
                : null;
        }

        private System.Collections.IEnumerator Send()
        {
            while (true)
            {
                if (messagesToSend.Count == 0)
                {
                    yield return null;
                    continue;
                }

                var message = messagesToSend.Dequeue();
                var lenBytes = BitConverter.GetBytes(message.length);

                do
                {
                    var buf = new byte[lenBytes.Length + message.length];
                    Array.Copy(lenBytes, buf, lenBytes.Length);
                    Array.Copy(message.bytes, message.start, buf, lenBytes.Length, message.length);
                    var sent = JsWebSocketPlugin_Send(buf, 0, buf.Length);
                    if (sent == 0)
                    {
                        yield return null;
                    }
                    else if (sent < 0)
                    {
                        yield break;
                    }
                    else
                    {
                        break;
                    }
                } while (true);

                message.Release();
            }
        }

        private System.Collections.IEnumerator Recv()
        {
            var buffer = new byte[4];

            while (true)
            {
                while (JsWebSocketPlugin_Receive(buffer, 0, 4) <= 0)
                {
                    // No messages waiting
                    yield return null;
                }

                // Message waiting, but might not have arrived yet
                int len = BitConverter.ToInt32(buffer, 0);
                var message = MessagePool.Shared.Rent(len);
                int received = 0;
                do
                {
                    var receive = JsWebSocketPlugin_Receive(message.bytes, message.start + received, len);
                    if (receive == 0)
                    {
                        // Not arrived yet
                        yield return null;
                    }
                    if (receive < 0)
                    {
                        // Socket closed
                        yield break;
                    }

                    received += receive;
                }
                while ((message.length - received) > 0);

                messagesReceived.Enqueue(message);
            }
        }

        public void Dispose()
        {
            JsWebSocketPlugin_Close();
        }
    }
#endif

    public class NativeWebSocketConnection : INetworkConnection
    {
        public string uri = "ws://localhost:8080";

        private ClientWebSocket websocket;
        private BlockingCollection<ReferenceCountedMessage> messagesToSend;
        private JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage> messagesReceived = new JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage>();

        public NativeWebSocketConnection(ConnectionDefinition def)
        {
            websocket = new ClientWebSocket();
            websocket.Options.SetBuffer(10000, 256);
            messagesToSend = new BlockingCollection<ReferenceCountedMessage>();
            uri = string.Format("ws://{0}:{1}", def.sendToIp, def.sendToPort);
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