using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class TextEntry : MonoBehaviour
    {
        public Text text;
        public string defaultText;
        public Color defaultTextColor;
        public Color entryTextColor;

        private bool clearCurrentTextOnInput;

        private void Start()
        {
            ShowDefaultText();
        }

        private void ShowDefaultText()
        {
            text.text = defaultText;
            text.color = defaultTextColor;
            clearCurrentTextOnInput = true;
        }

        private void ClearIfRequired()
        {
            if (clearCurrentTextOnInput) {
                this.text.text = "";
                this.text.color = entryTextColor;
                clearCurrentTextOnInput = false;
            }
        }

        private void ShowDefaultTextIfEmpty()
        {
            if (string.IsNullOrEmpty(this.text.text)) {
                ShowDefaultText();
            }
        }

        public void Clear () {
            ShowDefaultText();
        }

        public void Backspace () {
            ClearIfRequired();
            if (this.text.text.Length > 0)
            {
                this.text.text = this.text.text.Substring(0,this.text.text.Length-1);
            }

            ShowDefaultTextIfEmpty();
        }

        public void Enter (string text) {
            ClearIfRequired();
            this.text.text += text;
        }

        public void Enter (char character) {
            ClearIfRequired();
            this.text.text += character;
        }

        public void SetText (string text, Color color, bool clearOnInput)
        {
            this.text.text = text;
            this.text.color = color;

            clearCurrentTextOnInput = clearOnInput;
        }
    }
}
