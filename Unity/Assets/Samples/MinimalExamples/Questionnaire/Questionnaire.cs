using System.Collections;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples.Single.Questionnaire
{
    public class Questionnaire : MonoBehaviour
    {
        LogEmitter results;

        // Start is called before the first frame update
        void Start()
        {
            results = new ExperimentLogEmitter(this);
        }

        public void Done()
        {
            foreach (var item in GetComponentsInChildren<Slider>())
            {
                results.Log("Answer", item.name, item.value);
            }
        }

        public void Quit()
        {
            LogCollector.Find(this).WaitForTransmitComplete(results.EventType, ready =>
            {
                if(!ready)
                {
                    // Here it may be desirable to to save the logs another way
                    Debug.LogWarning("ActiveCollector at some point changed or did not respond. We cannot say for sure that the logs have been delivered!");
                }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }
    }
}