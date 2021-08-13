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
using RecorderReplayerTypes;

public class Recorder
{
    // Recording
    private RecorderReplayer recRep;

    private BinaryWriter binaryWriter;
    private string recordFileIDs; // save the objectIDs of the recorded avatars
    private Dictionary<NetworkId, string> avatars; // just avatars with texture
    private Dictionary<NetworkId, string> recordedObjectIds; // everything with prefab
    //private int lineNr = 0; // number of lines in recordFile
    private List<int> pckgSizePerFrame;
    private List<int> idxFrameStart; // start of a specific frame in the recorded data
    private int frameNr = 0;
    private int previousFrame = 0;
    private int avatarsAtStart = 0;
    private int objectsAtStart = 0;
    private bool initFile = false;
    private MessagePack messages = null;
   
    private List<float> frameTimes = new List<float>(); 
    private float recordingStartTime = 0.0f;

    public Recorder(RecorderReplayer recRep)
    {
        this.recRep = recRep;
        //recordedAvatarIds = new Dictionary<NetworkId, string>();
        avatars = new Dictionary<NetworkId, string>();
        recordedObjectIds = new Dictionary<NetworkId, string>();
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
            //recordedAvatarIds = new Dictionary<NetworkId, string>();
            recordedObjectIds = new Dictionary<NetworkId, string>();

            idxFrameStart.Add(0); // first frame in byte data has idx 0

            avatarsAtStart = recRep.aManager.Avatars.Count();
            // also consider avatars that are currently visible from a potential previous recording!
            //foreach (var props in recRep.replayer.replayedAvatars.Values)
            //{
            //    if (props.hider.GetCurrentLayer() == 0) // default visibility layer 
            //    {
            //        avatarsAtStart += 1;
            //    }    
            //}
            //objectsAtStart = recRep.spawner.GetSpawned().Count;
            foreach (var props in recRep.replayer.replayedObjects.Values)
            {
                if (props.hider.GetCurrentLayer() == 0) // default
                {   
                    objectsAtStart += 1;
                }
            }
            objectsAtStart += avatarsAtStart; // i think we don't need this

            //recStream = File.Open(recordFile, FileMode.OpenOrCreate);
            binaryWriter = new BinaryWriter(File.Open(recRep.recordFile, FileMode.OpenOrCreate)); // dispose when recording is finished
            
            // start recoding time
            recordingStartTime = Time.unscaledTime;

            initFile = true;

        }

        //Debug.Log("Framenr: " + frameNr);
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
        var msg = new byte[message.length + 10]; // just take header and message
        Buffer.BlockCopy(message.bytes, 0, msg, 0, message.length + 10);
        SingleMessage recMsg = new SingleMessage(msg);
        byte[] recBytes = recMsg.GetBytes();
          
        messages.AddMessage(recBytes);
        //SingleMessage3 test = new SingleMessage3(recMsg.GetBytes());
        //Debug.Log(message.objectid.ToString() + " " + frameNr);

        if (!recordedObjectIds.ContainsKey(message.objectid)) // dictionary ContainsKey is O(1) and List Contains is O(n)
        {
        
            if (obj is Avatar) // check it here too in case we later record other things than avatars as well
            {
                var name = (obj as Avatar).PrefabUuid;
                string uid = (obj as Avatar).gameObject.GetComponent<TexturedAvatar>().GetTextureUuid(); // get texture of avatar so we can later replay a look-alike avatar
                recordedObjectIds.Add(message.objectid, name); // change this to prefab later... dunno how to get that yet though...
                if (!avatars.ContainsKey(message.objectid))
                {
                    avatars.Add(message.objectid, uid);
                }
            }
            else
            {
                string name = (obj as MonoBehaviour).name;
                // objects that aren't avatars don't save the texture uid, but the name of their prefab 
                // maybe do something similar for avatars later, especially when different avatar types are used (how to deal with texture uid then?)
                recordedObjectIds.Add(message.objectid, name); 
            }
        }

    }

    public void SaveRecordingInfo()
    {
        if (recordedObjectIds != null && recordFileIDs != null) // save objectids and texture uids once recording is done
        {
            Debug.Log("Save recording info");
            //recStream.Dispose();
            binaryWriter.Dispose();

            Debug.Log("FrameNr, pckgsize, idxFrameStart" + frameNr + " " + pckgSizePerFrame.Count + " " + idxFrameStart.Count);
           
            File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(frameNr-1, avatarsAtStart, avatars.Count,
                objectsAtStart,
                new List<NetworkId>(avatars.Keys), new List<string>(avatars.Values),
                new List<NetworkId>(recordedObjectIds.Keys), new List<string>(recordedObjectIds.Values),
                frameTimes, pckgSizePerFrame, idxFrameStart)));

            avatars.Clear();
            recordedObjectIds.Clear();
            avatarsAtStart = 0;
            objectsAtStart = 0;
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
    private Dictionary<NetworkId, string> prefabs; // joined lists from recInfo
    private Dictionary<NetworkId, string> textures; // joined lists from recInfo
    // later for the recording of other objects consider not only saving the networkid but additional info such as class
    // maybe save info in Dictionary list and save objectid (key) and values (list: class, (if avatar what avatar type + texture info)
    private Dictionary<NetworkId, string> replayedObjectids; // avatar IDs and texture
    //public Dictionary<NetworkId, ReplayedObjectProperties> replayedAvatars; // new objectids for avatars only! 
    public Dictionary<NetworkId, ReplayedObjectProperties> replayedObjects; // new objectids for all other objects! 
    private Dictionary<NetworkId, NetworkId> oldNewIds;
    private bool loadingStarted = false; // set to true once loading recorded data starts
    private bool loaded = false;
    //private bool loaded = false; // set to true once all recorded data is loaded
    //private int msgIndex = 0; // for replaying from file where every msg is in separate line to get correct index for messages in next frame
    private FileStream streamFromFile;
    private bool avatarsCreated = false;
    private bool objectsCreated = false;
    private bool opened = false;
    private bool showAvatarsFromStart = false;
 
    public Replayer(RecorderReplayer recRep)
    {
        this.recRep = recRep;

        replayedObjectids = new Dictionary<NetworkId, string>();
        //replayedAvatars = new Dictionary<NetworkId, ReplayedObjectProperties>();
        replayedObjects = new Dictionary<NetworkId, ReplayedObjectProperties>();
        oldNewIds = new Dictionary<NetworkId, NetworkId>();
        spawner = recRep.spawner;
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
                // if show/hide messages are sent every frame then this might be obsolete...
                //if (!showAvatarsFromStart)
                //{
                //    Debug.Log("Show avatars from start");
                //    for (var i = 0; i < recInfo.avatarsAtStart; i++)
                //    {
                //        replayedAvatars.Values.ElementAt(i).hider.SetNetworkedObjectLayer(0); // show (default layer)
                //    }
                //    showAvatarsFromStart = true;
                //}

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
        recRep.currentReplayFrame++;
        //!!!!!!!!!!! sometimes the frame number is one too high so maybe just in case for now skip the last frame?
        // no clue why that happens actually... I do reset all the variables after each recording.
        if (recRep.currentReplayFrame == recInfo.frames-1)
        {
            recRep.currentReplayFrame = 0;
            HideAll();
            showAvatarsFromStart = false;
            streamFromFile.Position = 0;
            recRep.replayingStartTime = 0.0f;
            recRep.stopTime = 0.0f;
        }
        recRep.sliderFrame = recRep.currentReplayFrame;
    }

    //private bool CreateRecordedAvatars()
    //{
    //    for (var i = 0; i < recInfo.objectids.Count; i++)
    //    {
    //        var objectid = recInfo.objectids[i];
    //        var uuid = recInfo.textures[i];
    //        ReplayedObjectProperties props = new ReplayedObjectProperties();
            
    //        // if different avatar types are used for different clients change this!
    //        GameObject prefab = spawner.catalogue.prefabs[3]; // Spawnable Floating BodyA Avatar
    //        GameObject go = spawner.SpawnPersistentReplay(prefab, uuid); // this game object has network context etc. (not the prefab)
    //        Avatar avatar = go.GetComponent<Avatar>(); // spawns invisible avatar
    //        props.hider = go.GetComponent<ObjectHider>();
    //        Debug.Log("CreateRecordedAvatars() " + avatar.Id);

    //        oldNewIds.Add(objectid, avatar.Id);
    //        Debug.Log(objectid.ToString() + " new: " + avatar.Id.ToString());

    //        props.gameObject = go;
    //        props.id = avatar.Id;
    //        INetworkComponent[] components = go.GetComponents<INetworkComponent>();
    //        foreach (var comp in components)
    //        {
    //            props.components.Add(NetworkScene.GetComponentId(comp), comp);
    //        }
    //        replayedAvatars.Add(avatar.Id, props);
    //    }
    //    return true;
    //}

    private bool CreateRecordedObjects()
    {
        foreach (var item in prefabs)
        {
            var objectid = item.Key;
            var prefabName = item.Value;
            var uid = "n";
            GameObject prefab = spawner.catalogue.GetPrefab(prefabName);
            if (prefab == null)
            {
                continue;
            }
            if (prefab.GetComponent<Avatar>() != null) // object is an avatar, so it has a saved texture
            {
                uid = textures[objectid];
            }
            GameObject go = spawner.SpawnPersistentReplay(prefab, uid);

            ReplayedObjectProperties props = new ReplayedObjectProperties();
            props.hider = go.GetComponent<ObjectHider>();
            Debug.Log("CreateRecordedObjects():  " + go.name);
            NetworkId newId = go.GetComponent<INetworkObject>().Id;
            oldNewIds.Add(objectid, newId);
            Debug.Log(objectid.ToString() + " new: " + newId.ToString());
            props.gameObject = go;
            props.id = newId;
            INetworkComponent[] components = go.GetComponents<INetworkComponent>();
            foreach (var comp in components)
            {
                props.components.Add(NetworkScene.GetComponentId(comp), comp);
            }
            replayedObjects.Add(newId, props);

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
            //avatarsCreated = CreateRecordedAvatars();
            objectsCreated = CreateRecordedObjects();
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
        loaded = objectsCreated && opened;
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
            prefabs = recInfo.objectids.Zip(recInfo.prefabs, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            textures = recInfo.avatars.Zip(recInfo.textures, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
        }

        return recInfo;
    }

    //private async Task<bool> LoadMessages(string filepath)
    //{
    //    using (FileStream stream = File.Open(filepath, FileMode.Open))
    //    {
    //        int i = 0;
    //        replayedFrames = new int[recInfo.frames];
    //        byte[] msgs;
    //        replayedMessages = new ReferenceCountedSceneGraphMessage[recInfo.frames][];
    //        while (i < recInfo.frames)
    //        {
    //            msgs = new byte[recInfo.pckgSizePerFrame[i]];
    //            await stream.ReadAsync(msgs, 0, msgs.Length);

    //            MessagePack mp = new MessagePack(msgs);
    //            replayedMessages[i] = CreateRCSGMs(mp);
    //            i++;
    //        }
    //    }
    //    return true;
    //}

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
    // wouldn't it be nice to be able to skip a recorded message if something went wrong, so we just don't replay it
    private ReferenceCountedSceneGraphMessage CreateRCSGM(byte[] msg)
    {
        ReferenceCountedMessage rcm = new ReferenceCountedMessage(msg);
        ReferenceCountedSceneGraphMessage rcsgm = new ReferenceCountedSceneGraphMessage(rcm);
        NetworkId id = new NetworkId(msg, 0);
        try
        {
            rcsgm.objectid = oldNewIds[id];
        }
        catch (Exception e)
        {
            if (e.Source != null)
                Debug.Log("Network id: " + id + ", Exception source: " + e.Source);
            foreach(var i in oldNewIds)
            {
                Debug.Log("old: " + i.Key + " new: " + i.Value);
            }
            throw;
        }    
        return rcsgm;
    }

    
    private void ReplayFromFile()
    {
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
            Debug.Log(rcsgm.objectid.ToString());
            ReplayedObjectProperties props = replayedObjects[rcsgm.objectid]; // avatars and objects
            INetworkComponent component = props.components[rcsgm.componentid];

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

    //private void ReplayMessagesPerFrame()
    //{
    //    Debug.Log("Replay messages...");

    //    foreach (var message in replayedMessages[recRep.currentReplayFrame])
    //    {
    //        ReplayedObjectProperties props = replayedAvatars[message.objectid];
    //        INetworkComponent component = props.components[message.componentid];

    //        // send and replay remotely
    //        recRep.scene.Send(message);

    //        // replay locally
    //        component.ProcessMessage(message);

    //    }
    //    //msgIndex = msgIndex + msgsPerFrame;
    //    recRep.currentReplayFrame++;
    //    //Debug.Log(currentReplayFrame + " " + msgIndex);
    //}

    public void Cleanup(bool unspawn)
    {
        
        Debug.Log("Cleanup " + Time.unscaledTime);
        foreach (var i in oldNewIds)
        {
            Debug.Log("Cleanup ids old: " + i.Key + " new: " + i.Value);
        }

        loadingStarted = loaded = avatarsCreated = objectsCreated = showAvatarsFromStart = false;
        recRep.play = true;
        recRep.currentReplayFrame = 0;
        recRep.sliderFrame = 0;
        // only unspawn while in room, NOT when leaving the room as it will be unspawned by the OnLeftRoom event anyways.
        //if (unspawn && replayedAvatars.Count > 0)
        //{
        //    foreach (var ids in replayedAvatars.Keys)
        //    {
        //        spawner.UnspawnPersistent(ids);
        //    }
        //}
        if (unspawn && replayedObjects.Count > 0)
        {
            foreach (var ids in replayedObjects.Keys)
            {
                spawner.UnspawnPersistent(ids);
            }
        }

        //replayedAvatars.Clear();
        replayedObjects.Clear();
        oldNewIds.Clear();
        recInfo = null;
        if (prefabs != null)
        {
            prefabs.Clear();
            textures.Clear();
        }
        
        if (streamFromFile != null)
            streamFromFile.Close();
    }
}

public class RecorderReplayer : MonoBehaviour, IMessageRecorder, INetworkComponent
{
    public NetworkScene scene;
    [HideInInspector] public AvatarManager aManager;
    [HideInInspector] public NetworkSpawner spawner;
    private bool Recording = false; // this variable indicates if a recording is taking place, this doesn't need to be the local recording!
    private NetworkContext context;

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

    public struct Message
    {
        public bool recording;

        public Message(bool recording) { this.recording = recording; }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Message msg = message.FromJson<Message>();
        Recording = msg.recording;
        Debug.Log("Yeahh is there a recording happening? " + Recording);
    }

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
    }

    public struct RoomMessage
    {
        public string peerUuid;
        public bool isRecording;
    }

    void Start ()
    {
        context = scene.RegisterComponent(this);
        roomClient = GetComponent<RoomClient>();
        //roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
        roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);
        roomClient.OnPeerUpdated.AddListener(OnPeerUpdated);
        roomClient.Me["creator"] = "1"; // so recording is also enabled when not being in a room at startup
        roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
    }
    // if previous authority gets discoonected make sure that recording is stopped when authority is given to the next peer
    // it shouldnt even be necessary to call it here
    public void OnPeerUpdated(IPeer peer)
    {
        // just so i dont forget what i was thinking here...
        if (peer["creator"] == "1") // if creator just got reassigned in the room client this could be true
        {
            // need to make sure that if this is called because of another update and this peer had the authority anyways, that we do not 
            // end a recording that should not be ended... 
            if (!recording) // this is true if the authority just got reassigned because then the new peer never did a recording before
            {
                // so we can safely globally set the Recording to false for everyone in case the previous peer who got disconnected cannot do this anymore
                Debug.Log("RecorderReplayer: OnPeerUdated");
                Recording = false; // should be false already because of StopRecording() in RoomClient!
                // update the properties in the room for the new recorder authority...
                roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
            }
        }
    }

    public void OnRoomUpdated(IRoom room)
    {
        if (roomClient.Me["creator"] == "1")
        {
            Debug.Log("I record: " + Recording + " " + roomClient.Me.UUID);
            roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
        }
        else
        //if (roomClient.Me["creator"] != "1") 
        {
            if (room["Recorder"] != null)
            {
                RoomMessage msg = JsonUtility.FromJson<RoomMessage>(room["Recorder"]);
                Recording = msg.isRecording;
                Debug.Log("Someone records: " + Recording);
            }
            else
            {
                Debug.Log("No recorder was added to the dictionary... this shouldn't be.");
            }
        }
    }

    //public void OnPeerRemoved(IPeer peer)
    //{
    //    if (peer["creator"] == "1") // this might be called when the removed peer who was the creator isn't even the creator anymore...
    //    {
    //        Debug.Log("RecRep: OnPeerRemoved");
    //        cleanedUp = true; 
    //        replayer.Cleanup(true);
    //        Recording = false;
    //        roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });

    //        if (replaying)
    //        {
    //            replaying = false;
    //            replayingStartTime = 0.0f;
    //            stopTime = 0.0f;
    //            Debug.Log("Left room, replaying stopped!");
    //        }
    //        if (recording)
    //        {
    //            recording = false;
    //            Debug.Log("Left room, recording stopped!");
    //        }
    //    }
    //}

    private void OnDestroy()
    {
        Debug.Log("OnDestroy");
        //replayer.Cleanup(true); objects should be removed by each client when OnPeerRemoved is called
        if (recording)
        {
            Recording = recording = false;
            // this probably isn't sent anymore as the NetworkScene got already destroyed
            roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });

            recorder.SaveRecordingInfo();
        }
    }

    // Update is called once per frame
    void Update()
    {
       if (roomClient.Me["creator"] == "1") // don't bother if we are not room creators
        {
            if (!recording)
            {
                if (Recording)
                {
                    Recording = false;
                    roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
                    Debug.Log("Tell everyone we STOPPED recording");
                }

                if (recordingAvailable)
                {
                    recorder.SaveRecordingInfo();

                    // stop replaying once recording stops as it does not make sense to see the old replay since there is already a new one
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
                if (!Recording)
                {
                    Recording = true;
                    roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
                    Debug.Log("Tell everyone we ARE recording");
                }
                recordingAvailable = true;
            }

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
    }
    public void SetReplayFile()
    {
        // sets the previously recorded file as replay file
        replayFile = Path.GetFileNameWithoutExtension(recordFile); 
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

    public void StopRecording()
    {
        Recording = false;
    }

    public bool IsRecording()
    {
        return Recording;
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

