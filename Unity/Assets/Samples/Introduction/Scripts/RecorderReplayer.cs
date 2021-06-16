using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using System.IO;
using System.Threading.Tasks;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Spawning;
using Ubiq.Samples;

public class RecorderReplayer : MonoBehaviour, INetworkObject, INetworkComponent, IMessageRecorder
{
    public bool recording = false;
    public bool replaying = false;

    public NetworkScene scene;

    private AvatarManager avatarManager;
    private NetworkSpawner spawner;
    private string path;

    // Recording
    private string recordFile;
    private string recordFileIDs; // save the objectIDs of the recorded avatars
    private string recordedData;  // format of recorded data: (time), frame, object ID, component ID, sgbmessage
    private Dictionary<NetworkId, string> recordedObjectids;
    private int lineNr = 0; // number of lines in recordFile
    private int frameNr = 0;
    private int previousFrame = 0;
    private bool initFile = false;
    private Messages messages = null;

    // Replaying
    public string replayFile;
    private List<ReferenceCountedSceneGraphMessage>[] replayedMessages;
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

    private class ReplayedObjectProperties
    {
        public GameObject gameObject;
        public NetworkId id;
        public Dictionary<int, INetworkComponent> components = new Dictionary<int, INetworkComponent>();

    }
    // recorded messages per frame
    [System.Serializable]
    public class RecordedMessage
    {
        public NetworkId objectid;
        public ushort componentid;
        public string message;

        public RecordedMessage(NetworkId objectid, ushort componentid, string message)
        {
            this.objectid = objectid;
            this.componentid = componentid;
            this.message = message;
        }
    }
    [System.Serializable]
    public class RecordingInfo
    {
        public int recLinesNr;
        public int avatarNr;
        public List<NetworkId> objectids;
        public List<string> textures;

        public RecordingInfo (int recLinesNr, int avatarNr, List<NetworkId> objectids, List<string> textures)
        {
            this.recLinesNr = recLinesNr;
            this.avatarNr = avatarNr;
            this.objectids = objectids;
            this.textures = textures;
        }
    }

    [System.Serializable]
    public class Messages
    {
        public float frameTime;
        public int frameNr;
        [SerializeField]
        public List<RecordedMessage> messages = new List<RecordedMessage>();

        public Messages(float frameTime, int frameNr)
        {
            this.frameTime = frameTime;
            this.frameNr = frameNr;
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

            initFile = true;
        }

        string uid;
        if (obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            // save all messages that are happening in one frame in same line
            if (frameNr == 0 || previousFrame != frameNr)
            {
                if (messages != null)
                {
                    File.AppendAllText(recordFile, JsonUtility.ToJson(messages) + "\n");
                    //File.AppendAllText(recordFile, recordedData + "\n", System.Text.Encoding.UTF8);
                    lineNr += 1;
                    //recordedMessages = null;
                }
                //recordedData = Time.unscaledTime + "," + frameNr;
                messages = new Messages(Time.unscaledTime, frameNr);

                previousFrame++;
            }

            //Avatar avatar = obj as Avatar;
            uid = (obj as Avatar).Properties["texture-uid"]; // get texture of avatar so we can later replay a look-alike avatar

            //recordedData = Time.unscaledTime + "," + frameNr + "," + message.ToString().Replace("\n", "\\n").Replace("\r", "\\r") + "\n";
            //recordedData = recordedData + "," + message.ToString().Replace("\n", "\\n").Replace("\r", "\\r");

            messages.messages.Add(new RecordedMessage(message.objectid, message.componentid, message.ToString()));

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
        foreach (var message in replayedMessages[currentReplayFrame])
        {
            // send and replay remotely
            //scene.Send(message);

            // replay locally
            INetworkComponent component = replayedObjects[message.objectid].components[message.componentid];
            component.ProcessMessage(message);

        }
        currentReplayFrame++;
    }

    private void CreateRecordedAvatars()
    {
        foreach (var objectid in recInfo.objectids)
        {
            // if different avatar types are used for different clients change this!
            GameObject prefab = spawner.catalogue.prefabs[3]; // Spawnable Floating BodyA Avatar
            //prefab.GetComponent<RenderToggle>();
            Avatar avatar = spawner.SpawnPersistent(prefab).GetComponent<Avatar>(); // spawns invisible avatar
            Debug.Log("CreateRecordedAvatars() " + avatar.Id);
            
            oldNewObjectids.Add(objectid, avatar.Id);

            ReplayedObjectProperties props = new ReplayedObjectProperties();
            props.gameObject = prefab;
            props.id = avatar.Id;
            INetworkComponent[] components = prefab.GetComponents<INetworkComponent>();
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
        {
            string msg;
            int i = 0;
            replayedFrames = new int[recInfo.recLinesNr];
            replayedMessages = new List<ReferenceCountedSceneGraphMessage>[recInfo.recLinesNr];
            while ((msg = await reader.ReadLineAsync()) != null)
            {
                Messages msgs = JsonUtility.FromJson<Messages>(msg);
                replayedFrames[i] = msgs.frameNr;
                replayedMessages[i] = new List<ReferenceCountedSceneGraphMessage>();
                for (int j = 0; j < msgs.messages.Count; j++)
                {
                    RecordedMessage recMsg = msgs.messages[j];
                    var sgbmsg = ReferenceCountedSceneGraphMessage.Rent(recMsg.message);
                    sgbmsg.objectid = oldNewObjectids[recMsg.objectid]; // replace old with new objectid!!!
                    sgbmsg.componentid = recMsg.componentid;
                    replayedMessages[i].Add(sgbmsg);
                }
                i++;
            }
        }
        return true;
    }

    // so we know how many of the messages belonge to one frame,
    // this is called after all connections have received their messages after one Update()
    public void NextFrame()
    {
        previousFrame = frameNr;
        frameNr += 1;
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
                
                File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(lineNr, recordedObjectids.Count,
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
                if(currentReplayFrame == recInfo.recLinesNr)
                {
                    currentReplayFrame = 0;
                }
            }


        }

    }
}

