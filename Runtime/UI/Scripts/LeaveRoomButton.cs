using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples
{
    public class LeaveRoomButton : MonoBehaviour
    {
        public SocialMenu mainMenu;

        // Expected to be operated by UI
        public void LeaveRoom ()
        {
            if (mainMenu && mainMenu.roomClient)
            {
                mainMenu.roomClient.Join("",false);
            }
        }
    }
}
