using System.Collections;
using System.Collections.Generic;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JoinRoomClients : MonoBehaviour
{
    public bool JoinOnStart = false;

    public string Guid = null;

    // Start is called before the first frame update
    void Start()
    {
        if(Guid == null || Guid == "")
        {
            Guid = System.Guid.NewGuid().ToString();
        }
        foreach (var forest in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var item in forest.GetComponentsInChildren<RoomClient>())
            {
                item.Join(System.Guid.Parse(Guid));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
