using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class Keyboard : MonoBehaviour
    {
        public enum KeyCase
        {
            Upper,
            Lower
        }

        public KeyCase defaultKeyCase = KeyCase.Lower;

        [System.Serializable]
        public class InputEvent : UnityEvent<KeyCode> {};
        public InputEvent OnInput;

        [System.Serializable]
        public class KeyCaseEvent : UnityEvent<KeyCase> {};
        public KeyCaseEvent OnKeyCaseChange;

        public KeyCase currentKeyCase { get; private set; }

        // private Button[] keys;

        // private bool _interactable = true;
        // public bool interactable
        // {
        //     get
        //     {
        //         return _interactable;
        //     }
        //     set
        //     {
        //         if (_interactable != value)
        //         {
        //             SetInteractable(_interactable);
        //         }
        //     }
        // }

        // private void SetInteractable (bool value)
        // {
        //     if (keys == null)
        //     {
        //         return;
        //     }

        //     for (int i = 0; i < keys.Length; i++)
        //     {
        //         keys[i].interactable = value;
        //     }

        //     _interactable = value;
        // }

        private void Awake()
        {
            currentKeyCase = defaultKeyCase;

            // keys = GetComponentsInChildren<Button>();
            // SetInteractable(_interactable);
        }

        public void Input(KeyCode keyCode)
        {
            OnInput.Invoke(keyCode);

            if (currentKeyCase != defaultKeyCase)
            {
                currentKeyCase = defaultKeyCase;
                OnKeyCaseChange.Invoke(currentKeyCase);
            }
            else if (keyCode == KeyCode.LeftShift
                || keyCode == KeyCode.RightShift)
            {
                currentKeyCase = (defaultKeyCase == KeyCase.Lower)
                    ? KeyCase.Upper
                    : KeyCase.Lower;
                OnKeyCaseChange.Invoke(currentKeyCase);
            }
        }
    }
}