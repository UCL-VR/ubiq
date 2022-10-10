using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Extensions;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Samples.UnitTests.Logging
{
    /// <summary>
    /// The LoggingDiagnostics Component is a stress test for the logging
    /// framework.
    /// The class generates log events, while also exercising the
    /// LogCollector options.
    /// The events are deterministic, meaning the cumulative log files can
    /// be analysed afterwards for integrity.
    /// </summary>
    public class LoggingDiagnostics : MonoBehaviour
    {
        /// <summary>
        /// Each event increments the counter by 1. If the counter does not
        /// increment continuously, in order with time, then a log event has
        /// been lost.
        /// </summary>
        protected int Counter;

        /// <summary>
        /// This class uses a number of LogEmitters. No matter which emitter is
        /// used, events should be written identically.
        /// </summary>
        protected List<LogEmitter> emitters = new List<LogEmitter>();

        protected bool Run = true;

        protected LogCollector Collector;

        private NetworkContext context;

        void Start()
        {
            context = NetworkScene.Register(this);
            int numEmitters = UnityEngine.Random.Range(1, 5);
            for (int i = 0; i < numEmitters; i++)
            {
                emitters.Add(new ExperimentLogEmitter(this));
            }
            Collector = LogCollector.Find(this);
        }

        // The LoggingDiagnostics will generate deterministic events from
        // arbitrary Emitters at random intervals.
        // Every so often, a Collector will also volunteer as the Active
        // Collector.

        // After all Peers have had some time to transmit logs and shutdown,
        // LoggingDiagnostics can check the log files to determine if any
        // events have gone missing.

        // As any LoggingDiagnostics session may volunteer as the active
        // collector, the group of processes must shutdown gracefully to
        // prevent missing events.
        // A brief timeout is added when the shutdown is requested to
        // allow all Components to finish transmitting before all existing
        // together.

        void Update()
        {
            if (Run)
            {
                if (UnityEngine.Random.value > 0.7f)
                {
                    var emitter = emitters[UnityEngine.Random.Range(0, emitters.Count)];
                    emitter.Log("Counter", Counter++);
                }

                if (UnityEngine.Random.value > 0.99f)
                {
                    Collector.StartCollection();
                }
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            if(message.ToString() == "End")
            {
                Shutdown();
            }
        }

        public void End()
        {
            context.Send("End");
            Shutdown();
        }

        private void Shutdown()
        {
            Run = false;

            Collector.WaitForTransmitComplete(Ubiq.Logging.EventType.Experiment, success =>
            {
                // Check if we succeessfully checked in with the active collector
                if(!success)
                {
                    Debug.LogError("Failed to check successful receipt of last logs before shutting down.");
                }

                // Wait five seconds for all the logs to be written before quitting
                StartCoroutine(DelayedQuit());
            });
        }

        public IEnumerator DelayedQuit()
        {
            yield return new WaitForSeconds(3f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        struct Ev // Structure for use by JsonUtility
        {
            public long ticks;
            public string peer;
            public string Event; // Event must be capitalised as event is a keyword in C#. This requires a complementary modification to the source Json (below).
            public int arg1;
        }

        struct Record
        {
            public long ticks;
            public int counter;
        }

        public void AnalyseLogsInDefaultFolder()
        {
            Dictionary<string, List<Record>> series = new Dictionary<string, List<Record>>();

            int totalFiles = 0;

            foreach (var item in System.IO.Directory.EnumerateFiles(Application.persistentDataPath, "Experiment*.json"))
            {
                var contents = System.IO.File.ReadAllText(item);

                contents = contents.Replace("event", "Event");

                foreach (var eventstring in SplitJsonObjects(contents))
                {
                    var ev = JsonUtility.FromJson<Ev>(eventstring);

                    if(!series.ContainsKey(ev.peer))
                    {
                        series.Add(ev.peer, new List<Record>());
                    }
                    series[ev.peer].Add(new Record()
                    {
                        counter = ev.arg1,
                        ticks = ev.ticks
                    });
                }

                totalFiles++;
            }

            foreach (var peer in series)
            {
                peer.Value.Sort((x,y) => x.ticks.CompareTo(y.ticks)); // Ticks should come from the emitter Peer and so should be monotonic increasing regardless of which log file they end up in
            }

            int totalEvents = 0;

            foreach (var peer in series)
            {
                var sequence = peer.Value.Select(x => x.counter).ToArray();
                Debug.Assert(sequence[0] == 0);

                for (int i = 0; i < sequence.Length - 1; i++)
                {
                    if(sequence[i + 1] != sequence[i] + 1) // Checks that the counter follows the deterministic
                    {
                        Debug.LogError($"Missing Log Event {i+1} for Peer {peer.Key}");
                    }
                }

                totalEvents += sequence.Length;
            }

            Debug.Log($"Completed LogDiagnostics (Checked {totalEvents} Events from {series.Count} Peers across {totalFiles} Files)");
        }

        public IEnumerable<string> SplitJsonObjects(string contents)
        {
            int level = 0;
            int start = 0;
            int i = 0;
            do
            {
                switch (contents[i])
                {
                    case '{':
                        level++;
                        if(level == 1)
                        {
                            start = i;
                        }
                        break;
                    case '}':
                        level--;
                        if (level == 0)
                        {
                            yield return contents.Substring(start, i - start + 1);
                        }
                        break;
                }
                i++;
            } while (i < contents.Length);
        }
    }

}
