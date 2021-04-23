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

    [Serializable]
    public enum ConnectionType : int
    {
        tcp_client,
        tcp_server,
        udp,
        websocket,
    };

    [Serializable]
    public class ConnectionDefinition
    {
        public string listen_on_ip;
        public string listen_on_port;
        public string send_to_ip;
        public string send_to_port;
        public ConnectionType type;

        public override string ToString()
        {
            switch (type)
            {
                case ConnectionType.tcp_client:
                case ConnectionType.websocket:
                    return string.Format("{0}://{1}:{2}", Connections.Protocol(type), send_to_ip, send_to_port);
                case ConnectionType.tcp_server:
                    return string.Format("{0}://{1}:{2}", Connections.Protocol(type), listen_on_ip, listen_on_port);
                case ConnectionType.udp:
                    return string.Format("{0}://{1}:{2}:{3}:{4}", Connections.Protocol(type), send_to_ip, send_to_port, listen_on_ip, listen_on_port); //todo: annoying!!!
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public static class Connections
    {
        public static string Protocol(ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.tcp_client:
                    return "tcp";
                case ConnectionType.tcp_server:
                    return "tcps";
                case ConnectionType.udp:
                    return "udp";
                case ConnectionType.websocket:
                    return "ws";
                default:
                    throw new NotImplementedException();
            }
        }

        public static INetworkConnection Resolve(ConnectionDefinition definition)
        {
            INetworkConnection connection;
            switch (definition.type)
            {
                case ConnectionType.tcp_client:
                case ConnectionType.tcp_server:
                    connection = new TCPConnection(definition);
                    break;
                case ConnectionType.udp:
                    connection = new UDPConnection(definition);
                    break;
                case ConnectionType.websocket:
                    connection = new WebSocketConnection(definition);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return connection;
        }
    }

}