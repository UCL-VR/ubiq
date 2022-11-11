using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.XR;
using UnityEngine;
using Ubiq.Spawning;

namespace Ubiq.Samples
{
    public interface IFirework
    {
        void Attach(Hand hand);
    }

    /// <summary>
    /// The Fireworks Box is a basic interactive object. This object uses the NetworkSpawner
    /// to create shared objects (fireworks).
    /// The Box can be grasped and moved around, but note that the Box itself is *not* network
    /// enabled, and each player has their own copy.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FireworksBox : MonoBehaviour, IUseable, IGraspable
    {
        public GameObject FireworkPrefab;

        private Hand follow;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Grasp(Hand controller)
        {
            follow = controller;
        }

        public void Release(Hand controller)
        {
            follow = null;
        }

        public void UnUse(Hand controller)
        {
        }

        public void Use(Hand controller)
        {
            var go = NetworkSpawnManager.Find(this).SpawnWithPeerScope(FireworkPrefab);
            var firework = go.GetComponent<Firework>();
            firework.owner = true;
            if (firework != null)
            {
                firework.Attach(controller);
            }
        }

        private void Update()
        {
            if (follow != null)
            {
                transform.position = follow.transform.position;
                transform.rotation = follow.transform.rotation;
                body.isKinematic = true;
            }
            else
            {
                body.isKinematic = false;
            }
        }
    }
}