using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEditor;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Avatars;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Spawning;
using Ubiq.Rooms;
using Ubiq.Samples;

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

    private BinaryWriter binaryWriter;
    private string recordFileIDs; // save the objectIDs of the recorded avatars
    private Dictionary<NetworkId, string> recordedObjectids;
    //private int lineNr = 0; // number of lines in recordFile
    private List<int> pckgSizePerFrame;
    private List<int> idxFrameStart; // start of a specific frame in the recorded data
    private int frameNr = 0;
    private int previousFrame = 0;
    private int avatarsAtStart = 0;
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
            var dateTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            recRep.recordFile = recRep.path + "/rec" + dateTime + ".dat";
            recordFileIDs = recRep.path + "/IDsrec" + dateTime + ".txt";
            recordedObjectids = new Dictionary<NetworkId, string>();

            idxFrameStart.Add(0); // first frame in byte data has idx 0

            avatarsAtStart = recRep.aManager.Avatars.Count();
            // also consider avatars that are currently visible from a potential previous recording!
            foreach (var props in recRep.replayer.replayedObjects.Values)
            {
                if (props.hider.GetCurrentLayer() == 0) // default visibility layer 
                {
                    avatarsAtStart += 1;
                }    
            }
            //recStream = File.Open(recordFile, FileMode.OpenOrCreate);
            binaryWriter = new BinaryWriter(File.Open(recRep.recordFile, FileMode.OpenOrCreate)); // dispose when recording is finished
            
            // start recoding time
            recordingStartTime = Time.unscaledTime;

            initFile = true;

        }

        string uid;
        if (obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            Debug.Log("Framenr: " + frameNr);
            uid = (obj as Avatar).gameObject.GetComponent<TexturedAvatar>().GetTextureUuid(); // get texture of avatar so we can later replay a look-alike avatar
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
                    //Debug.Log("Pack size: " + bMessages.Length); // length includes 4 byte of size
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
           
            File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(frameNr-1, avatarsAtStart, recordedObjectids.Count,
                new List<NetworkId>(recordedObjectids.Keys), new List<string>(recordedObjectids.Values), frameTimes, pckgSizePerFrame, idxFrameStart)));

            recordedObjectids.Clear();
            recordFileIDs = null;
            pckgSizePerFrame.Clear();
            idxFrameStart.Clear();
            frameTimes.Clear();
            recordingStartTime = 0.0f;
            messages = null;

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
    public RecordingInfo recInfo = null;
    // later for the recording of other objects consider not only saving the networkid but additional info such as class
    // maybe save info in Dictionary list and save objectid (key) and values (list: class, (if avatar what avatar type + texture info)
    private Dictionary<NetworkId, string> replayedObjectids; // avatar IDs and texture
    public Dictionary<NetworkId, ReplayedObjectProperties> replayedObjects; // new objectids! 
    private Dictionary<NetworkId, NetworkId> oldNewObjectids;
    private bool loadingStarted = false; // set to true once loading recorded data starts
    private bool loaded = false;
    //private bool loaded = false; // set to true once all recorded data is loaded
    //private int msgIndex = 0; // for replaying from file where every msg is in separate line to get correct index for messages in next frame
    private FileStream streamFromFile;
    private bool created = false;
    private bool opened = false;
    private bool showAvatarsFromStart = false;
 
    public Replayer(RecorderReplayer recRep)
    {
        this.recRep = recRep;

        replayedObjectids = new Dictionary<NetworkId, string>();
        replayedObjects = new Dictionary<NetworkId, ReplayedObjectProperties>();
        oldNewObjectids = new Dictionary<NetworkId, NetworkId>();
        spawner = recRep.spawner;
    }

    public class ReplayedObjectProperties
    {
        public GameObject gameObject;
        public ObjectHider hider;
        public NetworkId id;
        public Dictionary<int, INetworkComponent> components = new Dictionary<int, INetworkComponent>();

    }
    public void Replay(string replayFile)
    {
        if (!loadingStarted)
        {
            LoadRecording(replayFile);
        }

        if (recRep.play)
        {
                
            if (loaded) // meaning recInfo
            {
                if (!showAvatarsFromStart)
                {
                    Debug.Log("Show avatars from start");
                    for (var i = 0; i < recInfo.avatarsAtStart; i++)
                    {
                        replayedObjects.Values.ElementAt(i).hider.SetNetworkedObjectLayer(0); // show (default layer)
                    }
                    showAvatarsFromStart = true;
                }

                //t1++;
                //test += Time.deltaTime;
                //Debug.Log(test);
                //var currentTime = Time.unscaledTime;
                //Debug.Log("loaded");
                recRep.replayingStartTime += Time.deltaTime;
                var t = recRep.replayingStartTime;
                //var t = currentTime - (recRep.replayingStartTime - recRep.stopTime);
                //Debug.Log("unscaled " + Time.unscaledTime + " start " + recRep.replayingStartTime + " stop " + recRep.stopTime);
                //Debug.Log("before times " + recInfo.frameTimes[recRep.currentReplayFrame] + " " + t);
                var replayTime = recInfo.frameTimes[recRep.currentReplayFrame];
                //var lb = t - 0.01f;

                //if (replayTime < t) // catch up
                //{
                //    //t2++;
                //    below = true;
                //    UpdateFrame();
                //    for (var i = recRep.currentReplayFrame; i < recInfo.frames; i++)
                //    {
                //        replayTime = recInfo.frameTimes[i];
                     
                //        if(replayTime >= t)
                //        {
                //            if (i > 0)
                //            {
                //                var prev = recInfo.frameTimes[i - 1];
                //                if (Math.Abs(replayTime - t) < Math.Abs(prev - t))
                //                {
                //                    recRep.currentReplayFrame = i;
                //                }
                //                else
                //                {
                //                    replayTime = prev;
                //                    recRep.currentReplayFrame = i - 1;
                //                }
                //            }
                //            break;
                //        }

                //    }
                    ReplayFromFile();
                    //Debug.Log("times " + recInfo.frameTimes[recRep.currentReplayFrame] + " " + t + " " + recRep.currentReplayFrame + " " + below);

                    // depending on settings either forwards or backwards
                    UpdateFrame();
                    
                //}
                //t = currentTime - (recRep.replayingStartTime - recRep.stopTime);
                //lb = t - 0.01f;
                //if (replayTime <= t)
                //{
                //}
                //Debug.Log(t1 + " " + t2);
            }
        }
        else // !play 
        {
            if (loaded)
            {
                //Debug.Log("!play");
                recRep.currentReplayFrame = recRep.sliderFrame;
                recRep.stopTime = recInfo.frameTimes[recRep.currentReplayFrame];
           
                ReplayFromFile();
            }
        }
    }

    private void UpdateFrame()
    {
        //if(!recRep.reverse)
        //{
            recRep.currentReplayFrame++;
            if (recRep.currentReplayFrame == recInfo.frames)
            {
                recRep.currentReplayFrame = 0;
                streamFromFile.Position = 0;
            //recRep.replayingStartTime = Time.unscaledTime;
            recRep.replayingStartTime = 0.0f;
                recRep.stopTime = 0.0f;
            }
        //}
        //else
        //{
        //    recRep.currentReplayFrame--;
        //    if (recRep.currentReplayFrame == 0)
        //    {
        //        recRep.currentReplayFrame = 0;
        //        streamFromFile.Position = recInfo.idxFrameStart[recInfo.frames-1];
        //        recRep.replayingStartTime = Time.unscaledTime;
        //        recRep.stopTime = 0.0f;
        //    }
        //}
        recRep.sliderFrame = recRep.currentReplayFrame;

    }

    private bool CreateRecordedAvatars()
    {
        for (var i = 0; i < recInfo.objectids.Count; i++)
        {
            var objectid = recInfo.objectids[i];
            var uuid = recInfo.textures[i];
            ReplayedObjectProperties props = new ReplayedObjectProperties();
            
            // if different avatar types are used for different clients change this!
            GameObject prefab = spawner.catalogue.prefabs[3]; // Spawnable Floating BodyA Avatar
            GameObject go = spawner.SpawnPersistentReplay(prefab, uuid); // this game object has network context etc. (not the prefab)
            Avatar avatar = go.GetComponent<Avatar>(); // spawns invisible avatar
            props.hider = go.GetComponent<ObjectHider>();
            Debug.Log("CreateRecordedAvatars() " + avatar.Id);

            oldNewObjectids.Add(objectid, avatar.Id);

            props.gameObject = go;
            props.id = avatar.Id;
            INetworkComponent[] components = go.GetComponents<INetworkComponent>();
            foreach (var comp in components)
            {
                props.components.Add(NetworkScene.GetComponentId(comp), comp);

            }

            replayedObjects.Add(avatar.Id, props);

        }
        return true;
    }

    public async void LoadRecording(string replayFile)
    {
        loadingStarted = true;

        string filepath = recRep.path + "/IDs" + replayFile + ".txt";
        if (File.Exists(filepath))
        {
            Debug.Log("Load info...");
            recInfo = await LoadRecInfo(filepath);
            Debug.Log(recInfo.frames + " " + recInfo.frameTimes.Count + " " + recInfo.pckgSizePerFrame.Count);
            created = CreateRecordedAvatars();
            Debug.Log("Info loaded!");
        }
        else
        {
            Debug.Log("Invalid replay file ID path!");
            recRep.replaying = false;
            loadingStarted = false;
            
        }

        filepath = recRep.path + "/" + replayFile + ".dat";
        if (File.Exists(filepath))
        {
            opened = OpenStream(filepath);
        }
        else
        {
            Debug.Log("Invalid replay file plath!");
            recRep.replaying = false;
            loadingStarted = false;
        }
        loaded = created && opened;
    }
    private bool OpenStream(string filepath)
    {
        streamFromFile = File.Open(filepath, FileMode.Open); // dispose once replaying is done
        //recRep.replayingStartTime = Time.unscaledTime;
        recRep.replayingStartTime = 0.0f;
        return true;
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
        try
        {
            rcsgm.objectid = oldNewObjectids[id];
        }
        catch (Exception e)
        {
            if (e.Source != null)
                Debug.Log("Network id: " + id + ", Exception source: " + e.Source);
            foreach(var i in oldNewObjectids)
            {
                Debug.Log("old: " + i.Key + " new: " + i.Value);
            }
            throw;
        }    
        return rcsgm;
    }

    
    private void ReplayFromFile()
    {
        //if (!recRep.play)
        //{
        //    HideAll();
        //}

        var pckgSize = recInfo.pckgSizePerFrame[recRep.currentReplayFrame];
        streamFromFile.Position = recInfo.idxFrameStart[recRep.currentReplayFrame];
        byte[] msgPack = new byte[pckgSize];

        var numberBytes = streamFromFile.Read(msgPack, 0, pckgSize);

        int i = 4; // first 4 bytes are length of package
        while (i < numberBytes)
        {
            int lengthMsg = BitConverter.ToInt32(msgPack, i);
            i += 4;
            byte[] msg = new byte[lengthMsg];
            Buffer.BlockCopy(msgPack, i, msg, 0, lengthMsg);

            ReferenceCountedSceneGraphMessage rcsgm = CreateRCSGM(msg);
            ReplayedObjectProperties props = replayedObjects[rcsgm.objectid];
            INetworkComponent component = props.components[rcsgm.componentid];

            //if (!recRep.play)
            //{
            //    props.hider.Show();
            //}

            // send and replay remotely
            recRep.scene.Send(rcsgm);
            // replay locally
            component.ProcessMessage(rcsgm);

            i += lengthMsg;
        }
    }

    private void HideAll()
    {
        foreach (ReplayedObjectProperties props in replayedObjects.Values)
        {
            props.hider.NetworkedHide();
        }
    }

    private void ReplayMessagesPerFrame()
    {
        Debug.Log("Replay messages...");

        foreach (var message in replayedMessages[recRep.currentReplayFrame])
        {
            ReplayedObjectProperties props = replayedObjects[message.objectid];
            INetworkComponent component = props.components[message.componentid];

            // send and replay remotely
            recRep.scene.Send(message);

            // replay locally
            component.ProcessMessage(message);

        }
        //msgIndex = msgIndex + msgsPerFrame;
        recRep.currentReplayFrame++;
        //Debug.Log(currentReplayFrame + " " + msgIndex);
    }

    public void Cleanup(bool unspawn)
    {
        
        Debug.Log("Cleanup " + Time.unscaledTime);
        foreach (var i in oldNewObjectids)
        {
            Debug.Log("Cleanup ids old: " + i.Key + " new: " + i.Value);
        }

        loadingStarted = false;
        loaded = false;
        showAvatarsFromStart = false;
        recRep.play = true;
        recRep.currentReplayFrame = 0;
        recRep.sliderFrame = 0;
        // only unspawn while in room, NOT when leaving the room as it will be unspawned by the OnLeftRoom event anyways.
        if (unspawn && replayedObjects.Count > 0)
        {
            foreach (var ids in replayedObjects.Keys)
            {
                spawner.UnspawnPersistent(ids);
            }
        }
        replayedObjects.Clear();
        oldNewObjectids.Clear();
        recInfo = null;
        
        if (streamFromFile != null)
            streamFromFile.Close();
    }
}

[System.Serializable]
public class RecordingInfo
{
    public int[] listLengths;
    public int frames;
    public int avatarsAtStart;
    public int avatarNr; 
    public List<NetworkId> objectids;
    public List<string> textures;
    public List<float> frameTimes;
    public List<int> pckgSizePerFrame;
    public List<int> idxFrameStart;

    public RecordingInfo(int frames, int avatarsAtStart, int avatarNr, List<NetworkId> objectids, List<string> textures, List<float> frameTimes, List<int> pckgSizePerFrame, List<int> idxFrameStart)
    {
        listLengths = new int[3] { frameTimes.Count, pckgSizePerFrame.Count, idxFrameStart.Count };
        this.frames = frames;
        this.avatarsAtStart = avatarsAtStart;
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
    [HideInInspector] public AvatarManager aManager;
    [HideInInspector] public NetworkSpawner spawner;

    public string replayFile;
    [HideInInspector] public string recordFile = null;
    [HideInInspector] public string path;
    [HideInInspector] public bool recording, replaying;
    [HideInInspector] public bool play = true;
    [HideInInspector] public int sliderFrame = 0;
    [HideInInspector] public float stopTime = 0.0f;
    [HideInInspector] public float replayingStartTime = 0.0f;
    [HideInInspector] public bool loop = true;
    [HideInInspector] public int currentReplayFrame = 0;
    [HideInInspector] public bool reverse = false;

    [HideInInspector] public bool leftRoom = false;
    private RoomClient roomClient;
    private Recorder recorder;
    [HideInInspector] public Replayer replayer;
    [HideInInspector] public bool recordingAvailable = false;
    [HideInInspector] public bool cleanedUp = true;  

    public bool IsOwner()
    {
        return roomClient.Me["creator"] == "1";
    }

    // Use Awake() because 
    void Awake()
    {
        //Application.targetFrameRate = 60;
        //Time.captureFramerate = 400;
        path = Application.persistentDataPath + "/Recordings";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        aManager = scene.GetComponentInChildren<AvatarManager>();
        spawner = NetworkSpawner.FindNetworkSpawner(scene);

        // create Recorder and Replayer
        recorder = new Recorder(this);
        replayer = new Replayer(this);
        roomClient = GetComponent<RoomClient>();
        roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
        roomClient.Me["creator"] = "1"; // so recording is also enabled when not being in a room at startup

    }

    private void OnPeerRemoved(IPeer peer)
    {
        if (peer == roomClient.Me)
        {
            cleanedUp = true; 
            replayer.Cleanup(true);
        
            if (replaying)
            {
                replaying = false;
                replayingStartTime = 0.0f;
                stopTime = 0.0f;
                Debug.Log("Left room, replaying stopped!");
            }
            if (recording)
            {
                recording = false;
                Debug.Log("Left room, recording stopped!");
            }
        }
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
                    cleanedUp = true;
                    replayer.Cleanup(true);
                    replayingStartTime = 0.0f;
                    stopTime = 0.0f;
                }

                SetReplayFile();
                recordingAvailable = false; // avoid unnecessary savings of same info (is checked in methods too)
            }
        }
       else
        {
            recordingAvailable = true;
            //cleanedUp = false;
        }
        // load file
        // create avatars (avatar manager to get exact avatars) on other clients
        // send messages over network
        if (replaying)
        {
            replayer.Replay(replayFile);
            cleanedUp = false;
        }
        else
        {
            if (!cleanedUp)
            {
                cleanedUp = true;
                replayer.Cleanup(true);
                replayingStartTime = 0.0f;
                stopTime = 0.0f;
            }
        }
    }
    public void SetReplayFile()
    {
        // sets the previously recorded file as replay file
        replayFile = Path.GetFileNameWithoutExtension(recordFile); 
        //recordFile.Substring(recordFile.IndexOf("rec"));
        //replayFile = replayFile.Remove(replayFile.LastIndexOf(".")); // remove the ".txt", or ".dat"
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
# if UNITY_EDITOR
[CustomEditor(typeof(RecorderReplayer))]
public class RecorderReplayerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var t = (RecorderReplayer)target;
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            EditorGUI.BeginDisabledGroup(!t.IsOwner());
            if (GUILayout.Button(t.recording == true ? "Stop Recording" : "Record"))
            {
                t.recording = !t.recording;
            }
            t.replaying = EditorGUILayout.Toggle("Replaying", t.replaying);
            if (t.replaying)
            {
                //t.cleanedUp = false;
                if (GUILayout.Button(t.play == true ? "Stop" : "Play"))
                {
                    if (!t.play)
                    {
                        //t.replayingStartTime = Time.unscaledTime;
                        t.replayingStartTime = t.replayer.recInfo.frameTimes[t.currentReplayFrame];
                    }
                    t.play = !t.play;
                }
                if (!t.play)
                {
                    t.sliderFrame = EditorGUILayout.IntSlider(t.sliderFrame, 0, t.replayer.recInfo.frames);
                }
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
# endif

