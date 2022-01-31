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
    }
}