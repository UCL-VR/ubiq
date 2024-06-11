using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace Ubiq.Networking
{
    public interface INetworkConnectionServer : IDisposable
    {
        Action<INetworkConnection> OnConnection { set; }
    }

    /// <summary>
    /// Listens on a port persistently for incoming connection requests and creates TCPConnections for these.
    /// This is not required to create a single point-to-point connection, which can be done using only TCPConnection in server or client mode.
    /// Calling Dispose will not destroy created TCPConnections. If the callback is null, TCPConnections will still be created, but no references
    /// to them will exist.
    /// </summary>
    public class TCPServer : INetworkConnectionServer
    {
        private Socket serverSocket;

        public Action<INetworkConnection> OnConnection { get; set; }

        public IPEndPoint Endpoint;

        public TCPServer() : this(new IPEndPoint(IPAddress.Any, 0))
        {
        }

        public TCPServer(string ip, string port) : this(new IPEndPoint(Dns.GetHostEntry(ip).AddressList[0], int.Parse(port)))
        {
        }

        public TCPServer(IPEndPoint endpoint)
        {
            serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(endpoint);
            serverSocket.Listen(100);
            serverSocket.BeginAccept(OnAccept, null);
            Endpoint = serverSocket.LocalEndPoint as IPEndPoint;
        }

        private void OnAccept(IAsyncResult result)
        {
            try
            {
                var socket = serverSocket.EndAccept(result);
                var connection = new TCPConnection(socket);
                if (OnConnection != null)
                {
                    OnConnection(connection);
                }
            }
            catch (ObjectDisposedException)
            {
                // listening socket has been closed cleanly
                return;
            }

            serverSocket.BeginAccept(OnAccept, null);
        }

        public void Dispose()
        {
            serverSocket.Close();
        }
    }


    /// <summary>
    /// Creates a non-blocking, reliable, point-to-point connection with a single remote host.
    /// An instance can either wait for a remote connection, or initiate a remote connection. This must be specified at design time.
    /// An instance cannot do both, but either can be created first.
    /// Must be disposed of when finished with.
    /// </summary>
    public class TCPConnection : INetworkConnection
    {
        public EndPoint Endpoint;
        public bool ServerEnd;

        private string name; // for the exception message strings
        private Socket socket;
        private Thread recvthread;
        private Thread sendthread;
        private Thread shutdownmonitor;
        private Queue messagestosend = Queue.Synchronized(new Queue());
        private JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage> messagesreceived = new JmBucknall.Structures.LockFreeQueue<ReferenceCountedMessage>();
        private byte[] msgData = new byte[4]; // helper buffer to receive expected integers
        private MessagePool pool = new MessagePool();

        private const int WSAECONNREFUSED = 10061;
        private const int WSAESHUTDOWN = 10058;
        private const int WSAEINTR = 10004; // A blocking operation was interrupted by a call to WSACancelBlockingCall.

        /// <summary>
        /// A callback made each time a network message is received. If this is set/non-null, the messages are *not* added to the queue.
        /// Can be set and reset at runtime.
        /// Messages must still be Release()'d by the reciever of this callback.
        /// This is a single action, not an event, because the number of fanouts for events are unknown.
        /// </summary>
        public Action<ReferenceCountedMessage> OnMessageReceived = null;

        /// <summary>
        /// A callback made when the connction is closed for any reason. If an exception is thrown by any thread, it is passed to this
        /// callback first, before being thrown.
        /// Any exception thrown by this callback will be captured and logged only. It will not be re-thrown.
        /// If the exception argument is null, the connection closed cleanly.
        /// Once the connection is closed, the TCPConnection object should be disposed off, though the endpoint parameter will still be valid,
        /// in case the application wants to try and re-establish the connection.
        /// </summary>
        public Action OnConnectionClosed = null;


        /// <summary>
        /// The recv thread will block waiting for the remote host, but the send thread needs (for performance
        /// reasons) to block until there is data to send.
        /// The race condition to watch for regardless of what interlock is used, is that a message may be added
        /// to the shared queue, then the thread released, but the message does not appear in the shared queue in
        /// time before the released thread checks for it, and resumes waiting for another signal.
        /// This is why we use a locking queue, as opposed to the lock free queue used by receive, the latter which
        /// is not deterministic.
        /// </summary>
        private AutoResetEvent sendsignal = new AutoResetEvent(false);

        public TCPConnection(ConnectionDefinition definition)
        {
            switch (definition.type)
            {
                case ConnectionType.TcpClient:
                    ServerEnd = false;
                    Endpoint = ParseEndpoint(definition.sendToIp, definition.sendToPort);
                    break;
                case ConnectionType.TcpServer:
                    ServerEnd = true;
                    Endpoint = ParseEndpoint(definition.listenOnIp, definition.listenOnPort);
                    break;
                default:
                    throw new ArgumentException();
            }
            name = definition.ToString();
            socket = null;
            StartThreads();
        }

        private static EndPoint ParseEndpoint(string host, string port)
        {
            return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], int.Parse(port));
        }

        /// <summary>
        /// Connects as Client
        /// </summary>
        public TCPConnection(EndPoint endpoint)
        {
            Endpoint = endpoint;
            ServerEnd = false;
            name = Endpoint.ToString();
            StartThreads();
        }

        /// <summary>
        /// Creates a TCPConnection around an existing socket. The socket must be open.
        /// </summary>
        /// <param name="existing"></param>
        public TCPConnection(Socket existing)
        {
            socket = existing;
            Endpoint = socket.RemoteEndPoint;
            ServerEnd = false;
            name = Endpoint.ToString();
            socket = existing;
            StartThreads();
        }

        private void StartThreads()
        {
            recvthread = new Thread(ReceiveThreadFunction);
            sendthread = new Thread(SendThreadFunction);
            recvthread.Start(); // receive thread is the one that makes the connection; it starts the send thread when it has done so
            shutdownmonitor = new Thread(ShutdownMonitorThreadFunction);
            shutdownmonitor.Start();
            Debug.Log(name + " Created.");
        }

        public void Dispose()
        {
            Debug.Log(name + " Going Away");
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            sendthread.Interrupt();
            recvthread.Interrupt(); // this should raise an exception also for any pending accept or connect calls
            socket.Close();
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

        private void ReceiveBytesBlocking(byte[] buffer, int offset, int size)
        {
            int received = 0;
            do
            {
                int n = socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                received += n;
                if(n == 0)
                {
                    throw new SocketException(WSAESHUTDOWN); // when the remote socket has been closed Receive will immediately return 0
                }
            }
            while ((size - received) > 0);
        }

        /* This member is the entry point for the receive thread. This thread creates (or waits for) the connection
        to the remote counterpart, and then continually recieves complete messages into inbox until the socket
        is closed. The correct way to terminate this thread is to close the socket, which will end any waiting. */
        private void ReceiveThreadFunction()
        {
            // make sure this block surrounds the entire function, as otherwise exceptions may disappear.
            try
            {
                if (socket == null)
                {
                    if (ServerEnd)
                    {
                        ConnectAsServer();
                    }
                    else
                    {
                        ConnectAsClient();
                    }
                }

                // ready to start sending
                sendthread.Start();

                while (true)
                {
                    ReceiveBytesBlocking(msgData, 0, 4);
                    int msgLen = BitConverter.ToInt32(msgData, 0);

                    var msg = pool.Rent(msgLen);
                    ReceiveBytesBlocking(msg.bytes, msg.start, msg.length);

                    if (OnMessageReceived != null)
                    {
                        OnMessageReceived.Invoke(msg);
                    }
                    else
                    {
                        messagesreceived.Enqueue(msg);
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException e)
            {
                switch (e.ErrorCode)
                {
                    case WSAESHUTDOWN:
                    case WSAEINTR:
                        return;
                    default:
                        Debug.LogException(e);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                sendthread.Interrupt(); // if not already
            }
        }

        private void SendBytesBlocking(byte[] m, int offset, int size)
        {
            int sent = 0;
            do
            {
                sent += socket.Send(m, offset + sent, size - sent, SocketFlags.None);
            } while ((size - sent) > 0);
        }

        /* This member is the entry point for the send thread. As this connection may wait for a remote to connect,
        send will accept and queue messages until ready. This thread will block until signalled that a connection is
        made, or that there are new messages to transmit. */
        private void SendThreadFunction()
        {
            try
            {
                // send all the data
                do
                {
                    // waits until there is some data to send
                    sendsignal.WaitOne();

                    while (messagestosend.Count > 0)
                    {
                        ReferenceCountedMessage m = (ReferenceCountedMessage)messagestosend.Dequeue();
                        if (m == null)
                        {
                            throw new ThreadInterruptedException("Null packet interpreted as signal for thread shutdown.");  // exit thread if sent a null payload
                        }
                        SendBytesBlocking(BitConverter.GetBytes(m.length), 0, 4);
                        SendBytesBlocking(m.bytes, m.start, m.length);
                        m.Release();
                    }

                } while (true);
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                recvthread.Interrupt(); // if not already interrupted
            }
        }

        private void ShutdownMonitorThreadFunction()
        {
            switch (recvthread.ThreadState)
            {
                case ThreadState.Unstarted:
                    break;
                default:
                    recvthread.Join();
                    break;
            }

            switch (sendthread.ThreadState)
            {
                case ThreadState.Unstarted:
                    break;
                default:
                    sendthread.Join();
                    break;
            }

            try
            {
                Debug.Log(name + " Closed.");

                if (socket.Connected) // the threads have died for another reason...
                {
                    socket.Shutdown(SocketShutdown.Both);
                }

                if (OnConnectionClosed != null)
                {
                    OnConnectionClosed();
                }
            }
            catch(ObjectDisposedException)
            {
                // socket has been closed remotely
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ConnectAsClient()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            while (!socket.Connected)
            {
                try
                {
                    socket.Connect(Endpoint);
                }
                catch(SocketException e)
                {
                    if(e.ErrorCode == WSAECONNREFUSED)
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

        private void ConnectAsServer()
        {
            var server = new Socket(SocketType.Stream, ProtocolType.Tcp);
            server.Bind(Endpoint);
            server.Listen(1);
            socket = server.Accept();
            server.Close();
        }
    }
}