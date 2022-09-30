using System.Collections;
using System.Collections.Generic;
using Ubiq.Rooms;
using UnityEngine;

public class RoomKnnAgent : MonoBehaviour
{
    public Transform PlayerTransform;

    RoomClient roomClient;

    private void Awake()
    {
        roomClient = GetComponent<RoomClient>();
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PositionUpdate());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator PositionUpdate()
    {
        while (true)
        {
            roomClient.Me["x.scalability.position"] = JsonUtility.ToJson(PlayerTransform.position);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
