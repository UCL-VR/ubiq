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
    /// The Fireworks Box is a basic interactive object. This object uses the
    /// NetworkSpawner to create shared objects (fireworks). The Box can be
    /// grasped and moved around, but note that the Box itself is *not* network
    /// enabled. Each player has their own copy.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FireworksBox : MonoBehaviour
    {
        public GameObject fireworkPrefab;

#if XRI_2_5_2_OR_NEWER
        private NetworkSpawnManager spawnManager;
        private XRGrabInteractable interactable;
        private XRInteractionManager interactionManager;

        private void Start()
        {
            spawnManager = NetworkSpawnManager.Find(this);
            interactable = GetComponent<XRGrabInteractable>();
            interactionManager = interactable.interactionManager;
            
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
            
            if (!interactionManager)
            {
                return;
            }

            // Force the interactor(hand) to drop the box and grab the firework
            var selectInteractor = eventArgs.interactorObject as IXRSelectInteractor;
            if (selectInteractor != null)
            {
                interactionManager.SelectExit(
                    selectInteractor,
                    this.interactable);
                interactionManager.SelectEnter(
                    selectInteractor,
                    firework.GetComponent<XRGrabInteractable>());
            }
        }
#endif
    }
}