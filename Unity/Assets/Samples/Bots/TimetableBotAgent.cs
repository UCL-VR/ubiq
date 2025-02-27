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
        private TimetableFactory.TimetableEvent currentEvent;
        // ExperimentLogEmitter logEmitter;

        private void Awake()
        {
            Debug.Log("Bot " + gameObject.name + " awake");
            navMeshAgent = GetComponent<NavMeshAgent>();
            events = new List<TimetableFactory.TimetableEvent>();

            CustomizeTimetable();
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Bot " + gameObject.name + " start");
            LookForNextEvent();
            Debug.Log("events count: " + events.Count + "nextEvent: " + nextEvent);
        }

        // Update is called once per frame
        void Update()
        {
            if (nextEvent >= events.Count - 1)
            {
                Debug.Log("Bot " + gameObject.name + " finished timetable");
                Destroy(transform.parent.gameObject); // Bot has finished its timetable
            }

            // Debug.Log("nextEvent: "+nextEvent);
            if (!navMeshAgent.isActiveAndEnabled)
            {
                return;
            }
            
            if (destination != null)
            {
                if (Vector3.Distance(destination, transform.position) < 1f)// when bot has arrived
                {
                    currentEvent = events[nextEvent];
                    if (ReadyToLeave())
                    {
                        LookForNextEvent();
                    }
                    else
                    {
                        WanderWithinRoom();
                    }
                }
            }
            else
            {
                LookForNextEvent();
            }    
        }

        public void JoinRoom(Guid roomId)
        {
            Debug.Log("Bot " + gameObject.name + " joined room " + roomId);

        }

        private void LookForNextEvent()
        {
            currentEvent = null;
            for (int i = nextEvent + 1; i < events.Count; i++)
            {
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
            if (nextEvent == -1)
            {
                return false;
            }
            if (nextEvent >= events.Count - 1)
            {
                return false;
            }
            return Time.time > events[nextEvent + 1].StartTime;
        }

        private void WanderWithinRoom()
        {
            if (!navMeshAgent.isActiveAndEnabled)
            {
                return;
            }

            if (currentEvent == null)
            {
                return;
            }

            NavMeshHit hit;

            int i = 0;
            do
            {
                i++;
                destination = currentEvent.Location.transform.position + UnityEngine.Random.insideUnitSphere * currentEvent.Location.size.x / 2;
            } while (!NavMesh.SamplePosition(destination, out hit, float.MaxValue, NavMesh.AllAreas) && i < 100);
            destination = hit.position;
            navMeshAgent.destination = destination;
        }

        /// <summary>
        /// Perturb the public timetable to create a customized timetable for the bot.
        /// This works by iterating through every event, and add a zero-meaned Gaussian noise to the start and end time of the event.
        /// When adding noise, it is also essential to validate that the new start and end time do not overlap with other events.
        /// The new timetable is stored in the events list.
        /// </summary>
        private void CustomizeTimetable()
        {
            List<TimetableFactory.TimetableEvent> publicTimetable = TimetableFactory.Instance.Events;

            for (int i = 0; i < publicTimetable.Count - 1; i++)
            {
                // Choose whether to add the event to the bot's timetable
                if (i == 0)  // Always add the first event as-is
                {                
                    events.Add(publicTimetable[i]);
                    continue;
                }
                if (UnityEngine.Random.value > 0.5f)
                    continue;


                TimetableFactory.TimetableEvent e = publicTimetable[i];

                TimetableFactory.TimetableEvent newEvent = new TimetableFactory.TimetableEvent();
                newEvent.Name = e.Name;
                newEvent.Location = e.Location;
                newEvent.StartTime = Util.GaussianRandom(e.StartTime, 10f);
                newEvent.EndTime = e.EndTime;
                events.Add(newEvent);
            }

            TimetableFactory.TimetableEvent lastE = publicTimetable[publicTimetable.Count - 1];
            TimetableFactory.TimetableEvent newLastEvent = new TimetableFactory.TimetableEvent();
            newLastEvent.Name = lastE.Name;
            newLastEvent.Location = lastE.Location;
            newLastEvent.StartTime = Util.GaussianRandom(lastE.StartTime, 10f);
            newLastEvent.EndTime = lastE.EndTime;
            events.Add(newLastEvent);

            // Make sure the end time of last event is large enough
            if (events.Count > 0)
            {
                events[events.Count - 1].EndTime = publicTimetable[publicTimetable.Count - 1].EndTime;
            } 
        }

        private TimetableFactory.TimetableEvent Customize(TimetableFactory.TimetableEvent e, float noise)
        {
            TimetableFactory.TimetableEvent newEvent = new TimetableFactory.TimetableEvent();
            newEvent.Name = e.Name;
            newEvent.Location = e.Location;
            newEvent.StartTime = Util.GaussianRandom(e.StartTime, noise);
            newEvent.EndTime = e.EndTime;
            return newEvent;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destination, 0.5f);
        }
    }
}