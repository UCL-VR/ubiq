using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Timetable class that stores a list of events
public class TimetableFactory : MonoBehaviour
{
    // Singleton timetable factory
    public static TimetableFactory Instance;

    [Serializable]
    public class TimetableEvent
    {
        public string Name;
        public Room Location;
        public int StartTime;
        public int EndTime;
    }
    public List<TimetableEvent> Events;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
}
