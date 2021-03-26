using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ubiq.Networking
{
    namespace JmBucknall.Threading
    {
        public class WaitableThread : WaitHandle
        {
            private ManualResetEvent signal;
            private ThreadStart startThread;
            private Thread thread;

            public WaitableThread(ThreadStart start)
            {
                this.startThread = start;
                this.signal = new ManualResetEvent(false);
                this.SafeWaitHandle = signal.SafeWaitHandle;
                this.thread = new Thread(new ThreadStart(ExecuteDelegate));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    signal.Close();
                    this.SafeWaitHandle = null;
                }
                base.Dispose(disposing);
            }

            public void Abort()
            {
                thread.Abort();
            }

            public void Join()
            {
                thread.Join();
            }

            public void Start()
            {
                thread.Start();
            }

            private void ExecuteDelegate()
            {
                signal.Reset();
                try
                {
                    startThread();
                }
                finally
                {
                    signal.Set();
                }
            }

        }
    }
}