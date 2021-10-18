using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotPeerTargetsCircle : MonoBehaviour
{

    public int MaxNumberOfPeers = 6;
    public float DistanceFromCenter = 2.0f;
    public List<Vector3> Positions;
    public bool debug = false;

    void Awake()
    {
        CreatePositions();
    }
   
    void CreatePositions()
    {
        for (int i = 0; i < MaxNumberOfPeers; i++) {
            var angle = i * (360f / MaxNumberOfPeers);
            var direction = Quaternion.Euler(0, angle, 0) * Vector3.right;
            var position = transform.position + direction * DistanceFromCenter;
            Positions.Add(position);
            if(debug)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
                sphere.transform.parent = transform;
            }
            
        }

    }
    
    public Vector3 GetPosition()
    {
        return Positions[UnityEngine.Random.Range(0, Positions.Count)];
    }

}
