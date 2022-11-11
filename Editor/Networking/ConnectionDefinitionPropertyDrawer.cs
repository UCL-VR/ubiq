using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ubiq.Networking
{
    [CustomPropertyDrawer(typeof(ConnectionDefinition))]
    public class ConnectionDefinitionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProperty = property.FindPropertyRelative("type");

            var connectionTypeWidth = position.width * 2 / 5;
            var ipWidth = position.width / 5;
            var portWidth = position.width / 5;

            var height = EditorGUIUtility.singleLineHeight;

            var rectType = new Rect(position.x, position.y, connectionTypeWidth, height);
            position.x += connectionTypeWidth;
            var rectIp = new Rect(position.x, position.y, ipWidth, height);
            position.x += ipWidth;
            var rectPort = new Rect(position.x, position.y, portWidth, height);

            EditorGUI.PropertyField(rectType, property.FindPropertyRelative("type"), GUIContent.none);

            switch ((ConnectionType)typeProperty.intValue)
            {
                case ConnectionType.websocket:
                case ConnectionType.tcp_client:
                    EditorGUI.PropertyField(rectIp, property.FindPropertyRelative("send_to_ip"), GUIContent.none);
                    EditorGUI.PropertyField(rectPort, property.FindPropertyRelative("send_to_port"), GUIContent.none);
                    break;
                case ConnectionType.tcp_server:
                    EditorGUI.PropertyField(rectIp, property.FindPropertyRelative("listen_on_ip"), GUIContent.none);
                    EditorGUI.PropertyField(rectPort, property.FindPropertyRelative("listen_on_port"), GUIContent.none);
                    break;
                case ConnectionType.udp:
                    EditorGUI.PropertyField(rectIp, property.FindPropertyRelative("listen_on_ip"), GUIContent.none);
                    EditorGUI.PropertyField(rectPort, property.FindPropertyRelative("listen_on_port"), GUIContent.none);
                    EditorGUI.PropertyField(new Rect(position.x, position.y, 100, height), property.FindPropertyRelative("send_to_ip"), GUIContent.none);
                    EditorGUI.PropertyField(new Rect(position.x, position.y, 100, height), property.FindPropertyRelative("send_to_port"), GUIContent.none);
                    break;
            }

            EditorGUI.EndProperty();
        }
    }
}