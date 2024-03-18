using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Examples
{
    public class _09_HintToggleButton : MonoBehaviour
    {
        public enum HintType
        {
            Position,
            Rotation,
            LookAt
        }

        public HintType hintType;
        public _09_HintSwapper hintSwapper;

        private UnityEngine.UI.Button button;
        private Color defaultColor;
        private Color selectedColor;
        private bool isSelected;

        void Start()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            defaultColor = button.colors.normalColor;
            selectedColor = button.colors.disabledColor;
            button.onClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            switch(hintType)
            {
                case HintType.Position: hintSwapper.TogglePositionHint();break;
                case HintType.Rotation: hintSwapper.ToggleRotationHint();break;
            }
            isSelected = !isSelected;
            var colors = button.colors;
            colors.normalColor = isSelected ? selectedColor : defaultColor;
            colors.selectedColor = isSelected ? selectedColor : defaultColor;
            button.colors = colors;
        }
    }
}