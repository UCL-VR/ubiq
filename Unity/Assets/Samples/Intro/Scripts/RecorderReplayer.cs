using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEditor;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Networking;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Spawning;
using Ubiq.Logging.Utf8Json;

// recorded messages per frame
public class SingleMessage2
{
    public int frame; // 4 bytes
    public byte[] message; // whole message including object and component ids
    public SingleMessage2(int frame, byte[] message)
    {
        this.frame = frame;
        this.message = message;
    }
    public byte[] GetBytes()
    {
        byte[] bFrame = BitConverter.GetBytes(frame);
        byte[] toBytes = new byte[bFrame.Length + message.Length];
        Buffer.BlockCopy(bFrame, 0, toBytes, 0, bFrame.Length);
        Buffer.BlockCopy(message, 0, toBytes, bFrame.Length, message.Length);
        return toBytes;
    }
    public SingleMessage2(byte[] bytes) // bytes does not contain length!(just frame and message)
    {
        message = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, message, 0, message.Length);
        frame = BitConverter.ToInt32(bytes, 0);
    }
}

[System.Serializable]
public class SingleMessage
{
    public int frame;
    [SerializeField]
    public NetworkId objectid;
    public ushort componentid;
    public byte[] message;
    public SingleMessage(int frame, NetworkId objectid, ushort componentid, byte[] message)
    {
        this.frame = frame;
        this.objectid = objectid;
        this.componentid = componentid;
        this.message = message;
    }
}

public class Recorder
{
    // Recording
    private RecorderReplayer recRep;

    private FileStream recStream;
    private BinaryWriter binaryWriter;
    private string recordFileIDs; // save the objectIDs of the recorded avatars
    private string recordedData;  // format of recorded data: (time), frame, object ID, component ID, sgbmessage
    private Dictionary<NetworkId, string> recordedObjectids;
    private int lineNr = 0; // number of lines in recordFile
    private int frameNr = 0;
    private int previousFrame = 0;
    private bool initFile = false;

    public Recorder(RecorderReplayer recRep)
    {
        this.recRep = recRep;
        recordedObjectids = new Dictionary<NetworkId, string>();

    }

    // so we know how many of the messages belonge to one frame,
    // this is called after all connections have received their messages after one Update()
    public void NextFrame()
    {
        previousFrame = frameNr;
        frameNr += 1;
    }

    public bool IsRecording()
    {
        Debug.Log("Recording...");
        return recRep.recording;
    }

    public void RecordMessage(INetworkObject obj, ReferenceCountedSceneGraphMessage message)
    {
        if (!initFile)
        {
            recRep.recordFile = recRep.path + "/rec" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            recordFileIDs = recRep.recordFile + "IDs.txt";
            //recRep.recordFile = recRep.recordFile + ".txt";
            recRep.recordFile = recRep.recordFile + ".dat";
            recordedObjectids = new Dictionary<NetworkId, string>();

            //recStream = File.Open(recordFile, FileMode.OpenOrCreate);
            binaryWriter = new BinaryWriter(File.Open(recRep.recordFile, FileMode.OpenOrCreate)); // dispose when recording is finished

            initFile = true;
        }

        string uid;
        if (obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            // that does not work for already replayed avatars because they do not have properties
            uid = (obj as Avatar).Properties["texture-uid"]; // get texture of avatar so we can later replay a look-alike avatar
            if(frameNr == 0) // to make sure we do not start with frame 0
            {
                NextFrame();
            }
            //string recMsg = JsonUtility.ToJson(new SingleMessage(frameNr, message.objectid, message.componentid, message.bytes));
            //File.AppendAllText(recRep.recordFile, recMsg + "\n");
            SingleMessage2 recMsg = new SingleMessage2(frameNr, message.bytes);
            binaryWriter.Write(recMsg.GetBytes());
            //SingleMessage2 test = new SingleMessage2(recMsg.GetBytes());
            //Debug.Log(message.objectid.ToString() + " " + frameNr);
            lineNr += 1;

            if (!recordedObjectids.ContainsKey(message.objectid))
            {
                recordedObjectids.Add(message.objectid, uid);
            }
        }
    }

    public void SaveRecordingInfo()
    {
        if (recordedObjectids != null && recordFileIDs != null) // save objectids and texture uids once recording is done
        {
            //recStream.Dispose();
            binaryWriter.Dispose();

            File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(lineNr, frameNr, recordedObjectids.Count,
                new List<NetworkId>(recordedObjectids.Keys), new List<string>(recordedObjectids.Values))));

            recordedObjectids = null;
            recordFileIDs = null;
        }
    }

    public void ResetVariables()
    {
        initFile = false;
        frameNr = 0;
        lineNr = 0;
    }

}
[System.Serializable]
public class Replayer
{
    // Replaying
    RecorderReplayer recRep;

    private NetworkSpawner spawner;

    private ReferenceCountedSceneGraphMessage[] replayedMessages;
    private int[] replayedFrames;
    private RecordingInfo recInfo;
    private int currentReplayFrame = 0;
    // later for the recording of other objects consider not only saving the networkid but additional info such as class
    // maybe save info in Dictionary list and save objectid (key) and values (list: class, (if avatar what avatar type + texture info)
    private Dictionary<NetworkId, string> replayedObjectids; // avatar IDs and texture
    private Dictionary<NetworkId, ReplayedObjectProperties> replayedObjects; // new objectids! 
    private Dictionary<NetworkId, NetworkId> oldNewObjectids;
    private bool loadingStarted = false; // set to true once loading recorded data starts
    private bool loaded = false; // set to true once all recorded data is loaded
    private int msgIndex = 0; // for replaying from file where every msg is in separate line to get correct index for messages in next frame

    public Replayer(RecorderReplayer recRep)
    {
        this.recRep = recRep;

        replayedObjectids = new Dictionary<NetworkId, string>();
        replayedObjects = new Dictionary<NetworkId, ReplayedObjectProperties>();
        oldNewObjectids = new Dictionary<NetworkId, NetworkId>();
        spawner = NetworkSpawner.FindNetworkSpawner(recRep.scene);

    }

    private class ReplayedObjectProperties
    {
        public GameObject gameObject;
        public NetworkId id;
        public Dictionary<int, INetworkComponent> components = new Dictionary<int, INetworkComponent>();

    }

    public void Replay(string replayFile)
    {
        if (!loadingStarted)
        {
            LoadRecording(replayFile);
        }

        if (loaded)
        {
            ReplayMessagesPerFrame();

            if (currentReplayFrame == recInfo.frames - 1)
            {
                currentReplayFrame = 0;
                msgIndex = 0;
            }
        }
    }

    private void ReplayMessagesPerFrame()
    {
        Debug.Log("Replay messages...");
        //for (int i = 0; i < replayedMessages.Length; i++)
        //foreach (var message in replayedMessages[currentReplayFrame])

        int msgsPerFrame = replayedFrames[currentReplayFrame];

        for (int i = 0; i < msgsPerFrame; i++)
        {
            Debug.Log("msgindex: " + (msgIndex + i));
            ReferenceCountedSceneGraphMessage message = replayedMessages[msgIndex + i];
            ReplayedObjectProperties props = replayedObjects[message.objectid];
            INetworkComponent component = props.components[message.componentid];

            // send and replay remotely
            recRep.scene.Send(message);

            // replay locally
            component.ProcessMessage(message);

        }
        msgIndex = msgIndex + msgsPerFrame;


        currentReplayFrame++;
        //Debug.Log(currentReplayFrame + " " + msgIndex);
    }

    private void CreateRecordedAvatars()
    {
        foreach (var objectid in recInfo.objectids)
        {
            // if different avatar types are used for different clients change this!
            GameObject prefab = spawner.catalogue.prefabs[3]; // Spawnable Floating BodyA Avatar
                                                              //prefab.GetComponent<RenderToggle>();
            GameObject go = spawner.SpawnPersistent(prefab); // this game object has network context etc. (not the prefab)
            Avatar avatar = go.GetComponent<Avatar>(); // spawns invisible avatar
            Debug.Log("CreateRecordedAvatars() " + avatar.Id);

            oldNewObjectids.Add(objectid, avatar.Id);

            ReplayedObjectProperties props = new ReplayedObjectProperties();
            props.gameObject = go;
            props.id = avatar.Id;
            INetworkComponent[] components = go.GetComponents<INetworkComponent>();
            foreach (var comp in components)
            {
                props.components.Add(NetworkScene.GetComponentId(comp), comp);

            }

            replayedObjects.Add(avatar.Id, props);

        }
    }

    public async void LoadRecording(string replayFile)
    {
        loadingStarted = true;

        string filepath = recRep.path + "/" + replayFile + "IDs.txt";
        if (File.Exists(filepath))
        {
            Debug.Log("Load info...");
            recInfo = await LoadRecInfo(filepath);
            Debug.Log("Info loaded!");

            CreateRecordedAvatars();
        }
        else
        {
            Debug.Log("Invalid replay file ID plath!");
        }

        //filepath = recRep.path + "/" + replayFile + ".txt";
        filepath = recRep.path + "/" + replayFile + ".dat";
        if (File.Exists(filepath))
        {
            Debug.Log("Load recording...");
            loaded = await LoadMessages(filepath);
            Debug.Log("Recording loaded!");
        }
        else
        {
            Debug.Log("Invalid replay file plath!");
        }

    }

    private async Task<RecordingInfo> LoadRecInfo(string filepath)
    {
        RecordingInfo recInfo;
        using (StreamReader reader = File.OpenText(filepath))
        {
            string recString = await reader.ReadToEndAsync();

            recInfo = JsonUtility.FromJson<RecordingInfo>(recString);
        }

        return recInfo;
    }

    private async Task<bool> LoadMessages(string filepath)
    {
        using (StreamReader reader = File.OpenText(filepath))
        //using (FileStream stream = File.Open(filepath, FileMode.Open))
        {
            string msg;
            int i = 0;
            replayedFrames = new int[recInfo.frames];
            replayedMessages = new ReferenceCountedSceneGraphMessage[recInfo.recLinesNr];
            while ((msg = await reader.ReadLineAsync()) != null)
            //var streamLength = stream.Length;
            //var currentPos = stream.Position;
            //while (currentPos < streamLength)
            {
                SingleMessage singleMsg = JsonUtility.FromJson<SingleMessage>(msg);
                //Debug.Log(singleMsg.frame + " " + replayedFrames.Length);
                var idx = singleMsg.frame;
                var pre = replayedFrames[idx-1];
                replayedFrames[idx-1] = pre + 1; // because frameNr starts at 1
                ReferenceCountedMessage buffer = new ReferenceCountedMessage(singleMsg.message);
                ReferenceCountedSceneGraphMessage rcsgm = new ReferenceCountedSceneGraphMessage(buffer);
                rcsgm.objectid = oldNewObjectids[singleMsg.objectid];
                //rcsgm.componentid = singleMsg.componentid; // dont need that because component should always stay the same
                replayedMessages[i] = rcsgm;
                i++;
            }
        }
        return true;
    }
}

[System.Serializable]
public class RecordingInfo
{
    public int recLinesNr;
    public int frames;
    public int avatarNr;
    public List<NetworkId> objectids;
    public List<string> textures;

    public RecordingInfo(int recLinesNr, int frames, int avatarNr, List<NetworkId> objectids, List<string> textures)
    {
        this.recLinesNr = recLinesNr;
        this.frames = frames;
        this.avatarNr = avatarNr;
        this.objectids = objectids;
        this.textures = textures;
    }
}

public class RecorderReplayer : MonoBehaviour, IMessageRecorder
{
    public NetworkScene scene;
    public string replayFile;
    [HideInInspector] public string recordFile = null;
    [HideInInspector] public string path;
    [HideInInspector] public bool recording, replaying;
    
    private Recorder recorder;
    private Replayer replayer;
    private bool recordingAvailable = false;

    private AvatarManager avatarManager;

    
    // Start is called before the first frame update
    void Start()
    {        
        path = Application.dataPath + "/Local/Recordings";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // create Recorder and Replayer
        recorder = new Recorder(this);
        replayer = new Replayer(this);

        avatarManager = scene.GetComponent<AvatarManager>();
    }

    // Update is called once per frame
    void Update()
    {
       if (!recording)
        {
            if (recordingAvailable)
            {
                recorder.SaveRecordingInfo();
                recorder.ResetVariables();
                recordingAvailable = false; // avoid unnecessary savings of same info (is checked in methods too)
            }

            //if(!replaying && recordFile.Length > 0)
            //{
            //    SetReplayFile();           
            //}

        }
       else
        {
            recordingAvailable = true;
        }
        // load file
        // create avatars (avatar manager to get exact avatars) on other clients
        // send messages over network
        if (replaying)
        {
            replayer.Replay(replayFile);
        }

    }

    public void SetReplayFile()
    {
        // sets the previously recorded file as replay file
        replayFile = recordFile.Substring(recordFile.IndexOf("rec"));
        replayFile = replayFile.Remove(replayFile.LastIndexOf(".")); // remove the ".txt", or ".dat"
        Debug.Log("Set replay file to " + replayFile);
        recordFile = null;
    }

    public void RecordMessage(INetworkObject networkObject, ReferenceCountedSceneGraphMessage message)
    {
        recorder.RecordMessage(networkObject, message);
    }

    public void NextFrame()
    {
        recorder.NextFrame();
    }

    public bool IsRecording()
    {
        return recording;
    }
}

[CustomEditor(typeof(RecorderReplayer))]
public class RecorderReplayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var t = (RecorderReplayer)target;
        DrawDefaultInspector();


        if (GUILayout.Button(t.recording == true ? "Stop Recording" : "Record"))
        {
            t.recording = !t.recording;
        }
        if (GUILayout.Button(t.replaying == true ? "Stop Replaying" : "Replay"))
        {
            t.replaying = !t.replaying;
        }
       
    }
}

