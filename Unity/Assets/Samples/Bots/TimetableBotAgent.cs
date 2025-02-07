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
        private int nextEvent = -1;
        // ExperimentLogEmitter logEmitter;

        public float Radius = 10f;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            events = TimetableFactory.Instance.Events;
        }

        // Start is called before the first frame update
        void Start()
        {
            // if (nextEvent == -1)
            // {
            //     for (int i = 0; i < events.Count; i++)
            //     {
            //         if (events[i].StartTime > Time.time)
            //         {
            //             nextEvent = i;
            //             break;
            //         }
            //     }
            // }
            // logEmitter = new ExperimentLogEmitter(this);
            LookForNextEvent();
            // destination = events[nextEvent].Location.transform.position;
            // navMeshAgent.SetDestination(events[nextEvent].Location.transform.position);
            Debug.Log("events count: " + events.Count + "nextEvent: " + nextEvent);
        }

        // Update is called once per frame
        void Update()
        {
            if (!navMeshAgent.isActiveAndEnabled)
            {
                return;
            }
            
            if (destination != null)
            {
                if (Vector3.Distance(destination, transform.position) < 1f)// when bot has arrived
                {
                    if (ReadyToLeave())
                    {
                        LookForNextEvent();
                    }
                }
            }
            else
            {
                LookForNextEvent();
                return;
            }

            // print("bot id: " + gameObject.name + " | time: " + Time.time + "| location: " + transform.position + " | destination: " + navMeshAgent.destination + " | nextEvent: " + nextEvent + " | next event start time: " + events[nextEvent].StartTime + " | next event location: " + events[nextEvent].Location.transform.position + "| remaining distance: " + navMeshAgent.remainingDistance);
        }

        public void JoinRoom(Guid roomId)
        {
            Debug.Log("Bot " + gameObject.name + " joined room " + roomId);

        }

        private void LookForNextEvent()
        {
            for (int i = nextEvent + 1; i < events.Count; i++)
            {
                print($"!!! event {i} start time: {events[i].StartTime} | current time: {Time.time} | events[i].StartTime > Time.time: {events[i].StartTime > Time.time}");
                if (events[i].StartTime > Time.time)
                {
                    break;
                }
                nextEvent = i;
            }
            print($"!!! next event: {nextEvent}");

            if (nextEvent != -1)
            {
                destination = events[nextEvent].Location.transform.position;
                navMeshAgent.SetDestination(events[nextEvent].Location.transform.position);
            }
        }

        private bool ReadyToLeave()
        {
            return Time.time > events[nextEvent].EndTime;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destination, 0.5f);
        }
    }
}