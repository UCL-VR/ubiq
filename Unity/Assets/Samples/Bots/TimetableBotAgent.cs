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
            navMeshAgent = GetComponent<NavMeshAgent>();
            events = new List<TimetableFactory.TimetableEvent>();

            CustomizeTimetable();
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("events count: " + events.Count + "nextEvent: " + nextEvent);
            LookForNextEvent();
            Debug.Log("UPDATED - nextEvent: " + nextEvent);
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
                Debug.Log(Time.time + "Looking for next event: " + events[i].Name + " at time: " + events[i].StartTime);
                if (events[i].StartTime > Time.time)
                {
                    break;
                }
                nextEvent = i;
            }

            if (nextEvent != -1)
            {
                Room room = RoomFactory.Instance.Locations[events[nextEvent].RoomIndex];
                if (room == null)
                {
                    Debug.LogError("Room not found for event: " + events[nextEvent].Name);
                    return;
                }
                destination = room.transform.position;
                navMeshAgent.SetDestination(room.transform.position);
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
            Room currentRoom = RoomFactory.Instance.Locations[currentEvent.RoomIndex];
            if (currentRoom == null)
            {
                Debug.LogError("Room not found for event: " + currentEvent.Name);
                return;
            }

            int i = 0;
            do
            {
                i++;
                destination = currentRoom.transform.position + UnityEngine.Random.insideUnitSphere * currentRoom.size.x / 2;
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
            List<TimetableFactory.TimetableEvent> publicTimetable = BotsManager.Find(GetComponent<Bot>()).GetTimetableEvents();

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



                events.Add(Customize(publicTimetable[i]));
            }

            events.Add(Customize(publicTimetable[publicTimetable.Count - 1]));

            // Make sure the end time of last event is large enough
            if (events.Count > 0)
            {
                events[events.Count - 1].StartTime = Mathf.Min(events[events.Count - 1].StartTime, publicTimetable[publicTimetable.Count - 1].StartTime + 15f); // Clamp start time of leaving the room to be at most 15 seconds after the original start time. This is under the assumption that the start time of the last event is 20 seconds before the end of the experiment.
                events[events.Count - 1].EndTime = publicTimetable[publicTimetable.Count - 1].EndTime;
            } 
        }

        private TimetableFactory.TimetableEvent Customize(TimetableFactory.TimetableEvent e, float noise=10f)
        {
            TimetableFactory.TimetableEvent newEvent = new TimetableFactory.TimetableEvent();
            newEvent.Name = e.Name;
            newEvent.RoomIndex = e.RoomIndex;
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