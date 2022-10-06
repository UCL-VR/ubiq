using System.Collections.Generic;
using Ubiq.Rooms;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Samples.Boids
{
    /// <summary>
    /// Manages the flocks for a client
    /// </summary>
    public class BoidsManager : MonoBehaviour
    {
        private RoomClient client;

        public GameObject boidsPrefab;
        private BoidsParams myBoidsParams;

        public Boids localBoids; // This is a local flock

        private Dictionary<NetworkId, Boids> boids; // This is the list of flocks

        private struct BoidsParams
        {
            public NetworkId sharedId;
        }

        private void Awake()
        {
            client = GetComponentInParent<RoomClient>();
            boids = new Dictionary<NetworkId, Boids>();
            if(localBoids)
            {
                localBoids.NetworkId = NetworkId.Unique();
            }
        }

        private void Start()
        {
            if (localBoids != null)
            {
                myBoidsParams.sharedId = localBoids.NetworkId;
                boids.Add(myBoidsParams.sharedId, localBoids);
            }
            else
            {
                myBoidsParams.sharedId = NetworkId.Unique(); // Create a new Boids
            }

            client.OnPeerAdded.AddListener(OnPeer);
            client.Me["boids-params"] = JsonUtility.ToJson(myBoidsParams);

            MakeUpdateBoids(myBoidsParams, true);
        }

        private Boids MakeBoids(BoidsParams args)
        {
            return GameObject.Instantiate(boidsPrefab, transform).GetComponentInChildren<Boids>();
        }

        private void MakeUpdateBoids(BoidsParams args, bool local)
        {
            if(!boids.ContainsKey(args.sharedId))
            {
                boids.Add(args.sharedId, MakeBoids(args));
            }

            var flock = boids[args.sharedId];
            var go = flock.gameObject;

            if (local)
            {
                go.name = "My Flock #" + args.sharedId.ToString();
            }
            else
            {
                go.name = "Remote Flock #" + args.sharedId.ToString();
            }

            flock.NetworkId = args.sharedId;
            flock.local = local;
        }

        private void OnPeer(IPeer peer)
        {
            if (peer.uuid == client.Me.uuid)
            {
                return;
            }

            var boidsParamsString = peer["boids-params"];
            if (boidsParamsString != null)
            {
                var boidsParams = JsonUtility.FromJson<BoidsParams>(boidsParamsString);
                MakeUpdateBoids(boidsParams, false);
            }
        }
    }

}