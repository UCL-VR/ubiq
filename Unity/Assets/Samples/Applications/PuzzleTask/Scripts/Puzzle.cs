using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Samples;
using Ubiq.Spawning;
using UnityEditor;
using Ubiq.Rooms;

/// <summary>
/// Puzzle organizes the spawning and unspawning of puzzle pieces with (random) images
/// </summary>
public class Puzzle : MonoBehaviour, INetworkObject
{
    //public GameObject puzzlePiecePrefab;
    public GameObject spawnPoint; // spawn point is in the middle of the table
    public Texture2D puzzleImage;
    public ImageCatalogue imageCatalogue;
    public PrefabCatalogue prefabCatalogue;
    public bool random = true;

    private NetworkScene scene;
    private RoomClient roomClient;

    private float minX;
    private float maxX;
    private float minZ;
    private float maxZ;

    private int unspawnedPuzzles = 0;
    private int spawnedPuzzles = 0;

    private RecorderReplayer recRep;

    [HideInInspector] public bool isSpawned = false;

    private List<GameObject> puzzlePiecesGo; // just the GameObjects that we can assign a material to
    private List<GameObject> puzzlePiecesSpawned; // the spawned puzzle pieces that have an object id and their material should not be changed as it is not networked

    private NetworkSpawner spawner;

    public NetworkId Id => throw new System.NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        scene = NetworkScene.FindNetworkScene(this);
        spawner = NetworkSpawner.FindNetworkSpawner(scene);
        roomClient = scene.gameObject.GetComponent<RoomClient>();
        roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);

        minX = spawnPoint.transform.position.x - 0.35f;
        maxX = spawnPoint.transform.position.x + 0.35f;
        minZ = spawnPoint.transform.position.z - 0.35f;
        maxZ = spawnPoint.transform.position.z + 0.35f;
        Debug.Log("spawn area: x " + minX + " " + maxX + " z " + minZ + " " + maxZ);

        puzzlePiecesGo = new List<GameObject>();
        puzzlePiecesSpawned = new List<GameObject>();

        foreach (var p in prefabCatalogue.prefabs)
        {
            if (p.name.StartsWith("part"))
            {
                puzzlePiecesGo.Add(p);
                Debug.Log("Adding " + p.name + " to puzzlePiecesGo");
            }
        }

        recRep = NetworkScene.FindNetworkScene(this).GetComponent<RecorderReplayer>();
    }

    // the NetworkSpawner makes sure that the pieces get removed, but need to remove them from the puzzlePiecesSpawned list too
    // would be possible to keep them spawned (e.g. respawn them immediately after joining the room) - not now though
    public void OnJoinedRoom(IRoom room)
    {
        puzzlePiecesSpawned.Clear();
        isSpawned = false;
    }
    public void UnspawnPuzzle()
    {
        foreach (var p in puzzlePiecesSpawned)
        {
            NetworkSpawner.UnspawnPersistent(this, p.GetComponent<PuzzlePiece>().Id);
        }
        puzzlePiecesSpawned.Clear();
        isSpawned = false;
        unspawnedPuzzles += 1;
    }

    public void SpawnPersistentPuzzle()
    {   
        if (isSpawned)
        {
            UnspawnPuzzle();
        }

        // create random spawn locations for the puzzle pieces
        Debug.Log(spawnPoint.transform.position.ToString());
        if (random)
        {
            puzzleImage = imageCatalogue.GetRandomImage();
        }
        foreach (var p in puzzlePiecesGo)
        {
            var position = new Vector3(Random.Range(minX, maxX), Random.Range(spawnPoint.transform.position.y, spawnPoint.transform.position.y + 0.2f), Random.Range(minZ, maxZ));
            TransformMessage transform = new TransformMessage(position, new Vector3(0.0f, Random.Range(0, 360), 0.0f));
            var piece = spawner.SpawnPersistentReplay(p, true, imageCatalogue.Get(puzzleImage), false, transform);
            puzzlePiecesSpawned.Add(piece);
            Debug.Log("Spawn a piece at: " + piece.transform.position.ToString() + ", rot: " + piece.transform.eulerAngles.ToString());
        }
        isSpawned = true;
        spawnedPuzzles += 1;
        Debug.Log("Spawned vs unspawned: " + spawnedPuzzles + " " + unspawnedPuzzles);
    }
    // Just shuffles existing puzzle, without spawning a new one
    public void Shuffle()
    {
        if (puzzlePiecesSpawned != null)
        {
            foreach(var p in puzzlePiecesSpawned)
            {
                PuzzlePiece pp = p.GetComponent<PuzzlePiece>();
                var position = new Vector3(Random.Range(minX, maxX), Random.Range(spawnPoint.transform.position.y, spawnPoint.transform.position.y + 0.2f), Random.Range(minZ, maxZ));
                p.transform.position = position;
                p.transform.eulerAngles = new Vector3(0.0f, Random.Range(0, 360), 0.0f);
                pp.SetNetworkedTransform(p.transform);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(recRep.replaying && isSpawned)
        {
            UnspawnPuzzle();
            isSpawned = false;
        }
    }
}

# if UNITY_EDITOR
[CustomEditor(typeof(Puzzle))]
public class PuzzleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var t = (Puzzle)target;
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Spawn"))
            {
                t.SpawnPersistentPuzzle();
            }

            if (GUILayout.Button("Shuffle"))
            {
                t.Shuffle();
            }
            if (GUILayout.Button("Unspawn"))
            {
                t.UnspawnPuzzle();
            }
            //if (!t.random)
            //{
            //    t.puzzleImage = EditorGUILayout.ObjectField("Template", t.puzzleImage, typeof(Material), true) as Material;
            //}
        }
    }
}
# endif
