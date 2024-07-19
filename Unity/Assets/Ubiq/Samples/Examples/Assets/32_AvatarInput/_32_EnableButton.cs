using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Examples
{
    public class _32_EnableButton : MonoBehaviour
    {
        public GameObject toToggle;
        public bool pushOnStart;
        
        private Button button;
        
        private Color defaultColor;
        private Color highlightedColor;
        private Color selectedColor;
        private Color disabledColor;
        
        private bool pushed;

        private void Start()
        {
            button = GetComponent<Button>();
            defaultColor = button.colors.normalColor;
            highlightedColor = button.colors.highlightedColor;
            selectedColor = button.colors.selectedColor;
            disabledColor = button.colors.disabledColor;
            
            button.onClick.AddListener(Button_OnClick);
            
            pushed = pushOnStart;
            Refresh();
        }
        
        private void Button_OnClick()
        {
            pushed = !pushed;
            Refresh();
        }
        
        private void Refresh()
        {
            toToggle.SetActive(pushed);
            SetColors(button,pushed);
        }
        
        private void SetColors(Button button, bool pushed)
        {
            var colors = button.colors;
            colors.normalColor = pushed ? defaultColor : disabledColor;
            colors.highlightedColor = pushed ? highlightedColor : disabledColor;
            colors.selectedColor = pushed ? selectedColor : disabledColor;
            button.colors = colors;
        }
    }
}