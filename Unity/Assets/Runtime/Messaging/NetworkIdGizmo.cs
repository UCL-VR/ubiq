using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Messaging
{
    public class NetworkIdGizmo : MonoBehaviour
    {
        private string addresses;

        public Color color = Color.black;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            var processors = NetworkScene.Find(this).GetProcessors();

            addresses = "";
            foreach (var item in GetComponentsInChildren<MonoBehaviour>())
            {
                if(item.GetType().GetMethod("ProcessMessage") != null)
                {
                    foreach (var processor in processors)
                    {
                        if(processor.Key.Target as MonoBehaviour == item)
                        {
                            addresses += $"{SceneGraphHelper.GetUniqueAddress(item)} {processor.Value}\n"; 
                        }
                    }
                }
                if(NetworkedBehaviour.NetworkedBehaviours.IsNetworkedBehaviour(item))
                {
                    addresses += SceneGraphHelper.GetUniqueAddress(item) + "\n";

                }
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            {
                UnityEditor.Handles.BeginGUI();

                var style = new GUIStyle();
                //style.fontSize = 20;

                style.normal.textColor = color;

                UnityEditor.Handles.Label(transform.position, $"{addresses}", style);
                UnityEditor.Handles.EndGUI();

            }
#endif
        }
    }
}