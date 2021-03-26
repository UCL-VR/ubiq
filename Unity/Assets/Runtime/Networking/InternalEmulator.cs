using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Ubiq.TimeManagement;
using Ubiq.Messaging;

namespace Ubiq.Networking
{
    /// <summary>
    /// Creates a virtual point to point connection within the same application to another Internal component. The connection is threadsafe.
    /// </summary>
    public class InternalEmulator : MonoBehaviour
    {
        public string Name = "Internal Loopback";
        public float latency = 0;  // emulated latency in s
        public bool connected = true;

        private TimeManager time;
        private List<EmulatorQueue> queues;

        private class EmulatorQueue : INetworkConnection
        {
            private InternalEmulator emulator;
            public JmBucknall.Structures.LockFreeQueue<BufferedMessage> messages;

            public EmulatorQueue(InternalEmulator emulator)
            {
                this.emulator = emulator;
                messages = new JmBucknall.Structures.LockFreeQueue<BufferedMessage>();
            }

            public ReferenceCountedMessage Receive()
            {
                BufferedMessage b;
                if (messages.Dequeue(out b))
                {
                    if (b.timeToRecieveOn <= emulator.time.gameTime)
                    {
                        return b.msg;
                    }
                    else
                    {
                        messages.Enqueue(b);
                    }
                }
                return b.msg;
            }

            public void Send(ReferenceCountedMessage m)
            {
                emulator.Send(m, this);
            }

            public void Dispose()
            {
            }
        }


        struct BufferedMessage
        {
            public ReferenceCountedMessage msg;
            public long timeToRecieveOn;
        }

        private void Awake()
        {
            time = GetComponentInChildren<TimeManager>();
            queues = new List<EmulatorQueue>();
            foreach (var item in GetComponentsInChildren<NetworkScene>())
            {
                var queue = new EmulatorQueue(this);
                this.queues.Add(queue);
                item.AddConnection(queue);
            }
        }

        private void Send(ReferenceCountedMessage m, EmulatorQueue from)
        {
            if (connected)
            {
                BufferedMessage b;
                b.msg = m;
                b.timeToRecieveOn = time.gameTime + TimeManager.FromSeconds(latency);

                Profiler.BeginSample("Enqueue");
                foreach (var item in queues)
                {
                    if(item != from)
                    {
                        m.Acquire();
                        item.messages.Enqueue(b);
                    }
                }
                m.Release();
                Profiler.EndSample();
            }
            else
            {
                m.Release();
            }
        }
    }
}