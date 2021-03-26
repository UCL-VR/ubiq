using Pixiv.Rtc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using UnityEngine;

public class WebRtcThreads
{
    private static WebRtcThreads instance;

    private int count;
    public Dictionary<int, IThread> threads;
    private WebRtcThreads()
    {
        threads = ThreadTools.CreateThreadsBlocking();
    }

    public static WebRtcThreads Acquire()
    {
        if (instance == null)
        {
            instance = new WebRtcThreads();
        }
        instance.count++;
        return instance;
    }

    public void Release()
    {
        count--;
        if(count == 0)
        {
            foreach (var item in threads.Values)
            {
                try
                {
                    item.Quit();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            threads = null;
        }
    }
}


public class ThreadTools
{
    public static Dictionary<int, IThread> CreateThreadsBlocking()
    {
        // Do not use Rtc.Thread.Start because one of the threads may invoke
        // delegates, and Mono requires a thread interacting with managed
        // objects to be tracked by the runtime.
        //
        // api/DESIGN.md
        // > At the moment, the API does not give any guarantee on which
        // > thread* the callbacks and events are called on.
        //
        // Generational GC | Mono
        // https://www.mono-project.com/docs/advanced/garbage-collector/sgen/
        // > The Mono runtime will automatically register all threads that
        // > are created from the managed world with the garbage collector.
        // > For developers embedding Mono it is important that they
        // > register with the runtime any additional thread they create
        // > that manipulates managed objects with mono_thread_attach.
        //
        // To make this work, create a managed thread, and inside the thread
        // wrap it using the native TheadManager which will give a native
        // object to pass to the native code.

        var threads = new Dictionary<int,IThread>();

        for (int i = 0; i < 3; i++)
        {
            var start = new ParameterizedThreadStart((threadid) =>
            {
                try
                {
                    var thread = ThreadManager.Instance.WrapCurrentThread();

                    try
                    {
                        lock (threads)
                        {
                            threads.Add((int)threadid, thread);
                        }
                        try
                        {
                            thread.Run();
                        }catch(Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    finally
                    {
                        ThreadManager.Instance.UnwrapCurrentThread();
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
            );
            var managedThread = new System.Threading.Thread(start);

            switch (i)
            {
                case 0:
                    managedThread.Name = "Unity WebRTC Network Thread [0]";
                    break;
                case 1:
                    managedThread.Name = "Unity WebRTC Worker Thread [1]";
                    break;
                case 2:
                    managedThread.Name = "Unity WebRTC Signalling Thread [2]";
                    break;
                default:
                    managedThread.Name = $"Unity WebRTC Unknown Thread [{i}]";
                    break;
            }

            managedThread.Start((object)i);
        }

        while (true)
        {
            lock (threads)
            {
                if (threads.Count >= 3)
                {
                    return threads;
                }
            }
        }
    }

}
