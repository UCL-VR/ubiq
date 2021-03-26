using Ubiq.Networking;
using Ubiq.Networking.JmBucknall.Structures;
using Pixiv.Webrtc;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ubiq.WebRtc
{
    public class WebRtcDataChannel : MonoBehaviour, IDataChannelObserver, INetworkConnection
    {
        private WebRtcPeerConnection pc;
        private DisposableDataChannelInterface channel;
        private LockFreeQueue<ReferenceCountedMessage> received;
        private MessagePool pool;
        private int id;

        public string label;

        public enum Type
        {
            Remote,
            Unreliable,
            Ordered,
            Reliable
        }

        public Type type = Type.Unreliable;

        private void Awake()
        {
            pool = new MessagePool();
            received = new LockFreeQueue<ReferenceCountedMessage>();
            pc = GetComponentInParent<WebRtcPeerConnection>();
            id = -1;
        }

        // Start is called before the first frame update
        void Start()
        {
            // id determines whether the channel is considered pre-negotiated.
            int id = this.id;
            switch (type)
            {
                case Type.Unreliable:
                    pc.CreateDataChannel(label, "", id, false, 0, OnDataChannelCreated);
                    break;
                case Type.Ordered:
                    pc.CreateDataChannel(label, "", id, true, 0, OnDataChannelCreated);
                    break;
                case Type.Reliable:
                    pc.CreateDataChannel(label, "", id, true, -1, OnDataChannelCreated);
                    break;
                case Type.Remote:
                    // wait for the pc to call
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Tests if this Component instance could be the counterpart of the Data Channel
        /// </summary>
        public bool Matches(DisposableDataChannelInterface potential)
        {
            return (this.label == potential.Label() && this.type == Type.Remote);
        }

        public void OnDataChannelCreated(DisposableDataChannelInterface channel)
        {
            this.channel = channel;
            this.label = channel.Label();
            channel.RegisterObserver(this);
        }

        private void OnDestroy()
        {
            //channel.UnRegisterObserver(); dont do this if the threads are already torn down!
        }

        public void OnStateChange()
        {
            Debug.Log("OnStateChange");
        }

        public void OnMessage(bool binary, IntPtr data, int size)
        {
            if (binary)
            {
                var msg = pool.Rent(size);
                Marshal.Copy(data, msg.bytes, msg.start, size);
                received.Enqueue(msg);
            }
            else
            {
                Debug.LogException(new NotImplementedException());
            }
        }

        public void OnBufferedAmountChange(ulong amount)
        {
            Debug.Log("OnBufferedAmountChange");
        }

        public void OnObserverDestroyed()
        {
        }

        public ReferenceCountedMessage Receive()
        {
            return received.Dequeue();
        }

        public void Send(ReferenceCountedMessage m)
        {
            if (channel != null)
            {
                channel.Send(m.bytes, m.start, m.length);
            }
        }

        public void Dispose()
        {
        }
    }
}