using System;
using System.Collections.Generic;
using System.Text;
using Ubiq.Networking.JmBucknall.Threading;

namespace Ubiq.Networking
{
    namespace JmBucknall.Structures
    {

        public class LockFreeStack<T>
        {

            private SingleLinkNode<T> head;

            public LockFreeStack()
            {
                head = new SingleLinkNode<T>();
            }

            public void Push(T item)
            {
                SingleLinkNode<T> newNode = new SingleLinkNode<T>();
                newNode.Item = item;
                do
                {
                    newNode.Next = head.Next;
                } while (!SyncMethods.CAS<SingleLinkNode<T>>(ref head.Next, newNode.Next, newNode));
            }

            public bool Pop(out T item)
            {
                SingleLinkNode<T> node;
                do
                {
                    node = head.Next;
                    if (node == null)
                    {
                        item = default(T);
                        return false;
                    }
                } while (!SyncMethods.CAS<SingleLinkNode<T>>(ref head.Next, node, node.Next));
                item = node.Item;
                return true;
            }

            public T Pop()
            {
                T result;
                Pop(out result);
                return result;
            }
        }
    }
}