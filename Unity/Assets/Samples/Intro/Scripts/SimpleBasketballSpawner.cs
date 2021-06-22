using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;
using Ubiq.Spawning;

namespace Ubiq.Samples
{

    public class SimpleBasketballSpawner : MonoBehaviour
    {
        public GameObject Prefab;

        public void SpawnBasketball()
        {
            NetworkSpawner.Spawn(this, Prefab);
        }
    }
}