using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Networking
{
    [Serializable]
    public enum ConnectionType : int
    {
        TcpClient,
        TcpServer,
        UDP,
        WebSocket,
    };

    [Serializable]
    public class PlatformConnectionDefinition
    {
        public RuntimePlatform platform;
        public ConnectionDefinition connection;
    }

    [Serializable]
    [CreateAssetMenu(fileName = "Connection", menuName = "Ubiq/Connection Definition", order = 1)]
    public class ConnectionDefinition : ScriptableObject
    {
        public string listenOnIp;
        public string listenOnPort;
        public string sendToIp;
        public string sendToPort;
        public ConnectionType type;

        public List<PlatformConnectionDefinition> platforms;

        public override string ToString()
        {
            switch (type)
            {
                case ConnectionType.TcpClient:
                case ConnectionType.WebSocket:
                    return string.Format("{0}://{1}:{2}", Connections.Protocol(type), sendToIp, sendToPort);
                case ConnectionType.TcpServer:
                    return string.Format("{0}://{1}:{2}", Connections.Protocol(type), listenOnIp, listenOnPort);
                case ConnectionType.UDP:
                    return string.Format("{0}://{1}:{2}:{3}:{4}", Connections.Protocol(type), sendToIp, sendToPort, listenOnIp, listenOnPort); //todo: annoying!!!
                default:
                    throw new NotImplementedException();
            }
        }

        public ConnectionDefinition()
        {
            platforms = new List<PlatformConnectionDefinition>();
        }

        public ConnectionDefinition(string tcpUri)
        {
            if (String.IsNullOrEmpty(tcpUri))
            {
                throw new ArgumentException($"Invalid Connection String {tcpUri}");
            }

            var tokens = tcpUri.Split(':');

            if (tokens.Length != 2)
            {
                throw new ArgumentException($"Invalid Connection String {tcpUri}");
            }

            sendToIp = tokens[0];
            sendToPort = tokens[1];
            type = ConnectionType.TcpClient;

            if (Uri.CheckHostName(sendToIp) == UriHostNameType.Unknown)
            {
                throw new ArgumentException($"Invalid Connection String {tcpUri}");
            }
            int temp = 0;
            if (!int.TryParse(sendToPort, out temp))
            {
                throw new ArgumentException($"Invalid Connection String {tcpUri}");
            }
        }
    }
}