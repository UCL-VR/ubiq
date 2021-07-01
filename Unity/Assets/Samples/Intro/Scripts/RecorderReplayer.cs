using System.Collections;
using System;
using System.Linq;
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

public class MessagePack
{
    public List<byte[]> messages;

    public void AddMessage(byte[] message)
    {
        messages.Add(message);
    }
    public MessagePack()
    {
        messages = new List<byte[]>();
        messages.Add(new byte[4]); // save space for size?
    }

    public MessagePack(byte[] messagePack) // 4 byte at beginning for size
    {
        messages = new List<byte[]>();
        messages.Add(new byte[] { messagePack[0], messagePack[1] , messagePack[2] , messagePack[3] }); 

        int i = 4;
        while (i < messagePack.Length) // error here!!!
        {
            int lengthMsg = BitConverter.ToInt32(messagePack, i);
            i += 4;
            byte[] msg = new byte[lengthMsg];
            Buffer.BlockCopy(messagePack, i, msg, 0, lengthMsg);
            messages.Add(msg);
            i += lengthMsg;
        }
    }

    public byte[] GetBytes()
    {
        byte[] toBytes = messages.SelectMany(a => a).ToArray();
        byte[] l = BitConverter.GetBytes(toBytes.Length - 4); // only need length of package not length of package + 4 byte of length
        toBytes[0] = l[0]; toBytes[1] = l[1]; toBytes[2] = l[2]; toBytes[3] = l[3];
        //int t = BitConverter.ToInt32(messages[0], 0);
        //if (BitConverter.IsLittleEndian)
        //    Array.Reverse(messages[0]);
        //t = BitConverter.ToInt32(messages[0], 0);
        return toBytes;

    }
}

public class SingleMessage3
{
    public byte[] message; // whole message including object and component ids
    public SingleMessage3(byte[] message)
    {
        this.message = message;
    }
    public byte[] GetBytes()
    {
        byte[] bLength = BitConverter.GetBytes(message.Length);
        //if (BitConverter.IsLittleEndian)
        //    Array.Reverse(bLength);
        byte[] toBytes = new byte[bLength.Length + message.Length];
        Buffer.BlockCopy(bLength, 0, toBytes, 0, bLength.Length);
        Buffer.BlockCopy(message, 0, toBytes, bLength.Length, message.Length);
        return toBytes;
    }
    //public SingleMessage3(byte[] bytes) // length + message
    //{
    //    message = new byte[bytes.Length - 4]; // 4 bytes per int
    //    Buffer.BlockCopy(bytes, 4, message, 0, message.Length);
    //}
}

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
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bFrame);
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
    private Dictionary<NetworkId, string> recordedObjectids;
    //private int lineNr = 0; // number of lines in recordFile
    private List<int> pckgSizePerFrame;
    private List<int> idxFrameStart; // start of a specific frame in the recorded data
    private int frameNr = 0;
    private int previousFrame = 0;
    private bool initFile = false;
    private MessagePack messages = null;

    private List<float> frameTimes = new List<float>(); 
    private float recordingStartTime = 0.0f;

    public Recorder(RecorderReplayer recRep)
    {
        this.recRep = recRep;
        recordedObjectids = new Dictionary<NetworkId, string>();
        pckgSizePerFrame = new List<int>();
        idxFrameStart = new List<int>();

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
            
            // start recoding time
            recordingStartTime = Time.unscaledTime;
            idxFrameStart.Add(0); // first frame in byte data has idx 0

            initFile = true;

        }

        string uid;
        if (obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            // that does not work for already replayed avatars because they do not have properties
            uid = (obj as Avatar).Properties["texture-uid"]; // get texture of avatar so we can later replay a look-alike avatar
            if (frameNr == 0 || previousFrame != frameNr) // went on to next frame so generate new message pack
            {

                if (messages != null)
                {
                    byte[] bMessages = messages.GetBytes();
                    binaryWriter.Write(bMessages);
                    pckgSizePerFrame.Add(bMessages.Length);
                    var idx = idxFrameStart[idxFrameStart.Count - 1] + bMessages.Length;
                    idxFrameStart.Add(idx);
                    frameTimes.Add(Time.unscaledTime - recordingStartTime);
                    Debug.Log("Pack size: " + bMessages.Length); // length includes 4 byte of size
                    //test++;
                    //Debug.Log(test);
                }
                messages = new MessagePack();
                previousFrame++;
            }

            SingleMessage3 recMsg = new SingleMessage3(message.bytes);
            byte[] recBytes = recMsg.GetBytes();
          
            messages.AddMessage(recBytes);
            //SingleMessage3 test = new SingleMessage3(recMsg.GetBytes());
            //Debug.Log(message.objectid.ToString() + " " + frameNr);

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
            Debug.Log("Save recording info");
            //recStream.Dispose();
            binaryWriter.Dispose();

            Debug.Log("FrameNr, pckgsize, idxFrameStart" + frameNr + " " + pckgSizePerFrame.Count + " " + idxFrameStart.Count);
           
            File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(frameNr-1, recordedObjectids.Count,
                new List<NetworkId>(recordedObjectids.Keys), new List<string>(recordedObjectids.Values), frameTimes, pckgSizePerFrame, idxFrameStart)));

            recordedObjectids = null;
            recordFileIDs = null;
            pckgSizePerFrame.Clear();
            idxFrameStart.Clear();
            frameTimes.Clear();
            recordingStartTime = 0.0f;

            initFile = false;
            frameNr = 0;

            Debug.Log("Recording info saved");

        }
    }
}

[System.Serializable]
public class Replayer
{
    // Replaying
    RecorderReplayer recRep;

    private NetworkSpawner spawner;

    private ReferenceCountedSceneGraphMessage[][] replayedMessages;
    private int[] replayedFrames;
    private RecordingInfo recInfo;
    private int currentReplayFrame = 0;
    // later for the recording of other objects consider not only saving the networkid but additional info such as class
    // maybe save info in Dictionary list and save objectid (key) and values (list: class, (if avatar what avatar type + texture info)
    private Dictionary<NetworkId, string> replayedObjectids; // avatar IDs and texture
    private Dictionary<NetworkId, ReplayedObjectProperties> replayedObjects; // new objectids! 
    private Dictionary<NetworkId, NetworkId> oldNewObjectids;
    private bool loadingStarted = false; // set to true once loading recorded data starts
    //private bool loaded = false; // set to true once all recorded data is loaded
    //private int msgIndex = 0; // for replaying from file where every msg is in separate line to get correct index for messages in next frame
    private FileStream streamFromFile;
    private float replayingStartTime = 0.0f;
    private int t1 = 0;
    private int t2 = 0;


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
        if (recInfo != null)
        {
            //t1++;
            if (recInfo.frameTimes[currentReplayFrame] <= (Time.unscaledTime - replayingStartTime))
            {
                //t2++;
                //Debug.Log("times " + recInfo.frameTimes[currentReplayFrame] + " " + (Time.unscaledTime - replayingStartTime));
                ReplayFromFile();
                currentReplayFrame++;
                if (currentReplayFrame == recInfo.frames)
                {
                    currentReplayFrame = 0;
                    streamFromFile.Position = 0;
                    replayingStartTime = Time.unscaledTime;
                }
            }
            //Debug.Log(t1 + " " + t2);

        }
        //else
        //{
        //if (loaded)
        //{
        //    ReplayMessagesPerFrame();
        //    if (currentReplayFrame == recInfo.frames - 1)
        //    {
        //        currentReplayFrame = 0;
        //        msgIndex = 0;
        //    }
        //}
        //  }

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
            Debug.Log(recInfo.frames + " " + recInfo.frameTimes.Count + " " + recInfo.pckgSizePerFrame.Count);

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
            //if (recRep.replayFromFile) // initialize Stream reader 
            //{
            streamFromFile = File.Open(filepath, FileMode.Open); // dispose once replaying is done
            replayingStartTime = Time.unscaledTime;

            //}
            //else // load whole dataset as usual
            //{
                //Debug.Log("Load recording...");
                //loaded = await LoadMessages(filepath);
                //Debug.Log("Recording loaded!");
            //}   
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
        using (FileStream stream = File.Open(filepath, FileMode.Open))
        {
            int i = 0;
            replayedFrames = new int[recInfo.frames];
            byte[] msgs;
            replayedMessages = new ReferenceCountedSceneGraphMessage[recInfo.frames][];
            while (i < recInfo.frames)
            {
                msgs = new byte[recInfo.pckgSizePerFrame[i]];
                await stream.ReadAsync(msgs, 0, msgs.Length);

                MessagePack mp = new MessagePack(msgs);
                replayedMessages[i] = CreateRCSGMs(mp);
                i++;
            }
        }
        return true;
    }

    private ReferenceCountedSceneGraphMessage[] CreateRCSGMs(MessagePack messagePack)
    {
        ReferenceCountedSceneGraphMessage[] rcsgms = new ReferenceCountedSceneGraphMessage[messagePack.messages.Count-1]; // first index is size
        for (int i = 1; i < messagePack.messages.Count; i++) // start at index 1 bc 0 is size
        {
            byte[] msg = messagePack.messages[i];
            rcsgms[i - 1] = CreateRCSGM(msg);
        }
        return rcsgms;
    }

    private ReferenceCountedSceneGraphMessage CreateRCSGM(byte[] msg)
    {
        ReferenceCountedMessage rcm = new ReferenceCountedMessage(msg);
        ReferenceCountedSceneGraphMessage rcsgm = new ReferenceCountedSceneGraphMessage(rcm);
        NetworkId id = new NetworkId(msg, 0);
        rcsgm.objectid = oldNewObjectids[id];
        return rcsgm;
    }

    private void ReplayFromFile()
    {
        var pckgSize = recInfo.pckgSizePerFrame[currentReplayFrame];
        streamFromFile.Position = recInfo.idxFrameStart[currentReplayFrame];
        byte[] msgPack = new byte[pckgSize];

        var numberBytes = streamFromFile.Read(msgPack, 0, pckgSize);

        int i = 4;
        while (i < numberBytes)
        {
            int lengthMsg = BitConverter.ToInt32(msgPack, i);
            i += 4;
            byte[] msg = new byte[lengthMsg];
            Buffer.BlockCopy(msgPack, i, msg, 0, lengthMsg);

            ReferenceCountedSceneGraphMessage rcsgm = CreateRCSGM(msg);
            ReplayedObjectProperties props = replayedObjects[rcsgm.objectid];
            INetworkComponent component = props.components[rcsgm.componentid];

            // send and replay remotely
            recRep.scene.Send(rcsgm);
            // replay locally
            component.ProcessMessage(rcsgm);

            i += lengthMsg;
        }
    }

    private void ReplayMessagesPerFrame()
    {
        Debug.Log("Replay messages...");

        foreach (var message in replayedMessages[currentReplayFrame])
        {
            ReplayedObjectProperties props = replayedObjects[message.objectid];
            INetworkComponent component = props.components[message.componentid];

            // send and replay remotely
            recRep.scene.Send(message);

            // replay locally
            component.ProcessMessage(message);

        }
        //msgIndex = msgIndex + msgsPerFrame;
        currentReplayFrame++;
        //Debug.Log(currentReplayFrame + " " + msgIndex);

    }

    public void Cleanup()
    {
        Debug.Log("Cleanup");
        loadingStarted = false;
        currentReplayFrame = 0;
        replayingStartTime = 0.0f;
        recInfo = null;

        if (replayedObjects.Count > 0)
        {
            foreach (var ids in replayedObjects.Keys)
            {
                spawner.UnspawnPersistent(ids);
            }
            replayedObjects.Clear();
        }

        oldNewObjectids.Clear();
        
        if (streamFromFile != null)
            streamFromFile.Close();
        // remove avatars

    }

}



[System.Serializable]
public class RecordingInfo
{
    public int[] listLengths;
    public int frames;
    public int avatarNr;
    public List<NetworkId> objectids;
    public List<string> textures;
    public List<float> frameTimes;
    public List<int> pckgSizePerFrame;
    public List<int> idxFrameStart;

    public RecordingInfo(int frames, int avatarNr, List<NetworkId> objectids, List<string> textures, List<float> frameTimes, List<int> pckgSizePerFrame, List<int> idxFrameStart)
    {
        listLengths = new int[3] { frameTimes.Count, pckgSizePerFrame.Count, idxFrameStart.Count };
        this.frames = frames;
        this.avatarNr = avatarNr;
        this.objectids = objectids;
        this.textures = textures;
        this.frameTimes = frameTimes;
        this.pckgSizePerFrame = pckgSizePerFrame;
        this.idxFrameStart = idxFrameStart;
    }
}

public class RecorderReplayer : MonoBehaviour, IMessageRecorder
{
    public NetworkScene scene;
    public string replayFile;
    [HideInInspector] public string recordFile = null;
    [HideInInspector] public string path;
    [HideInInspector] public bool recording, replaying;
    private bool play = false;
    
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

                // stop replaying once recording stops as is does not make sense to see the old replay since there is already a new one
                if (replaying)
                {
                    replaying = false;
                    replayer.Cleanup();
                }

                SetReplayFile();
                recordingAvailable = false; // avoid unnecessary savings of same info (is checked in methods too)
            }
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
        else
        {
            replayer.Cleanup();
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
            //if (t.replaying)
            //{
            //   if (GUILayout.Button(t.play == true ? "Play" : "Pause"))
            //    {
            //        t.play = !t.play;
            //    }
            //}
        }
       
    }
}

