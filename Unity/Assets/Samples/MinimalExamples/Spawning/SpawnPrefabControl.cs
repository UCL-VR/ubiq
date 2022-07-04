using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Spawning;

namespace Ubiq.Samples.Single.Spawning
{
    [RequireComponent(typeof(Button))]
    public class SpawnPrefabControl : MonoBehaviour
    {
        private GameObject prefab;

        public void SetPrefab(GameObject Prefab)
        {
            prefab = Prefab;
            GetComponentInChildren<Text>().text = prefab.name;
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(Spawn);
        }

        private void OnDestroy()
        {
            var button = GetComponent<Button>();
            if (button)
            {
                button.onClick.RemoveListener(Spawn);
            }
        }

        private void Spawn()
        {
            NetworkSpawnManager.Find(this).SpawnWithPeerScope(prefab);
        }
    }
}