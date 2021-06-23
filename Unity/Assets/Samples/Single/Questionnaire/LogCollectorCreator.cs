using System.Collections;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine;

public class LogCollectorCreator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(Application.isEditor)
        {
            gameObject.AddComponent<LogCollector>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
