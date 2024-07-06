using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine.Events;

namespace Ubiq.Spawning
{
    public enum NetworkSpawnOrigin
    {
        Local,
        Remote
    }

    public interface INetworkSpawnable
    {
        NetworkId NetworkId { get; set; }
    }

    public class OnSpawnedEvent : UnityEvent<GameObject, IRoom, IPeer, String> { }
    public class OnDespawnedEvent : UnityEvent<GameObject, IRoom, IPeer> { }

    /// <summary>
    /// Represents a Task of Spawning a specific object. When Spawning into
    /// a Room, this object can be used to wait for the Spawned Object to be
    /// created locally, and to return it. The object can be yielded in a 
    /// co-routine.
    /// </summary>
    public interface INetworkSpawningTask : IEnumerator
    {
        GameObject Spawned { get; }
    }

    public class NetworkSpawner : IDisposable
    {
        public string propertyPrefix { get; private set; }

        private RoomClient roomClient;
        private Transform parent; // Parent for newly spawned GameObjects. It is OK for it to be null.

        // Maps to and from a portable identifier of that Prefab in the project

        private Dictionary<string, GameObject> IdToPrefab = new Dictionary<string, GameObject>();
        private Dictionary<GameObject, string> prefabToId = new Dictionary<GameObject, string>();

        public OnSpawnedEvent OnSpawned = new OnSpawnedEvent();
        public OnDespawnedEvent OnDespawned = new OnDespawnedEvent();

        // All the spawned items currently in existence, keyed by their owner
        // (most likely a Room or Peer, but may be something else in the future...)

        private Dictionary<object, Dictionary<string, GameObject>> spawnedItems = new Dictionary<object, Dictionary<string, GameObject>>();
        private Dictionary<GameObject, object> spawnedToOwner = new Dictionary<GameObject, object>();
        private Dictionary<GameObject, string> spawnedToKey = new Dictionary<GameObject, string>();
        private HashSet<string> keysToDespawn = new HashSet<string>();
        private Dictionary<string, NetworkSpawningTask> tasks = new Dictionary<string, NetworkSpawningTask>();

        [Serializable]
        private struct Message
        {
            public NetworkId creatorPeer;
            public string prefabId;
            public string properties;
        }

        /// <summary>
        /// Adds a GameObject, making it Spawnable. The key of the GameObject is
        /// based on its name. Scene GameObjects or GameObjects created at
        /// runtime can be added, but keep in mind they need to be added at each
        /// Peer before they will work.
        /// </summary>
        public void AddPrefab(GameObject prefab)
        {
            var id = prefab.name;
            var i = 0;
            while (IdToPrefab.ContainsKey(id))
            {
                id = $"{id}{i++}";
            }
            IdToPrefab[id] = prefab;
            prefabToId[prefab] = id;
        }

        /// <summary>
        /// Returns the key used to uniquely identify this Prefab among all
        /// Prefabs when instantiating it.
        /// </summary>
        public string GetPrefabId(GameObject prefab)
        {
            return prefabToId[prefab];
        }

        public NetworkSpawner(RoomClient roomClient, IEnumerable<PrefabCatalogue> catalogues, string propertyPrefix = "ubiq.spawner.")
        {
            foreach (var catalogue in catalogues)
            {
                foreach (var item in catalogue.prefabs)
                {
                    AddPrefab(item);
                }
            }
            this.roomClient = roomClient;
            this.propertyPrefix = propertyPrefix;
            this.parent = NetworkScene.Find(roomClient).transform;
            AddListeners();
        }

        public NetworkSpawner(RoomClient roomClient, PrefabCatalogue catalogue, string propertyPrefix = "ubiq.spawner.")
        {
            foreach (var item in catalogue.prefabs)
            {
                AddPrefab(item);
            }
            this.roomClient = roomClient;
            this.propertyPrefix = propertyPrefix;
            AddListeners();
        }

        private void AddListeners()
        {
            roomClient.OnJoinedRoom.AddListener(UpdateRoom);
            roomClient.OnRoomUpdated.AddListener(UpdateRoom);
            roomClient.OnPeerAdded.AddListener(UpdatePeer);
            roomClient.OnPeerUpdated.AddListener(UpdatePeer);
            roomClient.OnPeerRemoved.AddListener(RemovePeer);
        }

        public void Dispose()
        {
            roomClient.OnJoinedRoom.RemoveListener(UpdateRoom);
            roomClient.OnRoomUpdated.RemoveListener(UpdateRoom);
            roomClient.OnPeerAdded.RemoveListener(UpdatePeer);
            roomClient.OnPeerUpdated.RemoveListener(UpdatePeer);
            roomClient.OnPeerRemoved.RemoveListener(RemovePeer);
        }

        /// <summary>
        /// Given a set of Properties, which describe a list of instantiated
        /// GameObjects and their parameters, belonging to an owner, create or
        /// destroy Spawned GameObjects within it as required. 
        /// The owner object is a regular object used as a key for a particular
        /// scope - it is most likely to be an IRoom or IPeer, though in theory 
        /// any object could be used. This method updates all the dictionaries 
        /// and issues the callbacks. It should be used whereever possible, when
        /// spawned items need to be updated.
        /// </summary>
        private void UpdateSpawned(IEnumerable<KeyValuePair<string, string>> properties, object owner)
        {
            // Check all the keys in the scope's properties against the currently
            // spawned items so see if any need to be created or destroyed.

            spawnedItems.TryGetValue(owner, out var spawned);

            if (spawned == null)
            {
                spawned = new Dictionary<string, GameObject>();
                spawnedItems.Add(owner, spawned);
            }

            foreach (var item in spawned.Keys)
            {
                keysToDespawn.Add(item);
            }

            foreach (var property in properties.Where(p => p.Key.StartsWith(propertyPrefix)))
            {
                if (!spawned.ContainsKey(property.Key))
                {
                    var msg = JsonUtility.FromJson<Message>(property.Value);
                    var local = msg.creatorPeer == roomClient.Me.networkId;
                    var go = InstantiateAndSetIds(property.Key, msg.prefabId, local);
                    spawned.Add(property.Key, go);
                    spawnedToOwner[go] = owner;
                    spawnedToKey[go] = property.Key;
                    OnSpawned.Invoke(go, owner as IRoom, owner as IPeer, msg.properties);
                    if (tasks.TryGetValue(property.Key, out var task))
                    {
                        tasks.Remove(property.Key);
                        task.Fulfill(go);
                    }
                }
                else
                {
                    // This item still exists so mark it as so
                    keysToDespawn.Remove(property.Key);
                }
            }

            // Remove any instances no longer present in the keys

            foreach (var item in keysToDespawn)
            {
                var go = spawned[item];
                if (go)
                {
                    OnDespawned.Invoke(go, owner as IRoom, owner as IPeer);
                    GameObject.Destroy(go);
                }
                spawned.Remove(item);
                spawnedToOwner.Remove(go);
                spawnedToKey.Remove(go);
            }

            keysToDespawn.Clear();
        }

        private void UpdateRoom(IRoom room)
        {
            UpdateSpawned(room.Where(pair => pair.Key.StartsWith(propertyPrefix)), room);
        }

        private void UpdatePeer(IPeer peer)
        {
            UpdateSpawned(peer.Where(pair => pair.Key.StartsWith(propertyPrefix)), peer);
        }

        private void RemovePeer(IPeer peer)
        {
            UpdateSpawned(Enumerable.Empty<KeyValuePair<string, string>>(), peer);
            spawnedItems.Remove(peer);
        }

        /// <summary>
        /// Spawn an entity with Peer scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIds set and
        /// synced. Should a Peer leave scope for any reason, its spawned 
        /// objects will be removed.
        /// </summary>
        public GameObject SpawnWithPeerScope(GameObject prefab, string properties = "")
        {
            var key = $"{ propertyPrefix }{ NetworkId.Unique() }"; // Uniquely id the whole object
            roomClient.Me[key] = JsonUtility.ToJson(new Message()
            {
                creatorPeer = roomClient.Me.networkId,
                prefabId = GetPrefabId(prefab),
                properties = properties
            });

            UpdateSpawned(roomClient.Me, roomClient.Me); // This will immediately create the entries in the spawnedItems dictionary for roomClient.Me

            return spawnedItems[roomClient.Me][key];
        }

        /// <summary>
        /// Spawn an entity with Room scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIds set and
        /// synced. On leaving the Room, objects spawned this way will be
        /// removed locally but will persist within the Room.
        /// </summary>
        public INetworkSpawningTask SpawnWithRoomScope(GameObject prefab, string properties = "")
        {
            var key = $"{ propertyPrefix }{ NetworkId.Unique() }"; // Uniquely id the whole object
            roomClient.Room[key] = JsonUtility.ToJson(new Message()
            {
                creatorPeer = roomClient.Me.networkId,
                prefabId = GetPrefabId(prefab),
                properties = properties
            });

            var task = new NetworkSpawningTask();
            tasks.Add(key, task);
            return task;
        }

        private class NetworkSpawningTask : INetworkSpawningTask
        {              
            public void Fulfill(GameObject instance)
            {
                Spawned = instance;
            }

            public object Current => null;
            public GameObject Spawned { get; private set; }
            public bool MoveNext() => Spawned == null;

            public void Reset()
            {
            }
        }

        public void Despawn(GameObject gameObject)
        {
            var owner = spawnedToOwner[gameObject];
            var key = spawnedToKey[gameObject];

            // This snippet sets the key on the room to nothing, which is the
            // same as removing it. When the room changes are reflected back,
            // the object will be despawned by UpdateSpawned.
            if (owner is IRoom)
            {
                (owner as IRoom)[key] = null;
            }

            if (owner is IPeer)
            {
                if (owner != roomClient.Me)
                {
                    throw new Exception("Cannot Despawn another Peer's GameObject - you can only destroy objects belonging to yourself or the Room");
                }
                else
                {
                    roomClient.Me[key] = null;
                    UpdateSpawned(roomClient.Me, roomClient.Me);
                }
            }
        }

        private static NetworkId ParseNetworkId(string key, string propertyPrefix)
        {
            if (key.Length > propertyPrefix.Length)
            {
                return new NetworkId(key.Substring(propertyPrefix.Length));
            }
            return NetworkId.Null;
        }

        private GameObject InstantiateAndSetIds(string propertyKey, string prefabId, bool local)
        {
            var networkId = ParseNetworkId(propertyKey, propertyPrefix);
            if (networkId == NetworkId.Null)
            {
                return null;
            }

            var go = GameObject.Instantiate(IdToPrefab[prefabId], parent);

            uint i = 1;
            foreach (var item in go.GetComponentsInChildren<INetworkSpawnable>(true))
            {
                item.NetworkId = NetworkId.Create(networkId, i++);
            } 

            return go;
        }
    }

    public class NetworkSpawnManager : MonoBehaviour
    {
        // As the contents of this list are processed on Awake, it can only
        // be updated at design time.
        [SerializeField]
        private List<PrefabCatalogue> Catalogues = new List<PrefabCatalogue>();

        public OnSpawnedEvent OnSpawned => spawner.OnSpawned;
        public OnDespawnedEvent OnDespawned => spawner.OnDespawned;

        private NetworkSpawner spawner;

        private void Reset()
        {
#if UNITY_EDITOR
            if (Catalogues.Count <= 0)
            {
                try
                {
                    var asset = UnityEditor.AssetDatabase.FindAssets("Prefab Catalogue").FirstOrDefault();
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                    Catalogues.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabCatalogue>(path));
                }
                catch
                {
                    // if the default prefab has gone away, no problem
                }
            }
#endif
        }

        private void Awake()
        {
            spawner = new NetworkSpawner(RoomClient.Find(this), Catalogues);
        }

        private void OnDestroy()
        {
            spawner.Dispose();
        }

        /// <summary>
        /// Spawn an entity with Peer scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. Should a Peer leave the scope of another Peer, for example,
        /// by moving to a different Room, that all the objects created by this
        /// Peer will be removed from that other Peer.
        /// </summary>
        public GameObject SpawnWithPeerScope(GameObject gameObject)
        {
            return spawner.SpawnWithPeerScope(gameObject);
        }

        /// <summary>
        /// Spawn an entity with Peer scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. Should a Peer leave the scope of another Peer, for example,
        /// by moving to a different Room, that all the objects created by this
        /// Peer will be removed from that other Peer.
        /// Includes a Properties field that follows the Instance and is passed
        /// to every Peer that spawns the object via OnSpawned. The properties
        /// cannot be changed once the object is spawned.
        /// </summary>
        public GameObject SpawnWithPeerScope(GameObject gameObject, string properties)
        {
            return spawner.SpawnWithPeerScope(gameObject, properties);
        }

        /// <summary>
        /// Spawn an entity with Room scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. On leaving the Room, objects spawned this way will be
        /// removed locally but will persist within the Room. This is the case
        /// even for the Peer that created the object.
        /// </summary>
        public INetworkSpawningTask SpawnWithRoomScope(GameObject gameObject)
        {
            return spawner.SpawnWithRoomScope(gameObject);
        }

        /// <summary>
        /// Spawn an entity with Room scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. On leaving the Room, objects spawned this way will be
        /// removed locally but will persist within the Room. This is the case
        /// even for the Peer that created the object.
        /// Includes a Properties field that follows the Instance and is passed
        /// to every Peer that spawns the object via OnSpawned. The properties
        /// cannot be changed once the object is spawned.
        /// </summary>
        public INetworkSpawningTask SpawnWithRoomScope(GameObject gameObject, string properties)
        {
            return spawner.SpawnWithRoomScope(gameObject, properties);
        }

        public void Despawn (GameObject gameObject)
        {
            spawner.Despawn(gameObject);
        }

        public static NetworkSpawnManager Find(MonoBehaviour component)
        {
            var scene = NetworkScene.Find(component);
            if (scene)
            {
                return scene.GetComponentInChildren<NetworkSpawnManager>();
            }
            return null;
        }
    }
}