using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Text;
using UnityEngine;

namespace Ubiq.Networking
{
    public interface IReferenceCounter
    {
        void Acquire();
        void Release();
    }

    /// <summary>
    /// A container for a discrete message sent over the network.
    /// Can re-use resources to avoid GC allocations. When additional copies of the message are created, Acquire must be called. When the message is finished with, Release must be called.
    /// NetMessages should be consumed as quickly as possible after receipt by a serialiser, to reduce the likelihood of forgetting to call Release.
    /// </summary>
    public class ReferenceCountedMessage // a class making it easier to implement reference counting methods
    {
        /// <summary>
        /// Buffer that contains the message payload. The message can be anywhere in the buffer. The buffer should not be copied or passed as an argument, as this will break reference counting. Buffer cannot be null.
        /// </summary>
        public byte[] bytes; // Byte buffers are used as opposed to Memory or Span, because the Socket class ultimately uses byte buffers for the Send and Receive calls.
        public int start;   //start and length specify where to find the message in the buffer. Length is always greater than zero.
        public int length;

        public virtual void Release()
        {
        }

        public virtual void Acquire()
        {
        }

        public ReferenceCountedMessage()
        {
        }

        public ReferenceCountedMessage(byte[] bytes)
        {
            this.bytes = bytes;
            this.start = 0;
            this.length = bytes.Length;
        }

        public override string ToString()
        {
            try
            {
                return Encoding.UTF8.GetString(bytes, start, length);
            }catch
            {
                return base.ToString();
            }
        }
    }

    public static class Connections
    {
        public static string Protocol(ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.TcpClient:
                    return "tcp";
                case ConnectionType.TcpServer:
                    return "tcps";
                case ConnectionType.UDP:
                    return "udp";
                case ConnectionType.WebSocket:
                    return "wss";
                default:
                    throw new NotImplementedException();
            }
        }

        public static INetworkConnection Resolve(ConnectionDefinition definition)
        {
            foreach (var item in definition.platforms)
            {
                if(item.platform == Application.platform)
                {
                    return Resolve(item.connection);
                }
            }

            INetworkConnection connection;
            switch (definition.type)
            {
                case ConnectionType.TcpClient:
                case ConnectionType.TcpServer:
                    connection = new TCPConnection(definition);
                    break;
                case ConnectionType.UDP:
                    connection = new UDPConnection(definition);
                    break;
                case ConnectionType.WebSocket:
                    connection = new WebSocketConnection(definition);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return connection;
        }
    }

}