using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Samples;
using Ubiq.Messaging;
using Ubiq.Networking;

public class RecorderReplayerMenu : MonoBehaviour
{
    public NetworkScene scene;
    private RecorderReplayer recRep;

    // Start is called before the first frame update
    void Start()
    {
        recRep = scene.GetComponent<RecorderReplayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(recRep.recording)
        {

        }
        else
        {

        }

       
    }
}
