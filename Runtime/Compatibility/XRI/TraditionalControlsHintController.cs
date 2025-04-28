#if XRI_3_0_7_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.XRI.TraditionalControls
{
    public class TraditionalControlsHintController : MonoBehaviour
    {
        public enum Platform
        {
            Mobile,
            Mac
        }
        
        [Serializable]
        public struct HintsByPlatform
        {
            public Platform platform;
            public List<TraditionalControlsHint> hints;
            public bool showHideButton;
        }
        
        public Button prev;
        public Button next;
        public Button hide;

        public List<TraditionalControlsHint> defaultHints;
        public List<HintsByPlatform> hintsByPlatforms;
        
        public bool isHidden { get; private set; }
        
        private List<TraditionalControlsHint> activeHints;
        private int activeIndex = 0;

        private void Start()
        {
            prev.onClick.AddListener(Previous);
            next.onClick.AddListener(Next);
            hide.onClick.AddListener(ToggleHidden);
            
            activeHints = defaultHints;
            for (int i = 0; i < hintsByPlatforms.Count; i++)
            {
                var platform = hintsByPlatforms[i].platform;
                if ((platform == Platform.Mobile && IsMobilePlatform())
                    || (platform == Platform.Mac && Application.platform == RuntimePlatform.OSXPlayer))
                {
                    activeHints = hintsByPlatforms[i].hints;
                    if (!hintsByPlatforms[i].showHideButton)
                    {
                        hide.gameObject.SetActive(false);
                    }
                    break;
                }
            }
            
            if (activeHints.Count < 2)
            {
                prev.gameObject.SetActive(false);
                next.gameObject.SetActive(false);
            }
            
            if (activeHints.Count == 0)
            {
                hide.gameObject.SetActive(false);
            }
            else
            {
                activeHints[activeIndex].gameObject.SetActive(true);
            }
        }
        
        public void Previous()
        {
            if (activeHints.Count == 0)
            {
                return;
            }
            
            activeHints[activeIndex].gameObject.SetActive(false);
            activeIndex = (activeIndex + activeHints.Count - 1) % activeHints.Count;
            activeHints[activeIndex].gameObject.SetActive(true);
        }
        
        private bool IsMobilePlatform()
        {
            return Application.isMobilePlatform || 
                   Application.platform == RuntimePlatform.Android || 
                   Application.platform == RuntimePlatform.IPhonePlayer ||
                   Application.platform == RuntimePlatform.WebGLPlayer;
        }

        public void Next()
        {
            if (activeHints.Count == 0)
            {
                return;
            }
            
            activeHints[activeIndex].gameObject.SetActive(false);
            activeIndex = (activeIndex + 1) % activeHints.Count;
            activeHints[activeIndex].gameObject.SetActive(true);
        }
        
        public void ToggleHidden()
        {
            isHidden = !isHidden;
            
            if (isHidden)
            {
                hide.GetComponentInChildren<Text>().text = "Show Hints";
                prev.gameObject.SetActive(false);
                next.gameObject.SetActive(false);
                activeHints[activeIndex].Hide(); 
            }
            else
            {
                hide.GetComponentInChildren<Text>().text = "Hide Hints";
                prev.gameObject.SetActive(true);
                next.gameObject.SetActive(true);
                activeHints[activeIndex].Show(); 
            }
        }
    }
}
#endif