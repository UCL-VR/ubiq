using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.XR
{
    /// <summary>
    /// This script registers a Canvas for events by the XRUIRaycaster
    /// </summary>
    public class XRUICanvas : MonoBehaviour
    {
        private static List<Canvas> canvases = new List<Canvas>();
        private static List<Canvas> toremove = new List<Canvas>();

        private void Start()
        {
            canvases.Add(GetComponent<Canvas>());
        }

        public static IEnumerable<Canvas> Canvases
        {
            get
            {
                foreach (var item in canvases)
                {
                    if(item)
                    {
                        if(item.isActiveAndEnabled)
                        {
                            yield return item;
                        }
                    }
                    else
                    {
                        toremove.Add(item);
                    }
                }
                foreach (var item in toremove)
                {
                    canvases.Remove(item);
                }
                toremove.Clear();
            }
        }
    }
}