using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
public class ClientAgentSpawner : MonoBehaviour
{

    public GameObject ClientAgentPrefab;

    public int MaxAgents;
    public int AgentsCount;

    public float SpawnFrequency;

    float lastSpawnTime;
    NavMeshTriangulation triangulation;

    public Transform[] TargetPoints;

    // Start is called before the first frame update
    void Start()
    {
        triangulation = NavMesh.CalculateTriangulation();
        lastSpawnTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time - lastSpawnTime > SpawnFrequency && AgentsCount < MaxAgents)
            SpawnAgent();
    }

    void SpawnAgent()
    {
        var agent = GameObject.Instantiate(ClientAgentPrefab, TargetPoints[AgentsCount % TargetPoints.Length].position, Quaternion.identity, transform);
        agent.GetComponentInChildren<ClientAgent>().SetTargetPoints(TargetPoints);
        lastSpawnTime = Time.time;
        AgentsCount++;
    }

    Vector3 GetRandomPointOnNavMesh()
    {
        
        
        // Pick the first indice of a random triangle in the nav mesh
        int t = Random.Range(0, triangulation.indices.Length-3);
        
        // Select a random point on it
        Vector3 point = Vector3.Lerp(triangulation.vertices[triangulation.indices[t]], 
                                    triangulation.vertices[triangulation.indices[t+1]], 
                                    Random.value);
                                    
        Vector3.Lerp(point, triangulation.vertices[triangulation.indices[t+2]], Random.value);

        return point;
    }
}
*/