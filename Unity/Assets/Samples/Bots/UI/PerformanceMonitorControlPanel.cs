using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples.Bots
{
    public class PerformanceMonitorControlPanel : MonoBehaviour
    {
        private PerformanceMonitor monitor;

        public Text Fps;
        public Button StartMeasurements;

        private void Awake()
        {
            monitor = GetComponentInParent<PerformanceMonitor>();
        }

        private void Start()
        {
            StartMeasurements.onClick.AddListener(() => monitor.StartMeasurements());
        }

        private void Update()
        {
            Fps.text = (1 / Time.deltaTime).ToString();

            if(monitor.Measure)
            {
                StartMeasurements.interactable = false;
            }
        }
    }
}