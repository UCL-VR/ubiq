using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Logging;
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
        NetworkId networkId { set; }
        void Spawned(bool local);
    }

    public class NetworkSpawner : IDisposable
    {
        public NetworkScene scene { get; private set; }
        public RoomClient roomClient { get; private set; }
        public PrefabCatalogue catalogue { get; private set; }
        public string propertyPrefix { get; private set; }

        public delegate void SpawnEventHandler(GameObject gameObject, IRoom room, IPeer peer, NetworkSpawnOrigin origin);
        public event SpawnEventHandler OnSpawned = delegate {};
        public delegate void DespawnEventHandler(GameObject gameObject, IRoom room, IPeer peer);
        public event DespawnEventHandler OnDespawned = delegate {};

        private IRoom prevSpawnerRoom;
        private Dictionary<string, GameObject> spawnedForRoom = new Dictionary<string, GameObject>();
        private Dictionary<IPeer, Dictionary<string,GameObject>> spawnedForPeers = new Dictionary<IPeer, Dictionary<string, GameObject>>();

        private List<string> tmpStrings = new List<string>();
        private List<INetworkSpawnable> tmpSpawnables = new List<INetworkSpawnable>();
        private List<NetworkId> tmpNetworkIds = new List<NetworkId>();

        [Serializable]
        private struct Message
        {
            public string creatorPeerUuid;
            public int catalogueIndex;
            public List<NetworkId> networkIds;
        }

        public NetworkSpawner (NetworkScene scene, RoomClient roomClient,
            PrefabCatalogue catalogue, string propertyPrefix = "ubiq.spawner.")
        {
            this.scene = scene;
            this.roomClient = roomClient;
            this.catalogue = catalogue;
            this.propertyPrefix = propertyPrefix;

            roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
            roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);

            roomClient.OnPeerAdded.AddListener(OnPeerAdded);
            roomClient.OnPeerUpdated.AddListener(OnPeerUpdated);
            roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
        }

        public void Dispose()
        {
            if (roomClient)
            {
                roomClient.OnJoinedRoom.RemoveListener(OnJoinedRoom);
                roomClient.OnRoomUpdated.RemoveListener(OnRoomUpdated);

                roomClient.OnPeerAdded.RemoveListener(OnPeerAdded);
                roomClient.OnPeerUpdated.RemoveListener(OnPeerUpdated);
                roomClient.OnPeerRemoved.RemoveListener(OnPeerRemoved);
            }
        }

        private void OnJoinedRoom(IRoom room)
        {
            // If we've moved room, remove all room scoped spawned objects
            if (room != prevSpawnerRoom)
            {
                foreach (var go in spawnedForRoom.Values)
                {
                    GameObject.Destroy(go);
                }
                spawnedForRoom.Clear();
                prevSpawnerRoom = room;
            }
            UpdateRoom(room);
        }

        private void OnRoomUpdated(IRoom room)
        {
            UpdateRoom(room);
        }

        private void UpdateRoom(IRoom room)
        {
            // Spawn all unspawned objects for this room
            foreach (var item in room)
            {
                var keyMissing = !spawnedForRoom.ContainsKey(item.Key);
                if(keyMissing && item.Key.StartsWith(propertyPrefix))
                {
                    var msg = JsonUtility.FromJson<Message>(item.Value);
                    var go = InstantiateAndSetIds(msg.catalogueIndex,msg.networkIds);
                    spawnedForRoom.Add(item.Key,go);

                    var origin = msg.creatorPeerUuid == roomClient.Me.uuid
                        ? NetworkSpawnOrigin.Local
                        : NetworkSpawnOrigin.Remote;

                    OnSpawned(go,room,peer:null,origin);
                }
            }

            // Remove any spawned items no longer present in properties
            tmpStrings.Clear();
            foreach(var item in spawnedForRoom)
            {
                if (room[item.Key] == string.Empty)
                {
                    tmpStrings.Add(item.Key);
                }
            }

            foreach(var key in tmpStrings)
            {
                var go = spawnedForRoom[key];
                if (go)
                {
                    OnDespawned(go,room,peer:null);
                    GameObject.Destroy(go);
                }
                spawnedForRoom.Remove(key);
            }
        }

        private void OnPeerAdded(IPeer peer)
        {
            UpdatePeer(peer);
        }

        private void OnPeerUpdated(IPeer peer)
        {
            UpdatePeer(peer);
        }

        private void UpdatePeer(IPeer peer)
        {
            Dictionary<string,GameObject> spawned = null;
            spawnedForPeers.TryGetValue(peer, out spawned);

            // Spawn all unspawned objects for this peer
            foreach(var item in peer)
            {
                var keyMissing = spawned == null || !spawned.ContainsKey(item.Key);
                if(keyMissing && item.Key.StartsWith(propertyPrefix))
                {
                    var msg = JsonUtility.FromJson<Message>(item.Value);
                    var go = InstantiateAndSetIds(msg.catalogueIndex,msg.networkIds);

                    if (!spawnedForPeers.ContainsKey(peer))
                    {
                        spawned = new Dictionary<string,GameObject>();
                        spawnedForPeers.Add(peer,spawned);
                    }
                    spawned.Add(item.Key,go);

                    var origin = msg.creatorPeerUuid == roomClient.Me.uuid
                        ? NetworkSpawnOrigin.Local
                        : NetworkSpawnOrigin.Remote;

                    OnSpawned(go,room:null,peer,origin);
                }
            }

            // Remove any spawned items no longer present in properties
            if (spawned == null)
            {
                return;
            }

            tmpStrings.Clear();
            foreach(var item in spawned)
            {
                if (peer[item.Key] == null)
                {
                    tmpStrings.Add(item.Key);
                }
            }

            foreach(var key in tmpStrings)
            {
                var go = spawned[key];
                if (go)
                {
                    OnDespawned(go,room:null,peer);
                    GameObject.Destroy(go);
                }

                spawned.Remove(key);
            }
        }

        private void OnPeerRemoved(IPeer peer)
        {
            // Remove all objects spawned for this peer
            if (spawnedForPeers.TryGetValue(peer, out var spawned))
            {
                foreach(var go in spawned.Values)
                {
                    GameObject.Destroy(go);
                }
                spawnedForPeers.Remove(peer);
            }
        }

        public void Despawn(GameObject gameObject)
        {
            string key = null;
            foreach(var kvp in spawnedForRoom)
            {
                if (kvp.Value == gameObject)
                {
                    key = kvp.Key;
                    break;
                }
            }

            // For room scope objects, despawn when we hear back from server
            if (key != null)
            {
                roomClient.Room[key] = string.Empty;
                return;
            }

            if (key == null && spawnedForPeers.TryGetValue(roomClient.Me,out var spawned))
            {
                foreach(var kvp in spawned)
                {
                    if (kvp.Value == gameObject)
                    {
                        key = kvp.Key;
                        break;
                    }
                }

                // For (local) peer scope objects, despawn immediately
                if (key != null)
                {
                    OnDespawned(gameObject,room:null,peer:roomClient.Me);
                    GameObject.Destroy(gameObject);
                    spawned.Remove(key);

                    roomClient.Me[key] = null;
                }
            }
        }

        /// <summary>
        /// Spawn an entity with peer scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. Should a peer leave scope for any other peer, accompanying
        /// objects for the leaving peer will be removed for the other peer. As
        /// only the local peer can set values in their own properties, the
        /// object is created and immediately accessible.
        /// </summary>
        public GameObject SpawnWithPeerScope(GameObject gameObject)
        {
            var key = $"{ propertyPrefix }{ NetworkId.Unique() }"; // Uniquely id the whole object
            var catalogueIdx = ResolveIndex(gameObject);
            GenerateIds(gameObject,tmpNetworkIds);

            var go = InstantiateAndSetIds(catalogueIdx,tmpNetworkIds);

            if (!spawnedForPeers.ContainsKey(roomClient.Me))
            {
                spawnedForPeers.Add(roomClient.Me,new Dictionary<string, GameObject>());
            }
            spawnedForPeers[roomClient.Me].Add(key,go);
            OnSpawned(go,room:null,roomClient.Me,NetworkSpawnOrigin.Local);

            roomClient.Me[key] = JsonUtility.ToJson(new Message()
            {
                creatorPeerUuid = roomClient.Me.uuid,
                catalogueIndex = catalogueIdx,
                networkIds = tmpNetworkIds
            });

            return go;
        }

        /// <summary>
        /// Spawn an entity with room scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. On leaving the room, objects spawned this way will be
        /// removed locally but will persist within the room. To avoid race
        /// conditions in the shared room dictionary the object is not
        /// immediately accessible.
        /// </summary>
        public void SpawnWithRoomScope (GameObject gameObject)
        {
            var key = $"{ propertyPrefix }{ NetworkId.Unique() }"; // Uniquely id the whole object
            var catalogueIdx = ResolveIndex(gameObject);
            GenerateIds(gameObject,tmpNetworkIds);
            roomClient.Room[key] = JsonUtility.ToJson(new Message()
            {
                creatorPeerUuid = roomClient.Me.uuid,
                catalogueIndex = catalogueIdx,
                networkIds = tmpNetworkIds
            });
        }

        private void GenerateIds(GameObject gameObject, List<NetworkId> outNetworkIds)
        {
            tmpSpawnables.Clear();
            gameObject.GetComponentsInChildren<INetworkSpawnable>(true,tmpSpawnables);

            outNetworkIds.Clear();
            for(int i = 0; i < tmpSpawnables.Count; i++)
            {
                outNetworkIds.Add(NetworkId.Unique());
            }
        }

        // private GameObject InstantiateAndGenerateIds(int catalogueIdx,
        //     List<NetworkId> outNetworkIds)
        // {
        //     var go = InstantiateAndGetSpawnables(catalogueIdx,tmpSpawnables);
        //     outNetworkIds.Clear();
        //     for(int i = 0; i < tmpSpawnables.Count; i++)
        //     {
        //         var id = NetworkId.Unique();
        //         tmpSpawnables[i].networkId = id;
        //         outNetworkIds.Add(id);
        //     }

        //     return go;
        // }

        private GameObject InstantiateAndSetIds(int catalogueIdx,
            List<NetworkId> networkIds)
        {
            var go = GameObject.Instantiate(catalogue.prefabs[catalogueIdx]);
            tmpSpawnables.Clear();
            go.GetComponentsInChildren<INetworkSpawnable>(true,tmpSpawnables);
            for(int i = 0; i < tmpSpawnables.Count; i++)
            {
                tmpSpawnables[i].networkId = networkIds[i];
            }

            return go;
        }

        // private GameObject InstantiateAndGetSpawnables(int catalogueIdx,
        //     List<INetworkSpawnable> outSpawnables)
        // {
        //     var go = GameObject.Instantiate(catalogue.prefabs[catalogueIdx]);
        //     tmpSpawnables.Clear();
        //     go.GetComponentsInChildren<INetworkSpawnable>(true,tmpSpawnables);
        //     return go;
        // }

        private int ResolveIndex(GameObject gameObject)
        {
            var i = catalogue.IndexOf(gameObject);
            Debug.Assert(i >= 0, $"Could not find {gameObject.name} in Catalogue. Ensure that you've added your new prefab to the Catalogue on NetworkSpawner before trying to instantiate it.");
            return i;
        }
    }

    public class NetworkSpawnManager : MonoBehaviour
    {
        public RoomClient roomClient;
        public PrefabCatalogue catalogue;

        public class OnSpawnedEvent : UnityEvent<GameObject,IRoom,IPeer,NetworkSpawnOrigin> {}

        public OnSpawnedEvent OnSpawned = new OnSpawnedEvent();

        private NetworkSpawner spawner;

        private void Reset()
        {
            if(roomClient == null)
            {
                roomClient = GetComponentInParent<RoomClient>();
            }
#if UNITY_EDITOR
            if(catalogue == null)
            {
                try
                {
                    var asset = UnityEditor.AssetDatabase.FindAssets("Prefab Catalogue").FirstOrDefault();
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                    catalogue = UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabCatalogue>(path);
                }
                catch
                {
                    // if the default prefab has gone away, no problem
                }
            }
#endif
        }

        private void Start()
        {
            spawner = new NetworkSpawner(NetworkScene.Find(this),roomClient,catalogue);
            spawner.OnSpawned += Spawner_OnSpawned;
        }

        private void OnDestroy()
        {
            spawner.OnSpawned -= Spawner_OnSpawned;
            spawner.Dispose();
            spawner = null;
        }

        /// <summary>
        /// Spawn an entity with peer scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. Should a peer leave scope for any other peer, accompanying
        /// objects for the leaving peer will be removed for the other peer. As
        /// only the local peer can set values in their own properties, the
        /// object is created and immediately accessible.
        /// </summary>
        public GameObject SpawnWithPeerScope(GameObject gameObject)
        {
            if (spawner != null)
            {
                return spawner.SpawnWithPeerScope(gameObject);
            }
            return null;
        }

        /// <summary>
        /// Spawn an entity with room scope. All Components implementing the
        /// INetworkSpawnable interface will have their NetworkIDs set and
        /// synced. On leaving the room, objects spawned this way will be
        /// removed locally but will persist within the room. To avoid race
        /// conditions in the shared room dictionary the object is not
        /// immediately accessible.
        /// </summary>
        public void SpawnWithRoomScope(GameObject gameObject)
        {
            if (spawner != null)
            {
                spawner.SpawnWithRoomScope(gameObject);
            }
        }

        private void Spawner_OnSpawned(GameObject gameObject, IRoom room,
            IPeer peer, NetworkSpawnOrigin origin)
        {
            gameObject.transform.parent = transform;
            OnSpawned.Invoke(gameObject,room,peer,origin);
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