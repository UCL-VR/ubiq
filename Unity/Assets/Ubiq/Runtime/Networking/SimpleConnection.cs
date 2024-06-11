﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Networking
{
    /// <summary>
    /// Represents a point-to-point connection to another endpoint. 
    /// The INetworkConnection is designed to be polled on the main Unity thread.
    /// Call Dispose to close the connection.
    /// </summary>
    public interface INetworkConnection : IDisposable
    {
        ReferenceCountedMessage Receive();
        void Send(ReferenceCountedMessage m);
    }

    /// <summary>
    /// The SimpleConnection component is a way of specifying a network connection in the Unity Editor during development.
    /// Attach this to the same GameObject as the Router, set the parameters, and the connection will be made on startup.
    /// </summary>
    public class SimpleConnection : MonoBehaviour
    {
        public string Name; // user friendly name
        public ConnectionDefinition def;

        void Start()
        {
            var connection = Connections.Resolve(def);
            foreach (var item in GetComponentsInChildren<NetworkScene>())
            {
                item.AddConnection(connection);
            }
        }
    }
}