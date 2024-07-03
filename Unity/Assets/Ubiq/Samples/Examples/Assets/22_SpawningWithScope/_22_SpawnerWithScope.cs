using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

namespace Ubiq.Examples
{
    public class _22_SpawnerWithScope : MonoBehaviour
    {
        public Transform spawnPoint;
        private NetworkSpawnManager manager;

        private void Start()
        {
            manager = GetComponentInParent<NetworkSpawnManager>();
            manager.OnSpawned.AddListener(Manager_OnSpawned);
        }

        private void Manager_OnSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
        {
            spawnPoint.GetPositionAndRotation(out var pos, out var rot);

            // Add a tiny bit of jitter so the spawned objects don't balance perfectly!
            pos += UnityEngine.Random.Range(0.0f,0.05f) * Vector3.one;

            go.transform.SetPositionAndRotation(pos,rot);
        }

        public void SpawnWithPeerScope(int index)
        {
            if (manager)
            {
                manager.SpawnWithPeerScope(manager.catalogue.prefabs[index]);
            }
        }

        public void SpawnWithRoomScope(int index)
        {
            if (manager)
            {
                manager.SpawnWithRoomScope(manager.catalogue.prefabs[index]);
            }
        }
    }
}