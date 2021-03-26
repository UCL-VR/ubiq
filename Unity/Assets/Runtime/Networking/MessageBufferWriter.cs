using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace Ubiq.Networking
{
    public class MessageBufferWriter : IBufferWriter<byte>
    {
        public MessageBufferWriter(int size) // size is the max size that will be passed to Send, so either desired packet size or network buffer size
        {
            maxsize = size;
            datagrams = new Queue<Datagram>();
            pool = new ConcurrentBag<Datagram>();
            datagram = GetDatagram();
        }

        private int maxsize;
        private ConcurrentBag<Datagram> pool;
        private Queue<Datagram> datagrams; // after finish is called, collect the datagrams from here and clear the list

        private class Datagram : ReferenceCountedMessage
        {
            public int end; // the end of the last atomic message that must be kept in one piece
            public int referenceCount;
            public ConcurrentBag<Datagram> pool;

            public Datagram(ConcurrentBag<Datagram> pool, int buffersize)
            {
                this.pool = pool;
                referenceCount = 0;
                bytes = new byte[buffersize];
            }

            public override void Acquire()
            {
                Interlocked.Increment(ref referenceCount);
            }

            public override void Release()
            {
                var referenceCount = Interlocked.Decrement(ref this.referenceCount);

                // use the return value of Decrement, because otherwise another thread may call Release() between the
                // decrement above, and the check below, causing the datagram to be added to the pool twice (or more).

                if (referenceCount < 0)
                {
                    throw new InvalidOperationException();
                }
                if (referenceCount == 0)
                {
                    start = 0;
                    length = 0;
                    end = 0;
                    pool.Add(this);
                }
            }
        }

        public bool HasMessages
        {
            get
            {
                return datagrams.Count > 0;
            }
        }

        public ReferenceCountedMessage Pop()
        {
            return datagrams.Dequeue();
        }

        private Datagram datagram; // the current datagram we are writing

        public void Advance(int count)
        {
            datagram.length += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckCapacity(sizeHint);
            return new Memory<byte>(datagram.bytes, datagram.start + datagram.length, datagram.bytes.Length - (datagram.start + datagram.length));
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckCapacity(sizeHint);
            return new Span<byte>(datagram.bytes, datagram.start + datagram.length, datagram.bytes.Length - (datagram.start + datagram.length));
        }

        private Datagram GetDatagram()
        {
            // try and get a buffer from the pool, or create one if none are available
            // based on https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool

            Datagram buffer;
            if(!pool.TryTake(out buffer))
            {
                buffer = new Datagram(pool, maxsize);
            }
            buffer.Acquire();
            return buffer;
        }

        private void CheckCapacity(int required)
        {
            if (required > maxsize)
            {
                throw new OutOfMemoryException();
            }

            if(required <= 0)
            {
                required = 1; // BufferWriter must always return a non-zero block
            }

            // More data for the current discrete message is incoming.
            // Do we have enough space in the current datagram? If so we continue as normal.
            // If not, we need to make a new one: copy the data since the last Next() call into the subsequent packet, and return that one.

            var used = (datagram.start + datagram.length);
            var remaining = datagram.bytes.Length - used;

            if (remaining < required)
            {
                if(datagram.end == 0)
                {
                    datagram.end = datagram.length; // if the boundary is never set, then just keep filling datagrams to the top
                }

                var amountToMove = datagram.length - datagram.end;

                var nextgram = GetDatagram();

                if (amountToMove > 0)
                {
                    Array.Copy(datagram.bytes, datagram.end, nextgram.bytes, nextgram.start, amountToMove);
                    nextgram.length += amountToMove;
                    nextgram.end = nextgram.start + nextgram.length;

                    datagram.length -= amountToMove;
                }

                datagrams.Enqueue(datagram);

                datagram = nextgram;
            }
        }

        public void Finish()
        {
            datagrams.Enqueue(datagram);
            datagram = GetDatagram();
        }

        /// <summary>
        /// Marks the end of an atomic message. Messages between two Next calls, or Next and Finish, will not be split across packets.
        /// </summary>
        public void Next()
        {
            datagram.end = datagram.start + datagram.length;
        }
    }
}