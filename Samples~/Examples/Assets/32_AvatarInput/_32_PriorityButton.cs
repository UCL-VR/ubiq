using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Examples
{
    public class _32_PriorityButton : MonoBehaviour
    {
        public Text priorityText;
        public PoseAvatarInput poseAvatarInput;
        public int changePerClick = 1;
        
        private Button button;
    
        void Start()
        {
            button = GetComponent<Button>();
            
            button.onClick.AddListener(Button_OnClick);
            Refresh();
        }
        
        private void Button_OnClick()
        {
            poseAvatarInput.priority += changePerClick;
            Refresh();
        }
    
        private void Refresh()
        {
            priorityText.text = poseAvatarInput.priority.ToString();           
        }
    }
}