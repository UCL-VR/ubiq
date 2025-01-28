#if XRI_3_0_7_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Compatibility.XRI.TraditionalControls
{
    public class TraditionalControlsHints : MonoBehaviour
    {
        [Serializable]
        public struct DefaultHint
        {
            public RuntimePlatform platform;
            public GameObject hint;
        }
        
        public List<GameObject> hints;
        public List<DefaultHint> defaultHintsPerPlatform;
        
        public Button prev;
        public Button next;
        public Button hide;

        private int index = 0;

        private void Start()
        {
            prev.onClick.AddListener(Previous);
            next.onClick.AddListener(Next);
            hide.onClick.AddListener(() =>
            {
                this.gameObject.SetActive(!gameObject.activeSelf);
                
                if (gameObject.activeSelf)
                {
                    hide.GetComponentInChildren<Text>().text = "Hide Control Schemes";
                }
                else
                {
                    hide.GetComponentInChildren<Text>().text = "Show Control Schemes";
                }
            });
            
            for (int i = 0; i < defaultHintsPerPlatform.Count; i++)
            {
                var defaultHint = defaultHintsPerPlatform[i];
                if (defaultHint.platform == Application.platform)
                {
                    var hintIndex = hints.IndexOf(defaultHint.hint);
                    if (hintIndex >= 0)
                    {
                        index = hintIndex;
                        break;
                    }
                }
            }
            
            hints[index].SetActive(true);
        }
        
        public void Previous()
        {
            if (hints.Count == 0)
            {
                return;
            }
            
            hints[index].SetActive(false);
            index = (index + hints.Count - 1) % hints.Count;
            hints[index].SetActive(true);
        }

        public void Next()
        {
            if (hints.Count == 0)
            {
                return;
            }
            
            hints[index].SetActive(false);
            index = (index + 1) % hints.Count;
            hints[index].SetActive(true);
        }

        public float offset = 0;
    }
}
#endif