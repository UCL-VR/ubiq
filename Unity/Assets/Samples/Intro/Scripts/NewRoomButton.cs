using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class NewRoomButton : MonoBehaviour
    {
        public SocialMenu mainMenu;
        public Text nameText;
        public bool publish;

        // Expected to be called by a UI element
        public void NewRoom ()
        {
            mainMenu.roomClient.Join(
                name: nameText.text,
                publish: publish);
        }
    }
}
