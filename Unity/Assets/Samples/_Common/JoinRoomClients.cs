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

        public string Guid = null;

        private void Start()
        {
            if(Guid == null || Guid == "")
            {
                Guid = System.Guid.NewGuid().ToString();
            }
            foreach (var forest in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var item in forest.GetComponentsInChildren<RoomClient>())
                {
                    item.Join(System.Guid.Parse(Guid));
                }
            }
            if(NetworkScene.Find(this) is NetworkScene ns)
            {
                ns.GetComponent<RoomClient>().Join(System.Guid.Parse(Guid));
            }
        }
    }
}