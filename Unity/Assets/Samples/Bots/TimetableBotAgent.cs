using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Ubiq.Logging;

namespace Ubiq.Samples.Bots
{
    /// <summary>
    /// The main controller for a Bot; this Component controls the behaviour of the bot, moving it around the scene
    /// and emulating interaction.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class TimetableBotAgent : MonoBehaviour
    {
        private NavMeshAgent navMeshAgent;
        private Action<string, string> sendMessageBack;
        public Vector3 destination;
        List<TimetableFactory.TimetableEvent> events;
        private int nextEvent = 0;
        ExperimentLogEmitter logEmitter;
        private string filePath = "/Users/peterbox/UCL/fyp/build/timetabledBotAgent.txt";

        public float Radius = 10f;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            events = TimetableFactory.Instance.Events;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (nextEvent == 0)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].StartTime > Time.time)
                    {
                        nextEvent = i;
                        break;
                    }
                }
            }
            logEmitter = new ExperimentLogEmitter(this);

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            Debug.Log("events count: " + events.Count + "nextEvent: " + nextEvent);
        }

        // Update is called once per frame
        void Update()
        {
            if (!navMeshAgent.isActiveAndEnabled)
            {
                return;
            }

            print("bot id: " + gameObject.name + " | time: " + Time.time  + " | destination: " + navMeshAgent.destination + " | nextEvent: " + nextEvent + " | next event location: " + events[nextEvent].Location.transform.position + "| location: " + transform.position +  "| remaining distance: " + navMeshAgent.remainingDistance);

            if (destination != null)
            {
                if (Vector3.Distance(destination, transform.position) < 1f)
                {
                    return;
                }
            }


            if (nextEvent != -1 && events[nextEvent].StartTime < Time.time)
            {
                navMeshAgent.SetDestination(events[nextEvent].Location.transform.position);
            }
        }

        public void JoinRoom(Guid roomId)
        {
            Debug.Log("Bot " + gameObject.name + " joined room " + roomId);

        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destination, 0.5f);
        }
    }
}