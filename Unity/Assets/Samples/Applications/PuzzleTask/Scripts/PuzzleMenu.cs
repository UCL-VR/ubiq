using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Rooms;
using Ubiq.Samples;


public class PuzzleMenu : MonoBehaviour
{
    public ImageCatalogue catalogue;
    public MainMenu mainMenu;
    public GameObject mainPanel; // where the record/replay menu is

    public Puzzle puzzle1;
    public Puzzle puzzle2;
    [HideInInspector]
    public bool random = false;
    [HideInInspector]
    public bool useOneTexture = true;
    [HideInInspector]
    public Texture2D texture;
    [HideInInspector]
    public Texture2D texture1;
    [HideInInspector]
    public Texture2D texture2;

    [HideInInspector]
    public RecorderReplayerMenu recRepMenu;

    [HideInInspector, System.NonSerialized]
    public bool hideAvatar = false;

    private Dictionary<int, int[]> collaboration;
    private Dictionary<int, int[]> competition;


    private NetworkScene scene;
    private RoomClient roomClient;
    private AvatarManager avatarManager;
    private NetworkedMainMenuIndicator uiIndicator;

    private ObjectHider avatarHider;
    private ObjectHider menuHider;

    // not play more than two puzzles anyways
    public int numberCollab = 1; // number of puzzle in collab/comp dictionary
    public int numberComp = 1; // competition dictionary has one puzzle less than collaboration


    public Texture2D SetTextureFromUid(int uid)
    {
        if (uid < 0)
            uid = 0;
        if (uid >= catalogue.images.Count)
            uid = catalogue.images.Count - 1;
        string suid = uid.ToString();
        if (suid != null || suid != "")
        {
            return texture = catalogue.Get(suid);
        }
        return texture;
    }

    public void SpawnPuzzlePair(string mode, int number)
    {
        if (number > collaboration.Count || number > competition.Count)
        {
            numberCollab = 1;
            numberComp = 1;
        }
        
        int p1, p2;
        if (mode == "col")
        {
            p1 = collaboration[number][0];
            p2 = collaboration[number][1];
        }
        else if (mode == "com")
        {
            p1 = competition[number][0];
            p2 = competition[number][1];
        }
        else
        {
            //Test puzzle
            p1 = collaboration[0][0];
            p2 = collaboration[0][1];
        }
 
        texture1 = SetTextureFromUid(p1);
        texture2 = SetTextureFromUid(p2);

        puzzle1.puzzleImage = texture1;
        puzzle1.SpawnPersistentPuzzle();

        puzzle2.puzzleImage = texture2;
        puzzle2.SpawnPersistentPuzzle();
    }

    public void UnspawnPuzzles()
    {
        puzzle1.UnspawnPuzzle();
        puzzle2.UnspawnPuzzle();
    }

    public void ShowHideAvatar(int layer)
    {
        if (avatarManager.LocalAvatar != null)
        {
            avatarManager.LocalAvatar.Peer["visible"] = layer == 0 ? "1" : "0";
            // if not in a room use this
            avatarHider = avatarManager.LocalAvatar.gameObject.GetComponent<ObjectHider>();
            avatarHider.SetLayer(layer);

            if (uiIndicator != null)// uiIndicator is of type NetworkedMainMenuIndicator and is the indicator of the local menu that would be sent to remote peers
            {
                // menuHider is the ObjectHider from the menu indicator
                menuHider.SetNetworkedObjectLayer(layer);
            }
        }
    }

    public void OnMenuIndicatorSpawned(NetworkedMainMenuIndicator uiIndicator)
    {
        Debug.Log("OnMenuIndicatorSpawned");
        this.uiIndicator = uiIndicator;
        menuHider = this.uiIndicator.gameObject.GetComponent<ObjectHider>();
    }

    // Start is called before the first frame update
    void Start()
    {
        recRepMenu = mainPanel.GetComponent<RecorderReplayerMenu>();
        collaboration = new Dictionary<int, int[]>();
        competition = new Dictionary<int, int[]>();
        collaboration.Add(0, new int[] { 0, 19 });

        collaboration.Add(1, new int[] { 11, 12 });
        collaboration.Add(2, new int[] { 17, 18 });

        competition.Add(1, new int[] { 9, 10 });
        competition.Add(2, new int[] { 15, 16 });

        collaboration.Add(3, new int[] { 1, 2 });
        collaboration.Add(4, new int[] { 3, 4 });
        collaboration.Add(5, new int[] { 5, 6 });
        collaboration.Add(6, new int[] { 7, 8 });

        competition.Add(3, new int[] { 13, 14 });

        scene = NetworkScene.FindNetworkScene(this);
        roomClient = scene.GetComponent<RoomClient>();
        avatarManager = scene.GetComponentInChildren<AvatarManager>();
        mainMenu.OnIndicatorSpawned.AddListener(OnMenuIndicatorSpawned);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

# if UNITY_EDITOR
[CustomEditor(typeof(PuzzleMenu))]
public class PuzzleMenuEditor : Editor
{
    bool avatarHidden, testSpawned, rec,
        recCollab, collabSpawned,
        recComp, compSpawned = false;

    bool enabledCollab = true;
    bool enabledComp = true;
    public override void OnInspectorGUI()
    {
        var t = (PuzzleMenu)target;
        DrawDefaultInspector();

        var white = new GUIStyle(GUI.skin.button);
        var black = new GUIStyle(GUI.skin.button);
        var red = new GUIStyle(GUI.skin.button);
        var labelRed = new GUIStyle(GUI.skin.label);
        var labelGreen = new GUIStyle(GUI.skin.label);

        white.normal.background = Texture2D.whiteTexture;
        black.normal.background = Texture2D.blackTexture;
        red.normal.background = Texture2D.redTexture;
        labelRed.normal.textColor = Color.red;
        labelGreen.normal.textColor = Color.green;


        //if (Application.isPlaying)
        //{
        EditorGUILayout.LabelField("<<< Workflow Checklist >>>");
        EditorGUILayout.LabelField("Explain teleport!");
        EditorGUILayout.LabelField("Guardian area");
        EditorGUILayout.LabelField("Grabbing objects (test puzzle)");

        if (GUILayout.Button(avatarHidden == true ? "1) Show Avatar" : "1) Hide Avatar"))
        {
            if (avatarHidden) // make avatar visible again
            {
                avatarHidden = !avatarHidden;
                t.ShowHideAvatar(0);
                Debug.Log("Show avatar");
            }
            else
            {
                avatarHidden = !avatarHidden;
                t.ShowHideAvatar(8);
                Debug.Log(" Hide avatar");
            }
        }
        if (GUILayout.Button(testSpawned == true ? "2) Unspawn Test" : "2) Spawn Test"))
        {
            if (testSpawned)
            {
                testSpawned = !testSpawned;
                t.UnspawnPuzzles();
            }
            else
            {
                testSpawned = !testSpawned;
                t.SpawnPuzzlePair("test", 0);

            }
        }

        EditorGUILayout.LabelField("START recording!!!", labelRed);
        GUI.enabled = enabledCollab;
        if (GUILayout.Button(rec == true ? "2) STOP recording" : "2) START recording", red))
        {
            rec = !rec;
            enabledComp = !enabledComp;
            t.recRepMenu.ToggleRecord();
        }

        //GUI.enabled = true;
        if (GUILayout.Button(collabSpawned == true ? "2) Unspawn Collab" : "2) Spawn Collab"))
        {
            if (collabSpawned)
            {
                collabSpawned = !collabSpawned;
                t.UnspawnPuzzles();
            }
            else
            {
                collabSpawned = !collabSpawned;
                t.SpawnPuzzlePair("col", t.numberCollab);
                Debug.Log("Spawn competitive puzzle " + t.numberCollab);
                t.numberCollab++;

            }
        }
        EditorGUILayout.LabelField("Puzzle number: " + (t.numberCollab-1));


        EditorGUILayout.LabelField("STOP recording!!!", labelGreen);

        EditorGUILayout.LabelField("Next task is competitive!");

        EditorGUILayout.LabelField("START recording!!!", labelRed);
        GUI.enabled = enabledComp;
        if (GUILayout.Button(rec == true ? "2) STOP recording" : "2) START recording", red))
        {
            rec = !rec;
            enabledCollab = !enabledCollab;
            t.recRepMenu.ToggleRecord();
        }
        if (GUILayout.Button(compSpawned == true ? "2) Unspawn Comp" : "2) Spawn Comp"))
        {
            if (compSpawned)
            {
                compSpawned = !compSpawned;
                t.UnspawnPuzzles();
            }
            else
            {
                compSpawned = !compSpawned;
                t.SpawnPuzzlePair("com", t.numberComp);
                Debug.Log("Spawn competitive puzzle " + t.numberComp);
                t.numberComp++;
            }
        }
        EditorGUILayout.LabelField("Puzzle number: " + (t.numberComp - 1));

        EditorGUILayout.LabelField("STOP recording!!!", labelGreen);
        //}

        EditorGUILayout.LabelField("START recording for collaborative debrief!!!", labelRed);
        EditorGUILayout.LabelField("START recording for competitive debrief!!!", labelRed);


    }
}
# endif

