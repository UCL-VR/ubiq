using System.Collections;
using System.Collections.Generic;
using Ubiq.Avatars;
using Ubiq.Rooms;
using UnityEngine;

public class BotBoidAgent : MonoBehaviour
{
    public float pullFactor = 1;
    public float inertiaFactor = 0.005f;
    public float proximityFactor = 2.5f;
    public float proximityDistance = 5f;
    public float maxSpeed = 15f;
    public float worldBounds = 100f;

    private RoomClient roomClient;
    private AvatarManager avatarManager;

    private Vector3 velocity;

    private void Awake()
    {
        roomClient = RoomClient.Find(this);
        avatarManager = AvatarManager.Find(this);
        velocity = Random.onUnitSphere; // Random velocity kickstarts the Bots
    }

    // Start is called before the first frame update
    void Start()
    {
   
    }

    // Update is called once per frame
    void Update()
    {
        var myAvatar = avatarManager.FindAvatar(roomClient.Me);

        if(!myAvatar)
        {
            return;
        }

        Vector3 center = myAvatar.Position;
        Vector3 inertia = myAvatar.Velocity;
        int numBoids = 1;
        Vector3 proximityResponse = Vector3.zero;

        // Find the properties (center and inertia) of the flock, and at the same
        // time check for collisions/proximity.
        foreach (var peer in roomClient.Peers)
        {
            var avatar = avatarManager.FindAvatar(peer);
            if (avatar) // It may take a while for Peers to get their avatars set up (and some peers may not have any)
            {
                center += avatar.Position;
                inertia += avatar.Velocity;
                numBoids++;

                var distance = transform.position - avatar.Position;
                if (distance.magnitude < proximityDistance)
                {
                    proximityResponse += distance;
                }
            }
        }

        center /= numBoids;
        inertia /= numBoids;
        var t = Mathf.Min(Time.deltaTime, 0.1f);

        // Pull towards the center of the flock
        var pull = (center - transform.position) * pullFactor * t;

        // Move with the flock (flock inertia)
        var impulse = inertia * inertiaFactor * t;

        // Neigbour avoidance
        var proximity = proximityResponse * proximityFactor * t;

        velocity += (pull + impulse + proximity);

        // Limit the velocity
        if(velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
        }

        // Calculate new position
        transform.position += velocity * t;

        // World Bounds
        for (int j = 0; j < 3; j++)
        {
            if (transform.position[j] > worldBounds)
            {
                velocity[j] *= -1f;
            }
            if (transform.position[j] < -worldBounds)
            {
                velocity[j] *= -1f;
            }
        }
    }
}
