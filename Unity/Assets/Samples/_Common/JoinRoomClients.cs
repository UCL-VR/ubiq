using System.Collections;
using System.Collections.Generic;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JoinRoomClients : MonoBehaviour
{
    public bool JoinOnStart = false;

    // Start is called before the first frame update
    void Start()
    {
        var guid = System.Guid.NewGuid();
        foreach (var forest in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var item in forest.GetComponentsInChildren<RoomClient>())
            {
                item.Join(guid);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
