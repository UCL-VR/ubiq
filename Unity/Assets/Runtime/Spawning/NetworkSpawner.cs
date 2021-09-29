using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Logging;
using Ubiq.Samples;

namespace Ubiq.Spawning
{
    public interface IUnspawnable
    {
        void Unspawn(bool remove);
    }
    public interface ISpawnable
    {
        NetworkId Id { set; }
        void OnSpawned(bool local);
        bool IsLocal();
    }

    public interface ITexturedObject
    {
        string GetTextureUid();
        Texture2D GetTexture();

        void SetTexture(Texture2D tex);
        void SetTexture(string uid);
    }

    public class NetworkSpawner : MonoBehaviour, INetworkObject, INetworkComponent
    {
        public NetworkId Id { get; } = new NetworkId("a369-2643-7725-a971");

        public RoomClient roomClient;
        public PrefabCatalogue catalogue;

        private NetworkContext context;
        private Dictionary<NetworkId, GameObject> spawned;
        private EventLogger events;

        [Serializable]
        public struct Message // public to avoid warning 0649
        {
            public int catalogueIndex;
            public NetworkId networkId;
            public string peerUuid;
            public bool remove;
            public bool recording;
            public bool replay;
            public bool visible;
            public string uuid;
        }

        public Dictionary<NetworkId, GameObject> GetSpawned()
        {
            return spawned;
        }

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

        private void Awake()
        {
            spawned = new Dictionary<NetworkId, GameObject>();
        }

        void Start()
        {
            context = NetworkScene.Register(this);
            events = new ContextEventLogger(context);
            roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);
            roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
            roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
        }

        private GameObject Instantiate(int i, NetworkId networkId, bool local)
        {
            var go = GameObject.Instantiate(catalogue.prefabs[i], transform);
            go.name = catalogue.prefabs[i].name; // assign prefab name and not prefab name + (Clone), to find correct prefab in catalogue for replay
            var spawnable = go.GetSpawnableInChildren();
            spawnable.Id = networkId;
            spawnable.OnSpawned(local); 
            spawned[networkId] = go;
            events.Log("SpawnObject", i, networkId, local);
            return go;
        }

        private GameObject InstantiateReplay(int i, NetworkId networkId, bool local, bool visible, string uuid)
        {
            Debug.Log("Instantiate Replay");
            GameObject go = Instantiate(i, networkId, local);
            
            // not everything that's replayed is an avatar or has a texture
            if (go.TryGetComponent(out TexturedAvatar texturedAvatar))
            {
                texturedAvatar.SetTexture(uuid);
            }
            if (go.TryGetComponent(out ITexturedObject texturedObject))
            {
                texturedObject.SetTexture(uuid);
            }
            //go.GetComponent<TexturedAvatar>().SetTexture(uuid); // only for avatars (for now)

            go.GetComponent<Outliner>().SetOutline(true); // every object/avatar that can be replayed should have this to distinguish them from the real deal
            Debug.Log("visible is " + visible);
            if (visible)
            {
                go.GetComponent<ObjectHider>().SetLayer(0); // show (default)
            }
            else
            {
                go.GetComponent<ObjectHider>().SetLayer(8); // hide
            }
            
            
            return go;
        }

        public GameObject Spawn(GameObject gameObject)
        {
            var i = ResolveIndex(gameObject);
            var networkId = NetworkScene.GenerateUniqueId();
            context.SendJson(new Message() { catalogueIndex = i, networkId = networkId });
            return Instantiate(i, networkId, true);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.remove)
            {
                Debug.Log("NetworkSpawner ProcessMessage: remove");
                var key = $"SpawnedObject-{ msg.networkId }";
                // might already be removed
                roomClient.Room[key] = JsonUtility.ToJson(new Message() {networkId = msg.networkId, remove = true});
                Destroy(spawned[msg.networkId]);
                spawned.Remove(msg.networkId);
            }
            else if (msg.replay)
            {
                Debug.Log("NetworkSpawner ProcessMessage (replay)");
                InstantiateReplay(msg.catalogueIndex, msg.networkId, false, msg.visible, msg.uuid);

            }
            else
            {
                Debug.Log("NetworkSpawner normal ProcessMessage");
                Instantiate(msg.catalogueIndex, msg.networkId, false);
            }
        }
        // called locally
        public GameObject SpawnPersistentReplay(GameObject gameObject, string uuid)
        {
            var i = ResolveIndex(gameObject);
            var networkId = NetworkScene.GenerateUniqueId();
            //Debug.Log("SpawnPersistentRecording() " + networkId.ToString());
            var key = $"SpawnedObject-{ networkId }";
            GameObject spawned;
            // locally replayed avatars cannot be local otherwise they follow the player
            if (gameObject.GetComponent<Ubiq.Avatars.Avatar>() != null)
            {
                spawned = InstantiateReplay(i, networkId, false, false, uuid); // not local
            }
            else
            {
                spawned = InstantiateReplay(i, networkId, true, false, uuid); // can be local
            }
            roomClient.Room[key] = JsonUtility.ToJson(new Message() { catalogueIndex = i, networkId = networkId, replay = true, visible = false, uuid = uuid});
            return spawned;
        }

        public GameObject SpawnPersistent(GameObject gameObject)
        {
            var i = ResolveIndex(gameObject);
            var networkId = NetworkScene.GenerateUniqueId();
            //Debug.Log("SpawnPersistent() " + networkId.ToString());
            var key = $"SpawnedObject-{ networkId }";
            var spawned = Instantiate(i, networkId, true);

            // let's not record a spawn message, because it would be sent only once, and if we ever wanted to 
            // sample a recording, this message might be skipped unless we find a way to mark messages that must not be skipped
            //if (context.scene.recorder != null && context.scene.recorder.IsRecording())
            //{
            //    context.SendJson(new Message() { catalogueIndex = i, networkId = networkId, replay = true });
            //}

            roomClient.Room[key] = JsonUtility.ToJson(new Message() { catalogueIndex = i, networkId = networkId });
            return spawned;
        }

        public void UnspawnPersistent(NetworkId networkId)
        {
            context.SendJson(new Message() { networkId = networkId, remove = true});
            var key = $"SpawnedObject-{ networkId }";
            // if OnLeftRoom is called too the objects are destroyed there and not here
            if (spawned.ContainsKey(networkId))
            {
                Destroy(spawned[networkId]);
                spawned.Remove(networkId);
            }
            roomClient.Room[key] = JsonUtility.ToJson(new Message() {networkId = networkId, remove = true});
            //Debug.Log("UnspawnPersistent " + networkId.ToString() + "  " + roomClient.Room[key]);
        }

        public void UpdateProperties(NetworkId networkId, string type, object arg)
        {
            Message msg;
            var key = $"SpawnedObject-{ networkId }";
            string prop = roomClient.Room[key];
            if (prop == null || prop == "")
            {
                Debug.Log("Object requested for Update is not in Room properties. It is probably a normal peer.");
                return;
            }
            else
            {
                Debug.Log("Update properties for " + networkId.ToString());
                msg = JsonUtility.FromJson<Message>(prop);
            }
            switch(type)
            {
                case "UpdateVisibility":
                    {
                        roomClient.Room[key] = JsonUtility.ToJson(new Message()
                        {
                            catalogueIndex = msg.catalogueIndex,
                            networkId = msg.networkId,
                            replay = msg.replay,
                            remove = msg.remove,
                            visible = (bool)arg,
                            uuid = msg.uuid
                        });
                        Debug.Log("UpdateVisibility of " + networkId + " to " + arg);
                        break;
                    }
                case "UpdateTexture":
                    {
                        roomClient.Room[key] = JsonUtility.ToJson(new Message()
                        {
                            catalogueIndex = msg.catalogueIndex,
                            networkId = msg.networkId,
                            replay = msg.replay,
                            remove = msg.remove,
                            visible = msg.visible,
                            uuid = (string)arg
                        });
                        Debug.Log("UpdateTexture of " + networkId + " to " + arg);
                        break;
                    }
            }
        }
        // if a recording authority disconnects completely make sure the replays disappear too (and the menu indicators maybe?)
        private void OnPeerRemoved(IPeer peer)
        {
            Debug.Log("NetworkSpawner: OnPeerRemoved");
            if (peer["creator"] == "1")
            {
                foreach (var item in roomClient.Room)
                {
                    if (item.Key.StartsWith("SpawnedObject"))
                    {
                        var msg = JsonUtility.FromJson<Message>(item.Value);

                        UnspawnPersistent(msg.networkId); // remove 

                    }
                }
            }

        }

        // Gets called after OnJoinedRoom
        private void OnRoomUpdated(IRoom room)
        {
            foreach (var item in room)
            {
                if(item.Key.StartsWith("SpawnedObject"))
                {
                    var msg = JsonUtility.FromJson<Message>(item.Value);
                    
                    if (!spawned.ContainsKey(msg.networkId))
                    {
                        
                        if (!msg.remove)
                        {
                            if (msg.replay)
                            {
                                Debug.Log("OnRoom InstantiateReplay" + item.Key);
                                InstantiateReplay(msg.catalogueIndex, msg.networkId, false, msg.visible, msg.uuid);
                            }
                            else
                            {
                                Debug.Log("OnRoom Instantiate" + item.Key);
                                Instantiate(msg.catalogueIndex, msg.networkId, false);
                            }
                        }
                    }
                }
            }
        }
        // Gets called before OnRoomUpdated
        private void OnJoinedRoom(IRoom room)
        {
            // Remove potential objects from previous room
            foreach(var item in spawned)
            {
                Destroy(spawned[item.Key]);
            }
            spawned.Clear();
            //foreach (var item in room)
            //{
            //    if (item.Key.StartsWith("SpawnedObject"))
            //    {
            //        Debug.Log("OnJoinedRoom: " + item.Key);
            //        var msg = JsonUtility.FromJson<Message>(item.Value);

            //        if (spawned.ContainsKey(msg.networkId))
            //        {
            //            Debug.Log("Remove object: " + msg.networkId);
            //            Destroy(spawned[msg.networkId]);
            //            spawned.Remove(msg.networkId);

            //        }              
            //    }
            //}
        }

        private int ResolveIndex(GameObject gameObject)
        {
            var i = catalogue.IndexOf(gameObject);
            Debug.Assert(i >= 0, $"Could not find {gameObject.name} in Catalogue. Ensure that you've added your new prefab to the Catalogue on NetworkSpawner before trying to instantiate it.");
            return i;
        }

        public static GameObject Spawn(MonoBehaviour caller, GameObject prefab)
        {
            var spawner = FindNetworkSpawner(NetworkScene.FindNetworkScene(caller));
            return spawner.Spawn(prefab);
        }
        public static void UnspawnPersistent(MonoBehaviour caller, NetworkId id)
        {
            var spawner = FindNetworkSpawner(NetworkScene.FindNetworkScene(caller));
            spawner.UnspawnPersistent(id);
        }
        public static GameObject SpawnPersistent(MonoBehaviour caller, GameObject prefab)
        {
            var spawner = FindNetworkSpawner(NetworkScene.FindNetworkScene(caller));
            return spawner.SpawnPersistent(prefab);
        }

        public static NetworkSpawner FindNetworkSpawner(NetworkScene scene)
        {
            var spawner = scene.GetComponentInChildren<NetworkSpawner>();
            Debug.Assert(spawner != null, $"Cannot find NetworkSpawner Component for {scene}. Ensure a NetworkSpawner Component has been added.");
            return spawner;
        }
    }

    public static class NetworkSpawnerExtensions
    {
        public static ISpawnable GetSpawnableInChildren(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is ISpawnable).FirstOrDefault() as ISpawnable;
        }
    }


}