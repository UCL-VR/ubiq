using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Networking;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Spawning;
using Ubiq.Logging.Utf8Json;

public class RecorderReplayer : MonoBehaviour, INetworkObject, INetworkComponent, IMessageRecorder
{
    public bool recording = false;
    public bool replaying = false;

    public NetworkScene scene;

    private AvatarManager avatarManager;
    private NetworkSpawner spawner;
    private string path;

    // Recording
    private FileStream recStream;
    private string recordFile;
    private string recordFileIDs; // save the objectIDs of the recorded avatars
    private string recordedData;  // format of recorded data: (time), frame, object ID, component ID, sgbmessage
    private Dictionary<NetworkId, string> recordedObjectids;
    private int lineNr = 0; // number of lines in recordFile
    private int frameNr = 0;
    private int previousFrame = 0;
    private bool initFile = false;

    // Replaying
    public string replayFile;
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

    private class ReplayedObjectProperties
    {
        public GameObject gameObject;
        public NetworkId id;
        public Dictionary<int, INetworkComponent> components = new Dictionary<int, INetworkComponent>();

    }
    // recorded messages per frame
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
 
    public class Message
    {
        public ReferenceCountedSceneGraphMessage message;
        public byte[] bytes;
        private int length;
        private byte[] blength;

        public Message(ReferenceCountedSceneGraphMessage message)
        {
            this.message = message;
            length = message.bytes.Length;

            blength = System.BitConverter.GetBytes(length);
            if (System.BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(blength);
            }

            bytes = ToBytes();
        }

        public byte[] ToBytes() // message to bytes inlcuding length of message as bytes at the beginning
        {
            byte[] bytes = new byte[4 + length];
            
            blength.CopyTo(bytes, 0);
            message.bytes.CopyTo(bytes, 4);
            return bytes;
        }
    }

    public class MessagesPerFrame
    {
        public float frameTime;

        public int frameNr;
        public int lengthMessages;
        public List<Message> messages = new List<Message>();

        public MessagesPerFrame(int frameNr)
        {
            this.frameNr = frameNr;
            frameTime = Time.unscaledTime;
        }
        public void AddToList(Message m)
        {
            messages.Add(m);
            lengthMessages += m.bytes.Length;
        }
        public byte[] ToBytes()
        {
            byte[] msgsPerFrame = new byte[lengthMessages];
            var i = 0;
            //var i = 4;
            //System.BitConverter.GetBytes(frameNr).CopyTo(msgsPerFrame, 0);
            foreach (var msg in messages)
            {
                msg.bytes.CopyTo(msgsPerFrame, i);
                i += msg.bytes.Length;
            }
            return msgsPerFrame;
        }
    }

    public NetworkId Id { get; } = new NetworkId();

    public bool IsRecording()
    {
        return recording;
    }
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        throw new System.NotImplementedException();
    }

    public void RecordMessage(INetworkObject obj, ReferenceCountedSceneGraphMessage message)
    {
        if (!initFile)
        {
            recordFile = path + "/rec" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            recordFileIDs = recordFile + "IDs.txt";
            recordFile = recordFile + ".txt";

            recordedObjectids = new Dictionary<NetworkId, string>();

            //recStream = File.Open(recordFile, FileMode.OpenOrCreate);

            initFile = true;
        }

        string uid;
        if (obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            // save all messages that are happening in one frame in same line
            //if (frameNr == 0 || previousFrame != frameNr)
            //{
            //    if (messages != null)
            //    {
            //        //var bytesPerFrame = messages.ToBytes();
            //        //recStream.Write(bytesPerFrame, 0, bytesPerFrame.Length);
            //        //File.AppendAllText(recordFile, JsonUtility.ToJson(message) + "\n");
            //        //File.AppendAllText(recordFile, recordedData + "\n", System.Text.Encoding.UTF8);
            //        //lineNr += 1;
            //        //recordedMessages = null;
            //    }
            //    //recordedData = Time.unscaledTime + "," + frameNr;
            //    //messages = new MessagesPerFrame(frameNr);

            //    //previousFrame++;
            //}

            //Avatar avatar = obj as Avatar;
            uid = (obj as Avatar).Properties["texture-uid"]; // get texture of avatar so we can later replay a look-alike avatar

            //recordedData = Time.unscaledTime + "," + frameNr + "," + message.ToString().Replace("\n", "\\n").Replace("\r", "\\r") + "\n";
            //recordedData = recordedData + "," + message.ToString().Replace("\n", "\\n").Replace("\r", "\\r");
            //System.Array.Copy(message.bytes, message.start, bmsg, 0, message.length);
            //messages.messages.Add(new RecordedMessage(message.objectid.ToString(), message.componentid, message.ToString()));
            string recMsg = JsonUtility.ToJson(new SingleMessage(frameNr, message.objectid, message.componentid, message.bytes));
            File.AppendAllText(recordFile, recMsg + "\n");
            Debug.Log(message.objectid.ToString() + " " + frameNr);
            lineNr += 1;
            //messages.AddToList(new Message(message));

            if (!recordedObjectids.ContainsKey(message.objectid))
            {
                recordedObjectids.Add(message.objectid, uid);
            }
        }
        //File.AppendAllText(recordFile, recordedData, System.Text.Encoding.UTF8);
    }

    public void Replay()
    {
        // Load data from file
        // keep change of IDs in mind
        // create new avatars and change IDs of messages to be sent to new avatars
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
            scene.Send(message);
            
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

        string filepath = path + "/" + replayFile + "IDs.txt";
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

        filepath  = path + "/" + replayFile + ".txt";
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

            //int i = 0;
            //while ((recInfo = await reader.ReadLineAsync()) != null)
            //{
            //    if (i == 0)
            //    {
            //        numberOfRecLines = int.Parse(recInfo.Split(',')[0]);
            //    }
            //    else if (i == 1)
            //    {
            //        numberOfRecAvatars = int.Parse(recInfo.Split(',')[0]);
            //    }
            //    else
            //    {
            //        var s = recInfo.Split(',');
            //        replayedObjectids.Add(new NetworkId(s[0]), s[1]);
            //    }
            //    i++;
            //}
        }
        //string[] recInfo = File.ReadAllLines(path + "/" + replayFile + "IDs.txt");
        //numberOfRecLines = int.Parse(recInfo[0].Split(',')[0]);
        //numberOfRecAvatars = int.Parse(recInfo[1].Split(',')[0]);

        //for (int i = 2; i < recInfo.Length; i++) // ignore first two entries
        //{
        //    var s = recInfo[i].Split(',');

        //    replayedObjectids.Add(new NetworkId(s[0]), s[1]);
        //}
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
                //var bLengthAllMsgs = new byte[4];
                //await stream.ReadAsync(bLengthAllMsgs, 0, 4);// read length
                //System.Array.Reverse(bLengthAllMsgs);
                //var lengthAllMsgs = System.BitConverter.ToInt32(bLengthAllMsgs, 0);
                //var bAllMsgs = new byte[System.BitConverter.ToInt32(bLengthAllMsgs, 0)];
                //await stream.ReadAsync(bAllMsgs, 0, bAllMsgs.Length);

                SingleMessage singleMsg = JsonUtility.FromJson<SingleMessage>(msg);
                //Debug.Log(singleMsg.frame + " " + replayedFrames.Length);
                var idx = singleMsg.frame;
                var pre = replayedFrames[idx - 1];
                replayedFrames[idx-1] = pre + 1; // because frameNr starts at 1
                ReferenceCountedMessage buffer = new ReferenceCountedMessage(singleMsg.message);
                ReferenceCountedSceneGraphMessage rcsgm = new ReferenceCountedSceneGraphMessage(buffer);
                rcsgm.objectid = oldNewObjectids[singleMsg.objectid];
                //rcsgm.componentid = singleMsg.componentid; // dont need that because component should always stay the same
                replayedMessages[i] = rcsgm;

                i++;

                //SingleMessage test = new SingleMessage(singleMsg.frame, rcsgm.objectid, rcsgm.componentid, rcsgm.data.ToArray());
                //File.AppendAllText("C:/Users/klara/PhD/Projects/ubiq/Unity/Assets/Local/Recordings/fromjson.txt", JsonUtility.ToJson(test) + "\n");
                //int j = 0;
                //while (j < bAllMsgs.Length)
                //{
                //    byte[] length = new byte[4];
                //    System.Array.Copy(bAllMsgs, j, length, 0, 4); // length of next message;
                //    //System.Array.Reverse(length);
                //    var l = System.BitConverter.ToInt32(length, 0);
                //    j = j + 4;
                //    byte[] message = new byte[l-10];
                //    byte[] objectid = new byte[8];
                //    byte[] componentid = new byte[2];

                //    for(int k = 0; k < 8; k++ )
                //    {
                //        objectid[k] = bAllMsgs[j + k];
                //    }
                //    j = j + 8;
                //    for (int k = 0; k < 2; k++)
                //    {
                //        componentid[k] = bAllMsgs[j + k];
                //    }
                //    j = j + 2;
                //    for (int k = 0; k < l-10; k++)
                //    {
                //        message[k] = bAllMsgs[j + k];
                //    }

                //    j = j + l-10;

                //    //Message recMsg = msgs.messages[j];
                //    ReferenceCountedMessage rcm = new ReferenceCountedMessage(message);
                //    var rcsgm = new ReferenceCountedSceneGraphMessage(rcm);
                //    NetworkId oid = new NetworkId(objectid, 0); 
                //    rcsgm.objectid = oldNewObjectids[oid]; // replace old with new objectid!!!
                //    System.Array.Reverse(componentid);
                //    ushort cid = System.BitConverter.ToUInt16(componentid, 0);
                //    rcsgm.componentid = cid;
                //    replayedMessages[i].Add(rcsgm);
                //}
            }
        }
        return true;
    }

    // so we know how many of the messages belonge to one frame,
    // this is called after all connections have received their messages after one Update()
    public void NextFrame()
    {
        previousFrame = frameNr;
        frameNr++;
    }

    // Get all the recordable components from the scene (avatars, objects too later)
    // This should be networked objects (for now). 

    // Start is called before the first frame update
    void Start()
    {
 
        path = Application.dataPath + "/Local/Recordings";
        //recordFile = path + "/" + recordFile;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        recordedObjectids = new Dictionary<NetworkId, string>();
        replayedObjectids = new Dictionary<NetworkId, string>();
        replayedObjects = new Dictionary<NetworkId, ReplayedObjectProperties>();
        oldNewObjectids = new Dictionary<NetworkId, NetworkId>();

        spawner = NetworkSpawner.FindNetworkSpawner(scene);

        avatarManager = scene.GetComponent<AvatarManager>();
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
            if (recordedObjectids != null && recordFileIDs != null) // save objectids and texture uids once recording is done
            {
                //recStream.Dispose();
                
                File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(lineNr, frameNr, recordedObjectids.Count,
                    new List<NetworkId>(recordedObjectids.Keys), new List<string>(recordedObjectids.Values))));

                //using (StreamWriter file = new StreamWriter(recordFileIDs))
                //{
                //    file.WriteLine("{0},{1}", lineNr, "lineNr"); // number of lines in recording
                //    file.WriteLine("{0},{1}", recordedObjectids.Count, "avatarNr"); // number of avatars in recording
                //    foreach (var entry in recordedObjectids)
                //    {
                //        file.WriteLine("{0},{1}", entry.Key, entry.Value);
                //    }
                //}

                // sets the previously recorded file as replay file
                replayFile = recordFile.Substring(recordFile.IndexOf("rec"));
                Debug.Log("Set replay file to " + replayFile);

                recordedObjectids = null;
                recordFileIDs = null;
            }
            if (initFile)
            {
                initFile = false;
            }
            if (frameNr > 0) // reset frameNr for next recording
            {
                frameNr = 0;
            }
            if (lineNr > 0)
            {
                lineNr = 0;
            }

        }
        // load file
        // create avatars (avatar manager to get exact avatars) on other clients
        // send messages over network
        if (replaying)
        {
            if (!loadingStarted)
            {
                LoadRecording(replayFile);
            }

            if (loaded)
            {
                ReplayMessagesPerFrame();
                //if(currentReplayFrame == recInfo.recLinesNr)
                if (currentReplayFrame == recInfo.frames-1)
                {
                    currentReplayFrame = 0;
                    msgIndex = 0;
                }
            }


        }

    }
}

