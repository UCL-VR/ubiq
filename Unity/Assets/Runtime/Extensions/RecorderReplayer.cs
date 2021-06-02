using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using System.IO;
using System.Text;

public class RecorderReplayer : MonoBehaviour, INetworkObject, INetworkComponent
{
    public bool recording = false;
    public bool replaying = false;

    public string recFileName;

    public NetworkScene scene;

    // format of recorded data: (time), frame, object ID, component ID, sgbmessage
    private string recordedData;
    private int frameNr = 0;

    private string path;

    public NetworkId Id { get; } = new NetworkId();

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        throw new System.NotImplementedException();
    }

    public void Record(ReferenceCountedSceneGraphMessage message)
    { 

        recordedData = Time.unscaledTime + "," + frameNr + "," + message.objectid + "," + message.componentid + "," + message + "\n";

        File.AppendAllText(path + "/" + recFileName, recordedData);
    }

    // so we know how many of the messages belonge to one frame,
    // this is called after all connections have received their messages after one Update()
    public void UpdateFrameNr()
    {
        frameNr += 1;
    }

    // Get all the recordable components from the scene (avatars, objects too later)
    // This should be networked objects (for now). 

    // Start is called before the first frame update
    void Start()
    {
        path = Application.dataPath + "/Local/Recordings";

        if(!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
    }

// Update is called once per frame
void Update()
    {
        // get messages 
        if (recording)
        {

        }
        else
        {
            if (frameNr > 0) // reset frameNr for next recording
                frameNr = 0;
        }
        // load file
        // create avatars (avatar manager to get exact avatars) on other clients
        // send messages over network
        if (replaying)
        {

        }

    }
}

