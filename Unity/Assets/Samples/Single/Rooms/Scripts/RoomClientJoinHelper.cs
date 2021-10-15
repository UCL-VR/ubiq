using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ubiq.Samples.Minimal.Rooms
{
    /// <summary>
    /// Joins all the RoomClients in a scene into one Room
    /// </summary>
    public class RoomClientJoinHelper : MonoBehaviour
    {
        public void JoinAllRoomClients()
        {
            var roomClients = new List<RoomClient>();

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var item in roots)
            {
                roomClients.AddRange(item.GetComponentsInChildren<RoomClient>());
            }

            if (roomClients.Count <= 0)
            {
                return;
            }

            // pick the first roomclient to join
            var primary = roomClients.First();

            roomClients.Remove(primary);

            primary.OnJoinedRoom.AddListener((IRoom room) =>
            {
                foreach (var item in roomClients)
                {
                    item.Join(primary.Room.JoinCode);
                }
            });

            primary.Join(name: $"Room Client Join Helper {Random.Range(0, 1000)}", false); // we will get the roomcode in the callback, so we dont need to make these public
        }
    }
}