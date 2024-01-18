using System;
using System.Collections.Generic;
using System.Text;

namespace Ubiq.Networking
{
    namespace JmBucknall.Structures
    {

        internal class SingleLinkNode<T>
        {
            // Note; the Next member cannot be a property since it participates in
            // many CAS operations
            public SingleLinkNode<T> Next;
            public T Item;
        }
    }
}