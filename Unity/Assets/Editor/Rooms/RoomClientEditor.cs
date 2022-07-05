using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;

namespace Ubiq.Rooms
{
    [CustomEditor(typeof(RoomClient))]
    public class RoomClientEditor : Editor
    {
        private ReorderableList managerReorderableList;
        private SerializedProperty serversProperty;
        private bool foldoutRooms;
        private List<IRoom> available = new List<IRoom>();
        private double nextRoomRefreshTime = -1;
        private const double ROOM_REFRESH_INTERVAL = 2d;

        private void Awake()
        {
            (target as RoomClient).OnRooms.AddListener(RoomClient_OnRoomsDiscovered);
        }

        private void OnDestroy()
        {
            (target as RoomClient).OnRooms.RemoveListener(RoomClient_OnRoomsDiscovered);
        }

        private void OnEnable()
        {
            serversProperty = serializedObject.FindProperty("servers");
            managerReorderableList = new ReorderableList(serializedObject, serversProperty, false, false, true, true);
            managerReorderableList.onCanAddCallback += CanAddCallbackDelegate;
            managerReorderableList.drawElementCallback += DrawElementCallback;
            managerReorderableList.drawHeaderCallback += HeaderCallbackDelegate;
            managerReorderableList.elementHeightCallback += ElementHeightCallbackDelegate;
        }

        bool CanAddCallbackDelegate(ReorderableList list)
        {
            return list.serializedProperty.arraySize < 1;
        }

        void HeaderCallbackDelegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "Default Server");
        }

        float ElementHeightCallbackDelegate(int index)
        {
            if (index >= serversProperty.arraySize)
            {
                return 0;
            }
            float propertyHeight = EditorGUI.GetPropertyHeight(serversProperty.GetArrayElementAtIndex(index), true);
            float spacing = EditorGUIUtility.singleLineHeight / 2;
            return propertyHeight + spacing;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            rect.y += EditorGUIUtility.singleLineHeight / 4;
            EditorGUI.PropertyField(rect, serversProperty.GetArrayElementAtIndex(index), new GUIContent(""), true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var component = target as RoomClient;

            if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup > nextRoomRefreshTime)
            {
                component.DiscoverRooms();
                nextRoomRefreshTime = EditorApplication.timeSinceStartup + ROOM_REFRESH_INTERVAL;
            }

            managerReorderableList.DoLayoutList();

            if(component.JoinedRoom)
            {
                EditorGUILayout.HelpBox("Joined Room " + component.Room.UUID, MessageType.Info);
            }

            foldoutRooms = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutRooms, "Available Rooms");

            if (foldoutRooms)
            {
                foreach (var item in available)
                {
                    EditorGUILayout.LabelField($"{item.Name}");
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            if (available != null)
            {
                for(int i = 0; i < Math.Min(available.Count, 5); i++)
                {
                    var room = available[i];
                    if (GUILayout.Button($"Join Room {room.Name}"))
                    {
                        component.Join(joincode: room.JoinCode);
                    }
                }
            }

            if(Application.isPlaying)
            {
                GUILayout.Label($"Session Id { component.SessionId }");
            }

            if(GUILayout.Button("Create Room")) // creates a room with a random id and joins it
            {
                component.Join(name:$"Editor Room {IdGenerator.GenerateUnique().ToString()}",publish:true); // publish true because we probably need other Editor inspectors to see it
            }

            if(GUILayout.Button("Leave Room"))
            {
                component.Join("",false);
            }

            if (GUILayout.Button("Refresh"))
            {
                component.DiscoverRooms();
            }

            GUI.enabled = false;

            if (component.Me != null)
            {
                EditorGUILayout.LabelField($"Me {component.Me.uuid}");
            }

            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        private void RoomClient_OnRoomsDiscovered(List<IRoom> rooms, RoomsDiscoveredRequest request)
        {
            // We're interested only in requests for all rooms
            if (!string.IsNullOrEmpty(request.joincode))
            {
                return;
            }

            available = new List<IRoom>(rooms);
        }
    }
}