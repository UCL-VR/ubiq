using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ubiq.Samples
{
    public class JoinRoomClients : MonoBehaviour
    {
        public bool JoinOnStart = false;

        public RoomGuid Guid = null;

        private void Start()
        {
            foreach (var forest in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var item in forest.GetComponentsInChildren<RoomClient>())
                {
                    item.Join(Guid);
                }
            }
            if(NetworkScene.Find(this) is NetworkScene ns)
            {
                ns.GetComponent<RoomClient>().Join(Guid);
            }
        }
    }
}