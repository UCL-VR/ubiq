using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class DesktopControlsHints : MonoBehaviour
    {
        public List<GameObject> Hints;

        public Button next;
        public Button hide;

        private int index = 0;

        private void Start()
        {
            next.onClick.AddListener(Next);
            hide.onClick.AddListener(() => { this.gameObject.SetActive(false); });
        }

        public void Next()
        {
            Hints[index].SetActive(false);
            index = (index + 1) % Hints.Count;
            Hints[index].SetActive(true);
        }

        public float offset = 0;

        private void Update()
        {



        }
    }
}