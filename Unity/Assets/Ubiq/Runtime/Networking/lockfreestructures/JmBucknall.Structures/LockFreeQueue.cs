using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ubiq.Networking.JmBucknall.Threading;

namespace Ubiq.Networking
{
    namespace JmBucknall.Structures
    {
        public class LockFreeQueue<T> : IEnumerable<T>
        {
            SingleLinkNode<T> head;
            SingleLinkNode<T> tail;

            public LockFreeQueue()
            {
                head = new SingleLinkNode<T>();
                tail = head;
            }

            public void Enqueue(T item)
            {
                SingleLinkNode<T> oldTail = null;
                SingleLinkNode<T> oldTailNext;

                SingleLinkNode<T> newNode = new SingleLinkNode<T>();
                newNode.Item = item;

                bool newNodeWasAdded = false;
                while (!newNodeWasAdded)
                {
                    oldTail = tail;
                    oldTailNext = oldTail.Next;

                    if (tail == oldTail)
                    {
                        if (oldTailNext == null)
                            newNodeWasAdded = SyncMethods.CAS<SingleLinkNode<T>>(ref tail.Next, null, newNode);
                        else
                            SyncMethods.CAS<SingleLinkNode<T>>(ref tail, oldTail, oldTailNext);
                    }
                }

                SyncMethods.CAS<SingleLinkNode<T>>(ref tail, oldTail, newNode);
            }

            public bool Dequeue(out T item)
            {
                item = default(T);
                SingleLinkNode<T> oldHead = null;

                bool haveAdvancedHead = false;
                while (!haveAdvancedHead)
                {

                    oldHead = head;
                    SingleLinkNode<T> oldTail = tail;
                    SingleLinkNode<T> oldHeadNext = oldHead.Next;

                    if (oldHead == head)
                    {
                        if (oldHead == oldTail)
                        {
                            if (oldHeadNext == null)
                            {
                                return false;
                            }
                            SyncMethods.CAS<SingleLinkNode<T>>(ref tail, oldTail, oldHeadNext);
                        }

                        else
                        {
                            item = oldHeadNext.Item;
                            haveAdvancedHead =
                              SyncMethods.CAS<SingleLinkNode<T>>(ref head, oldHead, oldHeadNext);
                        }
                    }
                }
                return true;
            }

            public T Dequeue()
            {
                T result;
                Dequeue(out result);
                return result;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <summary>
            /// The Enumerator will walk the Queue from the Head at the time
            /// the Enumerator was requested. The Enumerator may show items
            /// that are dequeued during Enumeration.
            /// Calling Reset() on the Enumerator will reset the Head to the
            /// Queue's current Head (i.e. calling Reset multiple times will
            /// not necessarily reset the Enumerator to the same place).
            /// </summary>
            /// <remarks>
            /// The Enumerator uses the Next member. This member is always 
            /// valid (it does not lead or lag). The operation of the linked
            /// list means the Next chain is never broken even when dequeueing.
            /// This means the Enumerator could walk an arbitrarily long number
            /// of items that are already dequeued. However, it will always
            /// return to the valid list by the end.
            /// </remarks>
            internal struct Enumerator : IEnumerator<T>
            {
                public Enumerator(LockFreeQueue<T> queue)
                {
                    this.queue = queue;
                    current = queue.head;
                }

                LockFreeQueue<T> queue;
                SingleLinkNode<T> current;

                public T Current { get { return current.Item; } }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    current = current.Next;
                    return (current != null);
                }

                public void Reset()
                {
                    current = queue.head;
                }
            }

        }
    }
}