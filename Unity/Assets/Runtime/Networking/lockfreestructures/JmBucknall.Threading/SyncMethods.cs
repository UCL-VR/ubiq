using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ubiq.Networking
{

    namespace JmBucknall.Threading
    {
        public static class SyncMethods
        {

            public static bool CAS<T>(ref T location, T comparand, T newValue) where T : class
            {
                return
                    (object)comparand ==
                    (object)Interlocked.CompareExchange<T>(ref location, newValue, comparand);
            }
        }
    }
}