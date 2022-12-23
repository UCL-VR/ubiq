using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;

namespace Ubiq.Networking
{
    [CustomPropertyDrawer(typeof(ConnectionDefinition))]
    public class ConnectionDefinitionPropertyDrawer : PropertyDrawer
    {
        private bool foldout;
        private bool foldoutSet;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * (foldout ? 2 : 1);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return base.CreatePropertyGUI(property);
        }

        private struct HorizontalElement
        {
            public float width;
            public float proportion;
            public SerializedProperty field;
            public GUIContent content;
        }

        private struct HorizontalLayout
        {
            public List<HorizontalElement> elements;
            public Rect position;

            public HorizontalLayout(Rect position)
            {
                this.position = position;
                this.elements = new List<HorizontalElement>();
            }

            public void Add(SerializedProperty property, float proportion)
            {
                elements.Add(new HorizontalElement()
                {
                    content = GUIContent.none,
                    field = property,
                    proportion = proportion,
                    width = 0
                });
            }

            public void Add(GUIContent content)
            {
                elements.Add(new HorizontalElement()
                {
                    content = content,
                    field = null,
                    proportion = 0,
                    width = EditorStyles.label.CalcSize(content).x
                });
            }

            public void Draw()
            {
                float absoluteWidth = 0;
                float proportionSum = 0;
                for (int i = 0; i < elements.Count; i++)
                {
                    absoluteWidth += elements[i].width;
                    proportionSum += elements[i].proportion;
                }

                float remaining = position.width - absoluteWidth;

                float x = position.x;
                foreach (var element in elements)
                {
                    if (element.field != null)
                    {
                        var r = new Rect(x, position.y, (element.proportion / proportionSum) * remaining, position.height);
                        EditorGUI.PropertyField(r, element.field, element.content);
                        x += r.width;
                    }
                    else
                    {
                        var r = new Rect(x, position.y, element.width, position.height);
                        EditorGUI.PrefixLabel(r, element.content);
                        x += r.width;
                    }
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (property.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                // Local/embedded Scene instances default to folded out, while Asset references do the opposite.

                if (!foldoutSet)
                {
                    foldout = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(property.objectReferenceValue));
                    foldoutSet = true;
                }

                var arrowRect = new Rect(position);
                arrowRect.width = EditorStyles.foldout.padding.left * 1.25f;
                arrowRect.x += EditorStyles.foldout.padding.left;

                position.x += arrowRect.width;
                position.width -= arrowRect.width;

                EditorGUI.PropertyField(position, property, label);

                foldout = EditorGUI.Foldout(arrowRect, foldout, GUIContent.none);

                if (foldout)
                {
                    position.y = position.y + position.height;

                    try
                    {
                        var serialised = new SerializedObject(property.objectReferenceValue);
                        serialised.Update();

                        var layout = new HorizontalLayout(position);

                        layout.Add(serialised.FindProperty("type"), 0.2f);

                        var type = (ConnectionType)serialised.FindProperty("type").intValue;
                        switch (type)
                        {
                            case ConnectionType.TcpClient:
                            case ConnectionType.WebSocket:
                                layout.Add(serialised.FindProperty("sendToIp"), 0.3f);
                                layout.Add(serialised.FindProperty("sendToPort"), 0.2f);
                                break;
                            case ConnectionType.TcpServer:
                                layout.Add(serialised.FindProperty("listenOnIp"), 0.3f);
                                layout.Add(serialised.FindProperty("listenOnPort"), 0.2f);
                                break;
                            case ConnectionType.UDP:
                                layout.Add(new GUIContent("Send:"));
                                layout.Add(serialised.FindProperty("sendToIp"), 0.3f);
                                layout.Add(serialised.FindProperty("sendToPort"), 0.2f);
                                layout.Add(new GUIContent("Receive:"));
                                layout.Add(serialised.FindProperty("listenOnIp"), 0.3f);
                                layout.Add(serialised.FindProperty("listenOnPort"), 0.2f);
                                break;
                        }

                        layout.Draw();
                        serialised.ApplyModifiedProperties();

                    }
                    catch(ArgumentException)
                    {
                        // For some reason when we delete an element using the Del key, the drawer tries to run
                        // with a non-null objectReferenceValue for a frame, which will result in an exception
                        // when trying to access its properties.
                    }
                }
            }
        }
    }
}