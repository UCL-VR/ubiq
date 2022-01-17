using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.XR
{
    public class HandControllerMenuRequester : MonoBehaviour
    {
        public HandController handController;
        public MenuRequestSource source;

        public void OnEnable()
        {
            if (handController)
            {
                handController.MenuButtonPress.AddListener(HandController_MenuButtonPress);
            }
        }

        public void OnDisable()
        {
            if (handController)
            {
                handController.MenuButtonPress.RemoveListener(HandController_MenuButtonPress);
            }
        }

        public void HandController_MenuButtonPress(bool pressed)
        {
            if (pressed)
            {
                source.Request(gameObject);
            }
        }
    }
}
