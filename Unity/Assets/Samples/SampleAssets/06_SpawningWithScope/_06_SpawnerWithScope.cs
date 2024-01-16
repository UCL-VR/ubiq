using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class _06_SpawnerWithScope : MonoBehaviour
    {
        public Transform spawnPoint;
        private NetworkSpawnManager manager;

        private void Start()
        {
            manager = GetComponentInParent<NetworkSpawnManager>();
            manager.OnSpawned.AddListener(Manager_OnSpawned);
        }

        private void Manager_OnSpawned(GameObject go, IRoom room, IPeer peer, string properties)
        {
            spawnPoint.GetPositionAndRotation(out var pos, out var rot);

            // Add a tiny bit of jitter so the spawned objects don't balance perfectly!
            pos += UnityEngine.Random.Range(0.0f,0.05f) * Vector3.one;

            go.transform.SetPositionAndRotation(pos,rot);

            // When a Peer creates an object it has the option to specify a
            // Properties string that will follow the instance so long as it
            // exists. This string could contain a Json object, for example, or
            // just one parameter. Here, the string is assumed to be an explicit
            // Name.
            if(!string.IsNullOrEmpty(properties))
            {
                go.name = properties;
            }
        }

        public void SpawnWithPeerScope(GameObject prefab)
        {
            // When spawning in the Peer Scope, the spawned objects are available
            // immediately because they belong to the Peer that created them.

            manager.SpawnWithPeerScope(prefab);
        }

        public void SpawnWithRoomScope(GameObject prefab)
        {
            // When spawning at the Room Scope, even the Peer that created them
            // must wait for the Room to be updated, before the instances
            // themselves are avaiable.

            // SpawnWithRoomScope returns a task that can be used for this.

            // In the example below, the task is used to get a reference to the
            // object in order to initialise it.

            StartCoroutine(SpawnWithRoomScopeAndChangeName(prefab));
        }

        private IEnumerator SpawnWithRoomScopeAndChangeName(GameObject prefab)
        {
            var task = manager.SpawnWithRoomScope(prefab, $"My Name was set by {transform.name}");

            // The task can be waited on in a co-routine
            yield return task;

            // After which the spawned object is available
            var spawed = task.Spawned;

            // Here you may want to do something, like highlight it locally
            // or attach it to the players hand.
        }
    }
}