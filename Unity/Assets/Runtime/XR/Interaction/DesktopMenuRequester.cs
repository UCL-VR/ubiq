using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.XR
{
    public class DesktopMenuRequester : MonoBehaviour
    {
        public MenuRequestSource source;
        public List<KeyCode> buttons;

        public void Update()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (Input.GetKeyDown(buttons[i]))
                {
                    source.Request(gameObject);
                }
            }
        }
    }
}
