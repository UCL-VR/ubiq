using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples
{
    public class TextEntryKeyboardListener : MonoBehaviour
    {
        public Keyboard keyboard;
        public TextEntry textEntry;

        void Start()
        {
            keyboard.OnInput.AddListener(Keyboard_OnInput);
        }

        void Keyboard_OnInput(KeyCode keyCode)
        {
            var intKeyCode = (int)keyCode;
            if (keyCode == KeyCode.Backspace)
            {
                textEntry.Backspace();
            }
            else if (intKeyCode >= 97 && intKeyCode <= 122)
            {
                // ASCII codes for a -> z
                if (keyboard.currentKeyCase == Keyboard.KeyCase.Upper)
                {
                    // To uppercase ascii
                    intKeyCode -= 32;
                }

                textEntry.Enter((char)intKeyCode);
            }
            else if (intKeyCode >= 48 && intKeyCode <= 57)
            {
                // ASCII codes for alpha 0 -> alpha 9
                textEntry.Enter((char)intKeyCode);
            }
            else if (intKeyCode == 32)
            {
                // ASCII code for spacebar
                textEntry.Enter((char)intKeyCode);
            }
        }
    }
}