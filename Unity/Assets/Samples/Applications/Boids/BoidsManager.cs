using System.Collections.Generic;
using Ubiq.Rooms;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Samples.Boids
{
    /// <summary>
    /// Manages the flocks for a client
    /// </summary>
    public class BoidsManager : MonoBehaviour, INetworkComponent
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
        }

        private void Start()
        {
            //todo: invert this so params come from avatar object?
            myBoidsParams.sharedId = NetworkScene.GenerateUniqueId();

            if (localBoids != null)
            {
                boids.Add(myBoidsParams.sharedId, localBoids);
            }

            client.OnPeerAdded.AddListener(OnPeer);
            client.Me["boids-params"] = JsonUtility.ToJson(myBoidsParams);

            MakeUpdateBoids(myBoidsParams, true);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
        }

        private Boids MakeBoids(BoidsParams args)
        {
            //todo: turn the prefab reference into a catalogue
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

            flock.Id = args.sharedId;
            flock.local = local;
        }

        private void OnPeer(IPeer peer)
        {
            if (peer.UUID == client.Me.UUID)
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

        // Update is called once per frame
        void Update()
        {

        }
    }

}