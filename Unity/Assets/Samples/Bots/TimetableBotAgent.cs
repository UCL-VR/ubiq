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
        public bool alwaysAttendEvents = true; // If false, bot will choose whether to attend an event or not. This gives more accurate capture of human behaviour but in stress testing, this should be true.
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
                Bot bot = GetComponent<Bot>();
                BotsManager.Find(bot).RemoveBot(bot);
                // Destroy(transform.parent.gameObject); // Bot has finished its timetable
            }

            // Debug.Log($"{Time.time}, nextEvent: {events[nextEvent].Name} at {events[nextEvent].StartTime}");s
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
                destination = currentRoom.transform.position + Vector3.Scale(UnityEngine.Random.insideUnitSphere, new Vector3(currentRoom.size.x / 2, 0, currentRoom.size.z / 2));
                // Make sure the destination is within the room bounds
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

            events.Add(publicTimetable[0]); // Add the first event as-is

            var currentGroupStartTime = publicTimetable[0].StartTime;
            List<TimetableFactory.TimetableEvent> currentGroup = new List<TimetableFactory.TimetableEvent>();
            // CurrentGroup is a list of events that have the same start time. Iterate through the public timetable and collect events that have the same start time. When a event with a different start time is found, randomly choose one of the events in the current group to add to the bot's timetable. Then, clear the current group and add the new event to it.
            for (int i = 1; i < publicTimetable.Count - 1; i++)
            {
                if (publicTimetable[i].StartTime == currentGroupStartTime)
                {
                    currentGroup.Add(publicTimetable[i]);
                }
                else
                {
                    AddRandomEventFromGroup(currentGroup);
                    currentGroup.Clear();
                    currentGroup.Add(publicTimetable[i]);
                    currentGroupStartTime = publicTimetable[i].StartTime;
                }
            }

            // If there are any events left in the current group, randomly choose one of them to add to the bot's timetable
            AddRandomEventFromGroup(currentGroup);
            currentGroup.Clear();
            // if (currentGroup.Count > 0)
            // {
            //     // int randomIndex = UnityEngine.Random.Range(0, currentGroup.Count + 1);
            //     int randomIndex = UnityEngine.Random.Range(0, currentGroup.Count);
            //     if (randomIndex < currentGroup.Count)
            //     {
            //         events.Add(Customize(currentGroup[randomIndex]));
            //     }
            //     currentGroup.Clear();
            // }

            events.Add(Customize(publicTimetable[publicTimetable.Count - 1]));

            // Make sure the end time of last event is large enough
            if (events.Count > 0)
            {
                events[events.Count - 1].StartTime = Mathf.Min(events[events.Count - 1].StartTime, publicTimetable[publicTimetable.Count - 1].StartTime + 15f); // Clamp start time of leaving the room to be at most 15 seconds after the original start time. This is under the assumption that the start time of the last event is 20 seconds before the end of the experiment.
                events[events.Count - 1].EndTime = publicTimetable[publicTimetable.Count - 1].EndTime;
            } 
        }
        
        private void AddRandomEventFromGroup(List<TimetableFactory.TimetableEvent> currentGroup)
        {
            if (currentGroup.Count <= 0)
                return;

            // Randomly choose one of the events in the current group to add to the bot's timetable
            int randomIndex;
            if (alwaysAttendEvents)
                randomIndex = UnityEngine.Random.Range(0, currentGroup.Count);
            else
                randomIndex = UnityEngine.Random.Range(0, currentGroup.Count + 1);
            
            if (randomIndex < currentGroup.Count)
            {
                events.Add(Customize(currentGroup[randomIndex]));
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