using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Samples;
using Ubiq.Spawning;
using UnityEditor;

public class Puzzle : MonoBehaviour, INetworkObject
{
    public string puzzleName = null;
    public GameObject puzzlePiecePrefab;
    public GameObject spawnPoint; // spawn point is in the middle of the table

    private float minX;
    private float maxX;
    private float minZ;
    private float maxZ;

    [HideInInspector] public bool isSpawned = false;

    private string pathToPuzzles;
    private List<Texture2D> pTextures;
    private List<GameObject> pPieces;

    public NetworkId Id => throw new System.NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        minX = spawnPoint.transform.position.x - 0.4f;
        maxX = spawnPoint.transform.position.x + 0.4f;
        minZ = spawnPoint.transform.position.z - 0.4f;
        maxZ = spawnPoint.transform.position.z - 0.4f;

        pathToPuzzles = Application.dataPath + "/Samples/Applications/PuzzleTask/PiecesData";
        if (puzzleName != null)
        {
            pathToPuzzles = pathToPuzzles + "/" + puzzleName;
            Debug.Log("Puzzle path: " + pathToPuzzles);
        }
        else
        {
            Debug.Log("No valid puzzle path!");
        }

        if (Directory.Exists(pathToPuzzles))
        {
            pTextures = new List<Texture2D>();
            Texture2D tex;
            string[] pieces = Directory.GetFiles(pathToPuzzles, "*.png");
            foreach (var piece in pieces)
            {
                byte[] img = File.ReadAllBytes(piece);
                tex = new Texture2D(2, 2); // size does not matter as it will be overwritten by new image
                tex.LoadImage(img);
                pTextures.Add(tex);
            }
        }

        pPieces = new List<GameObject>();

        //SpawnPersistentPuzzle();
    }

    public void SpawnPersistentPuzzle()
    {
        foreach (var tex in pTextures)
        {
            var piece = NetworkSpawner.SpawnPersistent(this, puzzlePiecePrefab);
            piece.GetComponent<TexturedObject>().SetTexture(tex);
            pPieces.Add(piece);
            Debug.Log("Spawn a piece");
        }
        isSpawned = true;
    }

    // Update is called once per frame
    void Update()
    {
        
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
            if (GUILayout.Button("SpawnPuzzle") && !t.isSpawned)
            {
                t.SpawnPersistentPuzzle();
            } 
        }
    }
}

# endif
