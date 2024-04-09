using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ubiq.Spawning;
#if XRI_2_5_2_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace Ubiq.Samples
{
    /// <summary>
    /// The Fireworks Box is a basic interactive object. This object uses the NetworkSpawner
    /// to create shared objects (fireworks).
    /// The Box can be grasped and moved around, but note that the Box itself is *not* network
    /// enabled, and each player has their own copy.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FireworksBox : MonoBehaviour
    {
        public GameObject fireworkPrefab;

#if XRI_2_5_2_OR_NEWER
        private NetworkSpawnManager spawnManager;
        private XRGrabInteractable interactable;

        private void Start()
        {
            spawnManager = NetworkSpawnManager.Find(this);
            interactable = GetComponent<XRGrabInteractable>();

            interactable.activated.AddListener(XRGrabInteractable_Activated);
        }

        private void OnDestroy()
        {
            interactable.activated.RemoveListener(XRGrabInteractable_Activated);
        }

        public void XRGrabInteractable_Activated(ActivateEventArgs eventArgs)
        {
            var go = spawnManager.SpawnWithPeerScope(fireworkPrefab);
            var firework = go.GetComponent<Firework>();
            firework.transform.position = transform.position;
            firework.owner = true;
        }
#endif
    }
}