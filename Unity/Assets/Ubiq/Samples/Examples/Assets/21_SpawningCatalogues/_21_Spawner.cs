using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

namespace Ubiq.Examples
{
    public class _21_Spawner : MonoBehaviour
    {
        public Transform spawnPoint;

        private NetworkSpawnManager manager;
        private List<GameObject> locallySpawned = new List<GameObject>();

        private void Start()
        {
            manager = GetComponentInParent<NetworkSpawnManager>();
            manager.OnSpawned.AddListener(Manager_OnSpawned);
        }

        private void Manager_OnSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
        {
            if (peer == GetComponent<RoomClient>().Me)
            {
                locallySpawned.Add(go);
            }
            spawnPoint.GetPositionAndRotation(out var pos, out var rot);

            // Add a tiny bit of jitter so the spawned objects don't balance perfectly!
            pos += UnityEngine.Random.Range(0.0f,0.05f) * Vector3.one;

            go.transform.SetPositionAndRotation(pos,rot);
        }

        public void Clear()
        {
            foreach(var spawned in locallySpawned)
            {
                manager.Despawn(spawned);
            }
            locallySpawned.Clear();
        }

        public void Spawn(int prefabIndex)
        {
            var prefab = manager.catalogue.prefabs[prefabIndex];
            manager.SpawnWithPeerScope(prefab);
        }
    }
}