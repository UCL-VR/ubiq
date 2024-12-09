using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System;

namespace Ubiq.Samples
{
public class RoomJoining : MonoBehaviour
{

    public GameObject world;
    public GameObject AvatarManager;

    private Guid[] rooms;
    private RoomClient roomClient;
    private int currentRoom;
    private int nextRoom;
    private Ubiq.Avatars.Avatar playerAvatar;

    // Start is called before the first frame update
    void Start()
    {
        print("hello???");
        print("World: " + world);
        rooms = world.GetComponent<Rooms>().rooms;
        currentRoom = 0;
        print("Joining room with guid: " + rooms[currentRoom]);

        roomClient = gameObject.GetComponent<RoomClient>();
        roomClient.Join(rooms[currentRoom]);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerAvatar == null) {
            getAvatar();
        }

        Vector3 headPos = getRelativePos();
        if (headPos != Vector3.zero) {
            nextRoom = checkPos(headPos);
        }

        if (nextRoom != currentRoom) {
            currentRoom = nextRoom;
            roomClient.Join(rooms[currentRoom]);
        }
    }

    void getAvatar() { 
        foreach (Transform child in AvatarManager.transform) 
        {
            Ubiq.Avatars.Avatar avatar = child.gameObject.GetComponent<Ubiq.Avatars.Avatar>();
            if (avatar != null && avatar.IsLocal)
            {
                playerAvatar = avatar;
                break;
            }
        }
    }

    Vector3 getRelativePos() {
        Transform headTransform = playerAvatar.gameObject.transform.Find("Body/Floating_Head");
        Transform worldTransform = transform.root.Find("World");
        if (headTransform != null && worldTransform != null) {
            Vector3 pos = headTransform.position;
            Vector3 worldPos = worldTransform.position;
            pos = pos - worldPos;
            return pos;
        }
        return Vector3.zero;
    }

    void joinRoom() {
        roomClient.Join(rooms[currentRoom]);
    }

    // TODO: Streamline room joining criteria. Currently the mapping between actual criteria and transform values depends on both rotation and position.
    int checkPos(Vector3 pos) {
        if (pos.z > 3) {
            if (pos.x < -3) {
                print("Joining room with guid: " + rooms[1]);
                return 1;
            } else if (pos.x > 3) {
                print("Joining room with guid: " + rooms[2]);
                return 2;
            }
        }
        return 0;
    }
}
}