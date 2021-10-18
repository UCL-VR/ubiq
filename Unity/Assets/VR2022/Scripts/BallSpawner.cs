using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms;
using Ubiq.Samples;
using Ubiq.XR;
using UnityEngine;


public interface IBall
{
    void Attach(Hand hand);
}
public class BallSpawner : MonoBehaviour, IUseable
{
    public GameObject BallPrefab;

    private Hand follow;
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void UnUse(Hand controller)
    {
    }

    public void Use(Hand controller)
    {
        Debug.Log("BallSpawner: Use");
        var ball = NetworkSpawner.SpawnPersistent(this, BallPrefab).GetComponents<MonoBehaviour>().Where(mb => mb is IBall).FirstOrDefault() as IBall;
        if (ball != null)
        {
            ball.Attach(controller);
        }
    }
}

