using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Avatar = Ubiq.Avatars.Avatar;

public class RecorderReplayer : MonoBehaviour, INetworkObject, INetworkComponent
{
    public bool recording = false;
    public bool replaying = false;

    public NetworkScene scene;
    public AvatarManager avatarManager;

    private string path;

    // Recording
    private string recordFile;
    private string recordFileIDs; // save the objectIDs of the recorded avatars
    private string recordedData;  // format of recorded data: (time), frame, object ID, component ID, sgbmessage
    private Dictionary<NetworkId, string> recordedObjectids;
    private int lineNr = 0; // number of lines in recordFile
    private int frameNr = 0;
    private bool initFile = false;

    // Replaying
    public string replayFile;
    private ReferenceCountedSceneGraphMessage[] replayedMessages;
    private int[] replayedFrames;
    private int replayedDataLength;
    private int numberOfRecAvatars;
    private int numberOfRecLines;
    private Dictionary<NetworkId, string> replayedObjectids;
    private bool initReplay = false;
    

    public NetworkId Id { get; } = new NetworkId();

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        throw new System.NotImplementedException();
    }

    public void Record(INetworkObject obj, ReferenceCountedSceneGraphMessage message)
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
        if(obj is Avatar) // check it here too in case we later record other things than avatars as well
        {
            //Avatar avatar = obj as Avatar;
            uid = (obj as Avatar).Properties["texture-uid"]; // get texture of avatar so we can later replay a look-alike avatar
            
            recordedData = Time.unscaledTime + "," + frameNr + "," + message.ToString().Replace("\n","\\n").Replace("\r","\\r") + "\n";

            if (!recordedObjectids.ContainsKey(message.objectid))
            {
                recordedObjectids.Add(message.objectid, uid);
            }
            lineNr += 1;
        }
        File.AppendAllText(recordFile, recordedData, System.Text.Encoding.UTF8);
    }

    public void Replay()
    {
        // Load data from file into (dunno yet)
        // keep change of IDs in mind
        // create new avatars and change IDs of messages to be sent to new avatars
    }

    private void ChangeObjectIdsOfMessage()
    {

    }

    public async void LoadRecording(string replayFile)
    {
        initReplay = true;

        if (File.Exists(path + "/" + replayFile + "IDs.txt"))
        {
            using (StreamReader reader = File.OpenText(path + "/" + replayFile + "IDs.txt"))
            {
                await LoadRecInfo(reader);
            }
        }

        if (File.Exists(path + "/" + replayFile + ".txt"))
        {

            using (StreamReader reader = File.OpenText(path + "/" + replayFile + ".txt"))
            {
                Debug.Log("Load recording...");
                
                await LoadMessages(reader);

                Debug.Log("Loading finished!");
            }

        }

    }

    private async Task LoadMessages(StreamReader reader)
    {
        string msg;
        string[] msgParts;
        int i = 0;
        replayedFrames = new int[numberOfRecLines];
        replayedMessages = new ReferenceCountedSceneGraphMessage[numberOfRecLines];
        while ((msg = await reader.ReadLineAsync()) != null)
        {
            msgParts = msg.Split(','); // time, frameNr, message
            replayedFrames[i] = int.Parse(msgParts[1]);
            replayedMessages[i] = ReferenceCountedSceneGraphMessage.Rent(msgParts[2].Replace("\\n", "\n").Replace("\\r", "\r"));

            i++;
        }
    }

    private async Task LoadRecInfo(StreamReader sr)
    {
        //string[] recInfo = File.ReadAllLines(path + "/" + replayFile + "IDs.txt");

        //numberOfRecLines = int.Parse(recInfo[0].Split(',')[0]);
        //numberOfRecAvatars = int.Parse(recInfo[1].Split(',')[0]);

        //for (int i = 2; i < recInfo.Length; i++) // ignore first two entries
        //{
        //    var s = recInfo[i].Split(',');

        //    replayedObjectids.Add(new NetworkId(s[0]), s[1]);
        //}
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
        //recordFile = path + "/" + recordFile;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        recordedObjectids = new Dictionary<NetworkId, string>();
        replayedObjectids = new Dictionary<NetworkId, string>();
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
                using (StreamWriter file = new StreamWriter(recordFileIDs))
                {
                    file.WriteLine("{0},{1}", lineNr, "lineNr"); // number of lines in recording
                    file.WriteLine("{0},{1}", recordedObjectids.Count, "avatarNr"); // number of avatars in recording
                    foreach (var entry in recordedObjectids)
                    {
                        file.WriteLine("{0},{1}", entry.Key, entry.Value);
                    }
                }
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
            if (!initReplay)
            {
                LoadRecording(replayFile);
            }
        }

    }
}

