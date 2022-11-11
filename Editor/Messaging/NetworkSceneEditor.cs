using System.Collections.Generic;
using Ubiq.Rooms;
using Ubiq.Networking;
using UnityEditor;
using UnityEngine;

namespace Ubiq.Messaging
{
    [CustomEditor(typeof(NetworkScene))]
    public class NetworkSceneEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var component = target as NetworkScene;

            EditorGUILayout.LabelField("Id", component.Id.ToString());

            var connections = new List<MonoBehaviour>();

            connections.AddRange(component.GetComponentsInParent<ConnectionManager>());
            connections.AddRange(component.GetComponentsInParent<SimpleConnection>());
            connections.AddRange(component.GetComponentsInParent<InternalEmulator>());
            connections.AddRange(component.GetComponentsInParent<RoomClient>());

            if (connections.Count > 0)
            {
                var label = "Connections: \n";
                foreach (var item in connections)
                {
                    label += item.gameObject.name + " " + item.GetType().Name.ToString() + "\n";
                }
                EditorGUILayout.LabelField(label, GUILayout.Height(20 * (connections.Count + 1)));
            }

            if (connections.Count <= 0)
            {
                EditorGUILayout.HelpBox("Network Scene does not appear to have any components to create connections.", MessageType.Warning);
            }
        }
    }
}