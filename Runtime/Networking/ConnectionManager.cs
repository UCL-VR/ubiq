using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Collections.Concurrent;
using Ubiq.Messaging;
using UnityEngine;
using System;

namespace Ubiq.Networking
{
    [RequireComponent(typeof(NetworkScene))]
    public class ConnectionManager : MonoBehaviour
    {
        public List<ConnectionDefinition> connections;

        private List<Server> servers;
        private ConcurrentQueue<Action> tasks;

        private void Awake()
        {
            servers = new List<Server>();
            tasks = new ConcurrentQueue<Action>();
        }

        void Start()
        {
            CreateConnections(); // create the connections defined at design time
        }

        private void Update()
        {
            Action task;
            tasks.TryDequeue(out task);
            if(task != null)
            {
                task();
            }
        }

        public IEnumerable<ConnectionDefinition> PublicUris
        {
            get
            {
                return servers.Select(s => s.definition);
            }
        }

        public class Server
        {
            public INetworkConnectionServer server;
            public ConnectionDefinition definition;
        }

        public void Connect(ConnectionDefinition remote)
        {
            switch (remote.type)
            {
                case ConnectionType.TcpServer:
                    {
                        OnConnection(new TCPConnection(new ConnectionDefinition()
                        {
                            type = ConnectionType.TcpClient, sendToIp = remote.listenOnIp, sendToPort = remote.listenOnPort
                        }));
                    }
                    break;
            }
        }

        private void OnConnection(INetworkConnection connection)
        {
            var router = GetComponent<NetworkScene>();
            if(router)
            {
                router.AddConnection(connection);
            }
        }

        /// <summary>
        /// If any of the connection definitions require that we create listening sockets, open them here.
        /// </summary>
        private void CreateConnections()
        {
            foreach (var item in connections)
            {
                switch (item.type)
                {
                    case ConnectionType.TcpServer:
                        {
                            var server = new TCPServer(item.listenOnIp, item.listenOnPort);
                            server.OnConnection = (connection) =>
                            {
                                tasks.Enqueue(() =>
                                {
                                    OnConnection(connection);
                                });
                            };
                            item.listenOnIp = server.Endpoint.Address.ToString();
                            item.listenOnPort = server.Endpoint.Port.ToString();
                            servers.Add(new Server()
                            {
                                server = server,
                                definition = item
                            });
                        }
                        break;
                }
            }
        }
    }
}