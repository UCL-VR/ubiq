using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
public struct CellBorderEventInfo
{
    public CellBorderPoint borderPoint;
    public GameObject go;
    public string objectType;
}

[RequireComponent(typeof(Collider))]
public class CellBorderPoint: MonoBehaviour
{
    [SerializeField]
    public Cell fromCell;
    [SerializeField]
    public Cell toCell;

    [HideInInspector]
    public float distanceToCell; //Only used in HexCell when creating Borders automatically

    public float ExitDelay = 5.0f;

    public class CellBorderEvent : UnityEvent<CellBorderEventInfo> { };

    public CellBorderEvent OnBorderTriggerEntered;
    public CellBorderEvent OnBorderTriggerExited;

    Dictionary<string, GameObject> triggeredObjects = new Dictionary<string, GameObject>();
    Dictionary<string, IEnumerator> removeCoroutines = new Dictionary<string, IEnumerator>();

    void Awake()
    {
        if(OnBorderTriggerEntered == null)
        {
            OnBorderTriggerEntered = new CellBorderEvent();
        }

        if(OnBorderTriggerExited == null)
        {
            OnBorderTriggerExited = new CellBorderEvent();
        }

        this.enabled = false;   
    }

    void Start()
    {
        this.name = "Border To Cell " + toCell.Name;
        GetComponent<Collider>().isTrigger = true;
    }

    void OnDisable() 
    {
        triggeredObjects.Clear();
        foreach(var item in removeCoroutines.Values)
        {
            StopCoroutine(item);
        }
        removeCoroutines.Clear();
    }

    void OnEnable()
    {
        GetComponent<Collider>().enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        bool playerTriggered = (other.name == "Player" || other.tag == "Player");
        bool invokeEvent = false;

        CellBorderEventInfo cellBorderEventInfo = new CellBorderEventInfo {
                                                    borderPoint = this,
                                                    go = other.gameObject
                                                };

        if(playerTriggered)
        {
            // Debug.Log("Player Entered Border: " + this.name);
            if(removeCoroutines.ContainsKey("Player"))
            {
                // Debug.Log("Stop Player Remove Coroutine");
                StopCoroutine(removeCoroutines["Player"]);
            }
            if(!triggeredObjects.ContainsKey("Player"))
            {
                // Debug.Log("Invoke Event For Player");
                invokeEvent = true;
                triggeredObjects.Add("Player", other.gameObject);
                cellBorderEventInfo.objectType = "Player";
            }
        }
        else
        {
            RoomObject roomObject = other.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
            if(roomObject != null)
            {
                if(removeCoroutines.ContainsKey(roomObject.Id.ToString()))
                {
                    // Debug.Log("Stop Client Agent Remove Coroutine");
                    StopCoroutine(removeCoroutines[roomObject.Id.ToString()]);
                }
                // Debug.Log("HexCell: roomObject object " + triggerInfo.triggeredObject.name + " entered trigger: " + triggerInfo.trigger.name);
                if(!triggeredObjects.ContainsKey(roomObject.Id.ToString()))
                {
                    invokeEvent = true;
                    triggeredObjects.Add(roomObject.Id.ToString(), other.gameObject);
                    cellBorderEventInfo.objectType = "RoomObject";
                }
            }

        }

        if(invokeEvent)
        {   
            OnBorderTriggerEntered.Invoke(cellBorderEventInfo);
        }
    }

    void OnTriggerExit(Collider other)
    {
        CellBorderEventInfo cellBorderEventInfo = new CellBorderEventInfo {
                                                    borderPoint = this,
                                                    go = other.gameObject
                                                };
        bool playerTriggered = (other.name == "Player" || other.tag == "Player");

        if(playerTriggered)
        {
            // Debug.Log("Player Exited Border: " + this.name);
            if(triggeredObjects.ContainsKey("Player"))
            {
                // Debug.Log("Add Coroutine For Removing Player");
                removeCoroutines["Player"] = WaitAndRemove(ExitDelay, "Player", "Player");
                StartCoroutine(removeCoroutines["Player"]);
            }
        }
        else
        {
            RoomObject roomObject = other.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
            if(roomObject != null)
            {
                // Debug.Log("Room Object Exited Border: " + this.name);
                if(triggeredObjects.ContainsKey(roomObject.Id.ToString()))
                {
                    // Debug.Log("Add Coroutine For Removing Client Agent");
                    removeCoroutines[roomObject.Id.ToString()] = WaitAndRemove(0, roomObject.Id.ToString(), "RoomObject");
                    StartCoroutine(removeCoroutines[roomObject.Id.ToString()]);
                }
            }

        }

        
        
    }

    public IEnumerator WaitAndRemove(float waitTime, string key, string type)
    {
        yield return new WaitForSeconds(waitTime);

        // Debug.Log("Remove Player Invoke");

        OnBorderTriggerExited.Invoke(new CellBorderEventInfo {
            borderPoint = this,
            go = triggeredObjects[key],
            objectType = type
        });

        triggeredObjects.Remove(key);
        
    }
}
