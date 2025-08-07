using System;
using System.Collections;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ubiq.Rooms
{
    /// <summary>
    /// Joins a Room based on a GUID. Triggered whenever connections change, so
    /// will attempt to join a room on initial connection and later reconnects.
    /// </summary>
    public class RoomJoiner : MonoBehaviour
    {
        [Tooltip("Required.")]
        public RoomGuid Guid;
        [Tooltip("If no NetworkScene supplied, will search for one in scene.")]
        public NetworkScene networkScene;
        [Tooltip("If no RoomClient supplied, will search for one in scene.")]
        public RoomClient roomClient;
        
        [Header("Triggers")]
        [Tooltip("Join will be triggered once during startup.")]
        public bool joinOnStart;
        [Tooltip("Join will be triggered whenever connections are setup or shutdown. Enable this for reconnect behaviour.")]
        public bool joinOnConnectionChange;
        
        private bool isConnectionsModified;
        private Coroutine joinCoroutine;
        
        private void Start()
        {
            networkScene ??= FindAnyObjectByType<NetworkScene>();
            roomClient ??= FindAnyObjectByType<RoomClient>();
                
            if (!networkScene)
            {
                Debug.LogWarning("No NetworkScene found, will not join a room.");
                return;
            }
            
            if (!roomClient)
            {
                Debug.LogWarning("No RoomClient found, will not join a room.");
                return;
            }
            
            if (joinOnStart)
            {
                DoJoin();
            }
            networkScene.OnConnectionsModified
                .AddListener(NetworkScene_OnConnectionsModified);
        }
        
        private void OnDestroy()
        {
            if (networkScene)
            {
                networkScene.OnConnectionsModified
                    .RemoveListener(NetworkScene_OnConnectionsModified);
            }
            
            joinCoroutine = null;
        }
    
        private void NetworkScene_OnConnectionsModified()
        {
            if (isActiveAndEnabled && joinOnConnectionChange && joinCoroutine == null)
            {
                joinCoroutine = StartCoroutine(Join());
            }
        }
        
        private IEnumerator Join()
        {
            // Wait for one frame to avoid spamming join requests if multiple
            // connections are being added at once.
            yield return null;
            
            DoJoin();
            
            joinCoroutine = null;
        }
        
        private void DoJoin()
        {
            if (Guid.Guid.Length <= 0)
            {
                return;
            }
            
            try
            {
                roomClient.Join(Guid);
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning("No RoomClient found");
            }
            catch (FormatException)
            {
                Debug.LogWarning($"The Room Guid {Guid} is not in the correct format");
            }
            catch (OverflowException)
            {
                Debug.LogWarning($"The Room Guid {Guid} is not in the correct format");
            }
        }
    }
}