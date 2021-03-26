using System.Collections.Generic;
using System;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Dictionaries;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.Avatars
{
    /// <summary>
    /// Manages the avatars for a client
    /// </summary>
    [NetworkComponentId(typeof(AvatarManager), 2)]
    public class AvatarManager : MonoBehaviour
    {
        public AvatarCatalogue Avatars;
        public string localPrefabUuid;

        private RoomClient client;
        private Dictionary<NetworkId, Avatar> avatars;
        private Dictionary<NetworkId, PeerInfo> peers;

        [SerializeField, HideInInspector]
        public Avatar LocalAvatar;

        private class AvatarArgs
        {
            public NetworkId objectId;
            public string prefabUuid;
            public SerializableDictionary properties;

            public AvatarArgs()
            {
                properties = new SerializableDictionary();
            }
        }

        private AvatarArgs localAvatarArgs;


        private void Awake()
        {
            client = GetComponentInParent<RoomClient>();
            avatars = new Dictionary<NetworkId, Avatar>();
            peers = new Dictionary<NetworkId, PeerInfo>();
            localAvatarArgs = new AvatarArgs();
        }

        private void Start()
        {
            client.OnPeer.AddListener(OnPeer);
            client.OnPeerRemoved.AddListener(OnPeerRemoved);
            client.OnJoinedRoom.AddListener(OnJoinedRoom);

            localAvatarArgs.objectId = NetworkScene.GenerateUniqueId();
            localAvatarArgs.prefabUuid = localPrefabUuid;

            if (localAvatarArgs.prefabUuid.Length > 0)
            {
                UpdateAvatar(localAvatarArgs, true);
                UpdatePeer(LocalAvatar);
            }
        }

        /// <summary>
        /// Creates a local Avatar for this peer based on the supplied prefab.
        /// </summary>
        public void CreateLocalAvatar(GameObject prefab)
        {
            localAvatarArgs.prefabUuid = prefab.GetComponent<Avatar>().uuid;
            localPrefabUuid = localAvatarArgs.prefabUuid;
            UpdateAvatar(localAvatarArgs, true);
            UpdatePeer(avatars[localAvatarArgs.objectId]);
        }

        private void UpdateAvatar(AvatarArgs args, bool local)
        {
            // if we have an existing instance, but it is the wrong model, destory it so we can start again

            if (avatars.ContainsKey(args.objectId))
            {
                var existing = avatars[args.objectId];
                if (existing.uuid != args.prefabUuid)
                {
                    Destroy(existing.gameObject);
                    avatars.Remove(args.objectId);
                }
            }

            // create an instance of the correct prefab for this avatar

            if (!avatars.ContainsKey(args.objectId))
            {
                var prefab = Avatars.GetPrefab(args.prefabUuid);
                var created = Instantiate(prefab, transform).GetComponentInChildren<Avatar>();
                created.Id = args.objectId;
                avatars.Add(created.Id, created);
                created.OnUpdated.AddListener(UpdatePeer);

                if (local)
                {
                    if (LocalAvatar != null)
                    {
                        created.transform.localPosition = LocalAvatar.transform.localPosition;
                        created.transform.localRotation = LocalAvatar.transform.localRotation;
                    }
                    LocalAvatar = created;
                }
            }

            // update the avatar instance

            var avatar = avatars[args.objectId];

            avatar.local = local;
            if (local)
            {
                avatar.gameObject.name = "My Avatar #" + avatar.Id.ToString();
            }
            else
            {
                avatar.gameObject.name = "Remote Avatar #" + avatar.Id.ToString();
            }

            avatar.Merge(args.properties);
        }

        private AvatarArgs GetAvatarArgs(Avatar avatar)
        {
            AvatarArgs args;
            if(avatar == LocalAvatar)
            {
                args = localAvatarArgs;
            }
            else
            {
                args = new AvatarArgs();
            }
            args.properties = avatar.Properties;
            args.objectId = avatar.Id;
            args.prefabUuid = avatar.uuid;
            localPrefabUuid = args.prefabUuid;
            return args;
        }

        private void UpdatePeer(Avatar avatar)
        {
            if (avatar.local)
            {
                client.Me["avatar-params"] = JsonUtility.ToJson(GetAvatarArgs(avatar));
            }
        }

        private void OnJoinedRoom()
        {
            foreach (var item in client.Peers)
            {
                OnPeer(item);
            }
        }

        private void OnPeer(PeerInfo peer)
        {
            var parms = peer["avatar-params"];
            if (parms != null)
            {
                var args = JsonUtility.FromJson<AvatarArgs>(parms);
                if (peer.UUID == client.Me.UUID)
                {
                    UpdateAvatar(args, true);
                }
                else
                {
                    UpdateAvatar(args, false);
                }
                peers[args.objectId] = peer;
            }
        }

        private void OnPeerRemoved(PeerInfo peer)
        {
            var parms = peer["avatar-params"];
            if (parms != null)
            {
                var args = JsonUtility.FromJson<AvatarArgs>(parms);
                if(avatars.ContainsKey(args.objectId))
                {
                    Destroy(avatars[args.objectId].gameObject);
                    avatars.Remove(args.objectId);
                }
                if (peers.ContainsKey(args.objectId))
                {
                    peers.Remove(args.objectId);
                }
            }
        }
    }

}