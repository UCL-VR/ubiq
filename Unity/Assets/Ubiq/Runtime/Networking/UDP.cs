using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
//using System.IO;


namespace Ubiq.Networking
{
    /// <summary>
    /// Creates a non-blocking, reliable, point-to-point connection with a single remote host.
    /// </summary>
    public class UDPConnection : INetworkConnection
    {
        public bool connected;
        private ConnectionDefinition def;
        private Socket socket;
        private Thread recvthread;
        private Thread sendthread;
        private Queue messagestosend = Queue.Synchronized(new Queue());
        private JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage> messagesreceived = new JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage>();
        private MessagePool pool = new MessagePool();

        private const int WSAECONNREFUSED = 10061;
        private const int WSAEINTR = 10004; // A blocking operation was interrupted by a call to WSACancelBlockingCall.
        private const int WSAECONNRESET = 10054;

        private static int UDP_MAX_DATAGRAM_SIZE = 65535; // defined by the size of the length field in the udp spec
        private static int HEADERSIZE = 4; // according to netmessage

        public class IncompleteMessageException : Exception
        {
            public IncompleteMessageException()
                :base("Connectionless socket Receive() call has returned too little data for a complete message. This should not be possible.")
            {
            }
        }

        /// <summary>
        /// The recv thread will block waiting for the remote host, but the send thread needs (for performance
        /// reasons) to block until there is data to send.
        /// The race condition to watch for regardless of what interlock is used, is that a message may be added
        /// to the shared queue, then the thread released, but the message is not visible to the second thread in
        /// time. This is why we use a locking queue, as opposed to the lock free queue used by receive, which is
        /// not deterministic.
        /// </summary>
        private AutoResetEvent sendsignal = new AutoResetEvent(false);

        public UDPConnection(ConnectionDefinition definition)
        {
            def = definition;
            recvthread = new Thread(ReceiveThreadFunction);
            recvthread.Start();
            sendthread = new Thread(SendThreadFunction);
            Debug.Log(def.ToString() + " Created.");
        }

        public void Dispose()
        {
            Debug.Log(def.ToString() + " Going Away");
            sendthread.Interrupt(); // the send thread should shut down gracefully on receiving the ThreadInterruptedException, or OjectDiposedException if it is sending
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            socket.Close(); // the threads should shut down gracefully on recieving the OjectDiposedException
        }

        public ReferenceCountedMessage Receive()
        {
            return messagesreceived.Dequeue();
        }

        public void Send(ReferenceCountedMessage m)
        {
            messagestosend.Enqueue(m);
            sendsignal.Set();
        }

        /// <summary>
        /// This member is the entry point for the receive thread. This thread creates (or waits for) the connection
        /// to the remote counterpart, and then continually recieves complete messages into inbox until the socket
        /// is closed.The correct way to terminate this thread is to close the socket, which will end any waiting.
        /// </summary>
        private void ReceiveThreadFunction()
        {
            byte[] buffer = new byte[UDP_MAX_DATAGRAM_SIZE]; // working memory for receiving udp packets. this is the max size according to udp. our max size may be smaller.

            // make sure this block surrounds the entire function, as otherwise exceptions may disappear.
            try
            {
                ConnectWithRemoteEndpoint();

                // ready to start sending
                sendthread.Start();

                while (true)
                {
                    /*
                     * If you are using a connectionless Socket, Receive will read the first queued datagram from the
                     * destination address you specify in the Connect method. If the datagram you receive is larger
                     * than the size of the buffer parameter, buffer gets filled with the first part of the message,
                     * the excess data is lost and a SocketException is thrown.
                     * https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receive?view=netframework-4.8
                     */

                    int receieved = socket.Receive(buffer);

                    if (receieved == 0)
                    {
                        break;  // the socket has been gracefully closed.
                    }

                    if(receieved < HEADERSIZE)
                    {
                        throw new IncompleteMessageException();
                    }

                    int size = Packing.GetInt(buffer, 0);

                    if(size != receieved - HEADERSIZE)
                    {
                        throw new IncompleteMessageException();
                    }

                    ReferenceCountedMessage m = pool.Rent(size);
                    Buffer.BlockCopy(buffer, HEADERSIZE, m.bytes, m.start, size);

                    messagesreceived.Enqueue(m);
                }
            }
            catch (ObjectDisposedException)
            {
                Debug.Log(def.ToString() + " Receive Shutdown.");
            }
            catch (SocketException e)
            {
                switch (e.ErrorCode)
                {
                    case WSAEINTR:
                        Debug.Log(def.ToString() + " Receive Shutdown");
                        return;
                    default:
                        Debug.LogException(e);
                        return;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// This member is the entry point for the send thread. As this connection may wait for a remote to connect,
        /// send will accept and queue messages until ready.This thread will block until signalled that a connection is
        /// made, or that there are new messages to transmit.
        /// </summary>
        private void SendThreadFunction()
        {
            byte[] buildBuffer = new byte[UDP_MAX_DATAGRAM_SIZE]; // working memory for building udp packets
            int maxPayloadSize = buildBuffer.Length - HEADERSIZE;

            try
            {
                do
                {
                    sendsignal.WaitOne();   // waits until there is some data to send

                    while (messagestosend.Count > 0)
                    {
                        ReferenceCountedMessage m = (ReferenceCountedMessage)messagestosend.Dequeue();

                        if (m.length == 0)
                        {
                            m.Release();
                            continue;   // don't send empty messages.
                        }

                        if (m.length > maxPayloadSize)
                        {
                            Debug.LogException(new Exception(string.Format("Message size {0} exceeds the maximum payload size of {1} bytes", m.length, maxPayloadSize)));
                            m.Release();
                            continue;
                        }

                        Packing.GetBytes(m.length, buildBuffer, 0);
                        Buffer.BlockCopy(m.bytes, m.start, buildBuffer, HEADERSIZE, m.length);
                        int datagramSize = HEADERSIZE + m.length;
                        int sent = socket.Send(buildBuffer, 0, datagramSize, SocketFlags.None);

                        if(sent != datagramSize)
                        {
                            Debug.LogException(new Exception("Socket.Send() sent the wrong number of bytes in UDP mode. This is unexpected."));
                        }

                        m.Release();
                    }

                } while (true);
            }
            catch(ThreadInterruptedException)
            {
                Debug.Log(def.ToString() + " Send Shutdown.");
                return;
            }
            catch(ObjectDisposedException)
            {
                Debug.Log(def.ToString() + " Send Shutdown.");
                return;
            }
            catch(Exception e)
            {
                // promote any unhandled exceptions up to Unity via the Debug Log
                Debug.LogError("Unhandled Exception in " + def.ToString());
                Debug.LogException(e);
            }
        }

        private void ConnectWithRemoteEndpoint()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // disables whether UDP PORT_UNREACHABLE messages are reported
            // https://docs.microsoft.com/en-us/windows/win32/winsock/winsock-ioctls
            // https://stackoverflow.com/questions/15228272

            // On Android this will result in an avc (SELinux) denial causing the socket creation to fail.

            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                    break;
            }

            IPEndPoint local_endpoint = new IPEndPoint(IPAddress.Parse(def.listenOnIp), int.Parse(def.listenOnPort));
            IPEndPoint remote_endpoint = new IPEndPoint(IPAddress.Parse(def.sendToIp), int.Parse(def.sendToPort));

            while (!connected)
            {
                try
                {
                    socket.Bind(local_endpoint);
                    socket.Connect(remote_endpoint);
                    connected = true;
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == WSAECONNREFUSED)
                    {
                        Thread.Sleep(250);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }


    }

}