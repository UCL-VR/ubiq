using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Compatibility.XRI.TraditionalControls
{
    public class TraditionalControlsHint : MonoBehaviour
    {
        public List<GameObject> toHide;
        public bool hideAll;
        
        public void Show()
        {
            if (hideAll)
            {
                gameObject.SetActive(true);
                return;
            }
            
            for(int i = 0; i < toHide.Count; i++)
            {
                toHide[i].gameObject.SetActive(true);
            }
        }
        
        public void Hide()
        {
            if (hideAll)
            {
                gameObject.SetActive(false);
                return;
            }
            
            for(int i = 0; i < toHide.Count; i++)
            {
                toHide[i].gameObject.SetActive(false);
            }
        }
    }
}