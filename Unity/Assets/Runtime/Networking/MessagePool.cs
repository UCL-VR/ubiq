using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Ubiq.Networking
{
    /// <summary>
    /// The
    /// </summary>
    public class MessagePool
    {
        // modelled on the MessageBufferWriter and this example: https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool

        private ArrayPool<byte> pool;
        private ConcurrentBag<MessagePoolMessage> bag;

        public MessagePool()
        {
            pool = ArrayPool<byte>.Shared;
            bag = new ConcurrentBag<MessagePoolMessage>();
        }

        public static MessagePool Shared
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new MessagePool();
                }
                return singleton;
            }
        }

        private static MessagePool singleton;

        private class MessagePoolMessage : ReferenceCountedMessage
        {
            private int referenceCount;
            private MessagePool pool;

            public MessagePoolMessage(MessagePool pool)
            {
                this.pool = pool;
            }

            public override void Acquire()
            {
                Interlocked.Increment(ref referenceCount);
            }

            public override void Release()
            {
                var referenceCount = Interlocked.Decrement(ref this.referenceCount);

                // use the return value of Decrement to avoid a race condition where another thread
                // calls Release here, changing the value from 1 to 0 before the check below, and causing
                // both threads to add it to the pool

                if (referenceCount < 0)
                {
                    throw new InvalidOperationException();
                }
                if (referenceCount == 0)
                {
                    pool.Return(this);
                }
            }
        }

        private ReferenceCountedMessage RentMessage()
        {
            MessagePoolMessage message;
            if (!bag.TryTake(out message))
            {
                message = new MessagePoolMessage(this);
            }
            message.Acquire();
            return message;
        }

        private byte[] RentBuffer(int length)
        {
            var bytes = pool.Rent(length);
            return bytes;
        }

        private void Return(ReferenceCountedMessage message)
        {
            // note the order of returns!
            bool clear = false;
#if DEBUG
            clear = true;
#endif
            pool.Return(message.bytes, clear);
            message.bytes = null;
            message.length = 0;
            bag.Add(message as MessagePoolMessage); // private method will only ever be called from within MessagePoolMessage
        }

        /// <summary>
        /// Returns a message with a valid bytes array member of the requested length. The start and length members will be initialised and valid, but the contents of bytes will be undefined.
        /// Acquire will have already been called on this message. When the message is finished with, call Release() on the message itself.
        /// </summary>
        public ReferenceCountedMessage Rent(int length)
        {
            // marry a block of bytes to a message
            var container = RentMessage();
            container.bytes = RentBuffer(length);
            container.start = 0;
            container.length = length;
            return container;
        }
    }
}