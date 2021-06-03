using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using System.IO;
using System.Text;
using Avatar = Ubiq.Avatars.Avatar;

public class RecorderReplayer : MonoBehaviour, INetworkObject, INetworkComponent
{
    public bool recording = false;
    public bool replaying = false;

    public string recFileName;

    public NetworkScene scene;
    public AvatarManager avatarManager;

    // format of recorded data: (time), frame, object ID, component ID, sgbmessage
    private string recordedData;
    private int frameNr = 0;

    private string path;
    private Dictionary<NetworkId, Avatar> avatars;

    public NetworkId Id { get; } = new NetworkId();

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        throw new System.NotImplementedException();
    }

    public void Record(INetworkObject obj, ReferenceCountedSceneGraphMessage message)
    {
        string uid;
        if(obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            //Avatar avatar = obj as Avatar;
            uid = (obj as Avatar).Properties["texture-uid"]; // get texture of avatar so we can later replay a look-alike avatar
            recordedData = Time.unscaledTime + "," + frameNr + "," + message.objectid + "," + message.componentid + "," + uid + "," + message + "\n";

        }
        else
        {

        }

        File.AppendAllText(path + "/" + recFileName, recordedData);
    }

    public void Replay()
    {
        // Load data from file into (dunno yet)
        // keep change of IDs in mind
        // create new avatars and change IDs of messages to be sent to new avatars
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

