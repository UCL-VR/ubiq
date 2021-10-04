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
    
    public Puzzle puzzle1;
    public Puzzle puzzle2;

    public bool random = true;
    public Texture2D texture;
    public int uid = 3;

    [HideInInspector, System.NonSerialized]
    public bool hideAvatar = false;

    private NetworkScene scene;
    private RoomClient roomClient;
    private AvatarManager avatarManager;
    private NetworkedMainMenuIndicator uiIndicator;

    private ObjectHider avatarHider;
    private ObjectHider menuHider;

    public void SetTextureFromUid(int uid)
    {
        if (uid < 0)
            uid = 0;
        if (uid >= catalogue.images.Count)
            uid = catalogue.images.Count - 1;
        string suid = uid.ToString();
        if (suid != null || suid != "")
        {
            texture = catalogue.Get(suid);
        }
    }

    public void SpawnPuzzle1(Texture2D tex)
    {
        puzzle1.random = random;
        puzzle1.puzzleImage = tex;
        puzzle1.SpawnPersistentPuzzle();

    }

    public void SpawnPuzzle2(Texture2D tex)
    {
        puzzle2.random = random;
        puzzle2.puzzleImage = tex;
        puzzle2.SpawnPersistentPuzzle();

    }

    public void SpawnPuzzles()
    {
        SetTextureFromUid(uid);
        SpawnPuzzle1(texture);
        SpawnPuzzle2(texture);
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
            //avatarHider = avatarManager.LocalAvatar.gameObject.GetComponent<ObjectHider>();
            //avatarHider.SetLayer(layer);

            if (uiIndicator != null)
            {
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
    public override void OnInspectorGUI()
    {
        var t = (PuzzleMenu)target;
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Spawn Puzzles"))
            {
                t.SpawnPuzzles();
            }
            if (GUILayout.Button("Unspawn Puzzles"))
            {
                t.UnspawnPuzzles();
            }
            if (GUILayout.Button(t.hideAvatar == true ? "Show Avatar" : "Hide Avatar"))
            {
                if (t.hideAvatar) // make avatar visible again
                {
                    t.hideAvatar = !t.hideAvatar;
                    t.ShowHideAvatar(0);
                    Debug.Log("Show avatar");
                }
                else
                {
                    t.hideAvatar = !t.hideAvatar;
                    t.ShowHideAvatar(8);
                    Debug.Log(" Hide avatar");
                }
            }



            //if (GUILayout.Button("Shuffle"))
            //{
            //    t.Shuffle();
            //}
            //if (!t.random)
            //{
            //    t.puzzleImage = EditorGUILayout.ObjectField("Template", t.puzzleImage, typeof(Material), true) as Material;
            //}
        }
    }
}
# endif

