using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public interface ICellCoordinates
{
    int X {get;}
    int Y {get;} 
	int Z {get;}
    
}

[Serializable]
public struct CellCoordinates: ICellCoordinates
{
    [SerializeField]
    private int x,y,z;

    public int X
    {
        get
        {
            return x;
        }
    }

    public int Y
    {
        get
        {
            return y;
        }
    }

    public int Z
    {
        get
        {
            return z;
        }
    }

    public CellCoordinates(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;

    }

    public override string ToString () 
    {
		return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

    public string ToStringOnSeparateLines() 
    {
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}
}

public struct CellEventInfo
{
    public Cell cell;
    public GameObject go;
    public string objectType;
}

public interface ICell
{
    List<string> NeighborNames  {get;}
    Dictionary<string, ICell> Neighbors {get;}
    ICellCoordinates Coordinates {get; set;}
    string Name {get;}
    string CellUUID{get;}
    IGrid Grid {get;set;}
}

public class Cell: MonoBehaviour, ICell
{
    public class CellEvent : UnityEvent<CellEventInfo> { };
    public CellEvent OnEnteredCell;
    public CellEvent OnLeftCell;

    public CellEvent OnCloseToCellBorder;
    public CellEvent OnNotCloseToCellBorder;

    public string CellUUID
    {
        get
        {
            return GetCellUUID(this);
        }
    }

    public string Name
    {
        get
        {
            return this.name;
        }
    }

    public IGrid Grid
    {
        get
        {
            return _Grid;
        }
        set
        {
            _Grid = value;
        }
    }

    protected IGrid _Grid;

    [SerializeField]
    public ICellCoordinates Coordinates
    {
        get
        {
            return _Coordinates;
        }
        set
        {
            _Coordinates = value;
        }
    }

    protected ICellCoordinates _Coordinates;

    // For debug visualisation
    protected Canvas cellCanvas;

    protected Text cellLabel;

    // Keeps track of which objects have fired which triggers
    // to make sure cell entered/left events are invoked only at appropriate times
    protected Dictionary<string, List<string>> activeTriggers = new Dictionary<string, List<string>>();

    protected List<string> activeBorders = new List<string>();

    protected Dictionary<string, ICell> neighbors;

    public Dictionary<string, ICell> Neighbors
    {
        get
        {
            return neighbors;
        }
    }

    public List<string> NeighborNames
    {
        get
        {
            return Neighbors.Values.ToList().Select(n => n.Name).ToList();
        }
    }

    // List of neighboring cell game objects, these need to be set in the editor
    [SerializeField]
    public List<Cell> NeighborCellObjects = new List<Cell>();

    // List of neighboring cell game objects, these need to be set in the editor
    [SerializeField]
    public List<CellBorderPoint> BorderPointObjects;

    public Dictionary<string, CellBorderPoint> BorderPoints = new Dictionary<string, CellBorderPoint>();

    protected void SetupCell()
    {
        if(OnEnteredCell == null)
        {
            OnEnteredCell = new CellEvent();
        }

        if(OnLeftCell == null)
        {
            OnLeftCell = new CellEvent();
        }

        if(OnCloseToCellBorder == null)
        {
            OnCloseToCellBorder = new CellEvent();
        }

        if(OnNotCloseToCellBorder == null)
        {
            OnNotCloseToCellBorder = new CellEvent();
        }

        Grid = GetComponentInParent<IGrid>();

        neighbors = new Dictionary<string, ICell>();

        cellCanvas = GetComponentInChildren<Canvas>(); 
        cellLabel = GetComponentInChildren<Text>();
    }


    protected virtual void Awake()
    {
        SetupCell();
        Coordinates = new CellCoordinates((int) transform.position.x, (int) transform.position.y, (int) transform.position.z);
    }

    protected virtual void Start()
    {
        foreach (Cell cell in NeighborCellObjects)
        {
            neighbors[cell.CellUUID] = cell;
        }

        foreach (CellBorderPoint borderPoint in BorderPointObjects)
        {
            borderPoint.fromCell = this;
            BorderPoints[borderPoint.toCell.CellUUID] = borderPoint;
        }

        if(cellCanvas != null)
        {
            
            cellLabel.text = ((CellCoordinates) Coordinates).ToStringOnSeparateLines();
        }

        foreach (var trigger in GetComponentsInChildren<CellTrigger>())
        {
            trigger.SetupTrigger();
            trigger.OnTriggerEntered.AddListener(CellTriggerEntered);
            trigger.OnTriggerExited.AddListener(CellTriggerExited);
        } 
    }

    public void SetBorderPointsActive(bool active)
    {
        foreach (var item in BorderPoints.Values)
        {
            item.enabled = active;
        }
    }


    // Handle cell trigger entered events, if a new object has entered, invokes OnCellEntered event
    protected virtual void CellTriggerEntered(CellTriggerInfo triggerInfo)
    {
        CellEventInfo cellEventInfo = new CellEventInfo {
            cell = this,
            go = triggerInfo.triggeredObject
        };

        bool invokeEvent = false;
        bool playerTriggered = (triggerInfo.triggeredObject.name == "Player" || triggerInfo.triggeredObject.tag == "Player");
        if(playerTriggered) //Player triggered
        {
            if(!activeTriggers.ContainsKey("Player"))
            {
                invokeEvent = true;
                activeTriggers["Player"] = new List<string>();
                cellEventInfo.objectType = "Player";
            }

            activeTriggers["Player"].Add(triggerInfo.trigger.name);

        }
        else //Something else triggered
        {
            RoomObject roomObject = triggerInfo.triggeredObject.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
            if(roomObject != null)
            {
                // Debug.Log("HexCell: roomObject object " + triggerInfo.triggeredObject.name + " entered trigger: " + triggerInfo.trigger.name);
                if(!activeTriggers.ContainsKey(roomObject.Id.ToString()))
                {
                    // Debug.Log("HexCell: new object, create new mepty list with key: " + roomObject.Id.ToString());
                    roomObject.OnObjectDestroyed.AddListener(OnObjectDestroyed);
                    invokeEvent = true;
                    activeTriggers[roomObject.Id.ToString()] = new List<string>();
                    cellEventInfo.objectType = "RoomObject";
                }
                // Debug.Log("HexCell: add trigger: " + triggerInfo.trigger.name + " to active triggers for the object");
                activeTriggers[roomObject.Id.ToString()].Add(triggerInfo.trigger.name);
            }
        }
        
        if(invokeEvent)
        {
            // Debug.Log("Hex Cell On Trigger Enter: object: " +triggerInfo.triggeredObject.name + ", entered cell: " + this.name);
            OnEnteredCell.Invoke(cellEventInfo);
        }
    }

    // Handle trigger exited events
    // Player leaving a cell does not cause an event to be invoked, player moving from one cell to another is handled only by OnCellEntered events
    protected virtual void CellTriggerExited(CellTriggerInfo triggerInfo)
    {
        bool playerTriggered = (triggerInfo.triggeredObject.name == "Player" || triggerInfo.triggeredObject.tag == "Player");
        if(playerTriggered)
        {
            activeTriggers["Player"].Remove(triggerInfo.trigger.name);
            if(activeTriggers["Player"].Count == 0)
            {
                activeTriggers.Remove("Player");
                // Debug.Log("Hex Cell On Trigger Exit: Player, left cell: " + this.name);
            }
        }
        else //Something else triggered
        {
            RoomObject roomObject = triggerInfo.triggeredObject.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
            if(roomObject != null && activeTriggers.ContainsKey(roomObject.Id.ToString())) 
            {
                activeTriggers[roomObject.Id.ToString()].Remove(triggerInfo.trigger.name);
                // Debug.Log("Hex Cell On Trigger Exit: " + triggerInfo.triggeredObject.name + " left trigger: " + triggerInfo.trigger.name);
                if(activeTriggers[roomObject.Id.ToString()].Count == 0)
                {
                    activeTriggers.Remove(roomObject.Id.ToString());
                    OnLeftCell.Invoke(new CellEventInfo {
                        cell = this,
                        go = triggerInfo.triggeredObject,
                        objectType = "RoomObject"
                    });
                    // Debug.Log("Hex Cell On Trigger Exit: " + triggerInfo.triggeredObject.name + " left cell: " + this.name);
                }
            }

        }
        // UpdateActiveState();
    }

    protected virtual void OnBorderEnter(CellBorderEventInfo info)
    {
        if(!activeBorders.Contains(info.borderPoint.toCell.CellUUID))
        {
            activeBorders.Add(info.borderPoint.toCell.CellUUID);
        }
        OnCloseToCellBorder.Invoke(new CellEventInfo {
            cell = info.borderPoint.toCell,
            go = info.go,
            objectType = info.objectType
        });
    }

    protected virtual void OnBorderExit(CellBorderEventInfo info)
    {
        activeBorders.Remove(info.borderPoint.toCell.CellUUID);
        OnNotCloseToCellBorder.Invoke(new CellEventInfo {
            cell = info.borderPoint.toCell,
            go = info.go,
            objectType = info.objectType
        });
    }

    protected virtual void OnObjectDestroyed(RoomObjectInfo objectInfo)
    {
        // Debug.Log("HexCell: " + name + " OnObjectDestroyed: " + objectInfo.Name);
        if(activeTriggers.ContainsKey(objectInfo.Id.ToString()))
        {
            activeTriggers.Remove(objectInfo.Id.ToString());
            OnLeftCell.Invoke(new CellEventInfo {
                        cell = this,
                        objectType = "RoomObject"
                    });
        }
    }

    public static string GetCellUUID(ICell cell)
    {
        return  new Guid(Animator.StringToHash(cell.Name), 
                        (short)Animator.StringToHash(cell.Coordinates.ToString()), 
                        (short)Animator.StringToHash(SceneManager.GetActiveScene().name), 
                        new byte[]{
                            (byte)cell.Coordinates.X,
                            (byte)cell.Coordinates.Y,
                            (byte)cell.Coordinates.Z,
                            (byte)(cell.Coordinates.X+cell.Coordinates.Y),
                            (byte)(cell.Coordinates.X+cell.Coordinates.Z),
                            (byte)(cell.Coordinates.Y+cell.Coordinates.Z),
                            (byte)(cell.Coordinates.X+cell.Coordinates.X),
                            (byte)(cell.Coordinates.Z+cell.Coordinates.Z)
                        }).ToString("N");
    }

    public static string GetCellUUID(string cellName, ICellCoordinates coords)
    {
        return  new Guid(Animator.StringToHash(cellName), 
                        (short)Animator.StringToHash(coords.ToString()), 
                        (short)Animator.StringToHash(SceneManager.GetActiveScene().name), 
                        new byte[]{
                            (byte)coords.X,
                            (byte)coords.Y,
                            (byte)coords.Z,
                            (byte)(coords.X+coords.Y),
                            (byte)(coords.X+coords.Z),
                            (byte)(coords.Y+coords.Z),
                            (byte)(coords.X+coords.X),
                            (byte)(coords.Z+coords.Z)
                        }).ToString("N");
    }
}





