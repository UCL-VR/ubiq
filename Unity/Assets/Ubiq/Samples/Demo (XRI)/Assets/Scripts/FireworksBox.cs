using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ubiq.Spawning;
#if XRI_3_0_7_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#endif

namespace Ubiq.Samples
{
    /// <summary>
    /// The Fireworks Box is a basic interactive object. This object uses the
    /// NetworkSpawner to create shared objects (fireworks). The Box can be
    /// grasped and moved around, but note that the Box itself is *not* network
    /// enabled. Each player has their own copy.
    /// </summary>
    public class FireworksBox : MonoBehaviour
    {
        public GameObject fireworkPrefab;

#if XRI_3_0_7_OR_NEWER
        private NetworkSpawnManager spawnManager;
        private XRSimpleInteractable interactable;
        private XRInteractionManager interactionManager;

        private void Start()
        {
            spawnManager = NetworkSpawnManager.Find(this);
            interactable = GetComponent<XRSimpleInteractable>();
            interactionManager = interactable.interactionManager;
            
            interactable.selectEntered.AddListener(XRGrabInteractable_SelectEntered);
        }

        private void OnDestroy()
        {
            interactable.selectEntered.RemoveListener(XRGrabInteractable_SelectEntered);
        }

        private void XRGrabInteractable_SelectEntered(SelectEnterEventArgs eventArgs)
        {
            var go = spawnManager.SpawnWithPeerScope(fireworkPrefab);
            var firework = go.GetComponent<Firework>();
            firework.transform.position = transform.position;
            firework.owner = true;
            
            if (!interactionManager)
            {
                return;
            }

            // Force the interactor(hand) to stop selecting the box and select the firework
            var selectInteractor = eventArgs.interactorObject;
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