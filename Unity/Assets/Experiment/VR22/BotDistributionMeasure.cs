using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms.Spatial;
using UnityEditor;
using UnityEngine;

public class BotDistributionMeasure : MonoBehaviour
{
    public static BotDistributionMeasure Singleton;

    private void Awake()
    {
        Singleton = this;
    }

    private Dictionary<RoomSpatialAgent, List<Guid>> rooms = new Dictionary<RoomSpatialAgent, List<Guid>>();

    public void AddVisitedRoom(RoomSpatialAgent agent, Guid guid)
    {
        if(!rooms.ContainsKey(agent))
        {
            rooms.Add(agent, new List<Guid>());
        }
        rooms[agent].Add(guid);
    }

    public static void AddVisitedRoomStatic(RoomSpatialAgent agent, Guid guid)
    {
        Singleton.AddVisitedRoom(agent, guid);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    float timer = 0;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 60)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }
    }

    [Serializable]
    private class Results
    {
        public List<int> Rooms = new List<int>();
    }

    private void OnDestroy()
    {
        var results = new Results();

        foreach (var item in rooms.Values)
        {
            var r = item.Distinct().Count();
            results.Rooms.Add(r);
        }

        Debug.Log(JsonUtility.ToJson(results));
    }
}
