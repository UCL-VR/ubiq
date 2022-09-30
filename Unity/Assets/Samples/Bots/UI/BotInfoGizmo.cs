using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    public class BotInfoGizmo : MonoBehaviour
    {
        private RoomClient roomClient;

        // Start is called before the first frame update
        void Start()
        {
            roomClient = NetworkScene.Find(this).GetComponent<RoomClient>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (roomClient)
            {
                UnityEditor.Handles.BeginGUI();

                var Peers = roomClient.Peers.Count();

                //var style = new GUIStyle();
                //style.fontSize = 20;
                UnityEditor.Handles.Label(transform.position, $"Peers: { Peers }");
                UnityEditor.Handles.EndGUI();
            }
#endif
        }
    }
}