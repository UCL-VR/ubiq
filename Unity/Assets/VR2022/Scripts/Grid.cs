using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrid
{
    ICell PlayerCell {get;set;}
    Dictionary<string, ICell> Cells {get;}
}

public class Grid : MonoBehaviour, IGrid
{
    public Cell.CellEvent OnPlayerCellChanged;
    public Cell.CellEvent OnActiveCellsChanged;

    public Cell.CellEvent OnObjectEnteredCell;
    public Cell.CellEvent OnObjectLeftCell;

    public Cell.CellEvent OnEnteredCellBorder;
    public Cell.CellEvent OnLeftCellBorder;

    public List<string> activeCells = new List<string>();
    // protected Dictionary<string, int> objectsInCellCounts = new Dictionary<string, int>();

    protected string PlayerCellName; // For inspector view debugging

    protected ICell _PlayerCell;
    public ICell PlayerCell
    {
        get
        {
            return _PlayerCell;
        }
        set
        {
            if(_PlayerCell != null)
            {
                ((Cell)_PlayerCell).SetBorderPointsActive(false);
            }
            _PlayerCell = value;
            ((Cell)_PlayerCell).SetBorderPointsActive(true);
            PlayerCellName = _PlayerCell.Name;
        }
    }

    protected Dictionary<string, ICell> cellDictionary;

    public Dictionary<string, ICell> Cells
    {
        get
        {
            return cellDictionary;
        }
    }

    protected virtual void Awake()
    {
        if(OnPlayerCellChanged == null)
        {
            OnPlayerCellChanged = new Cell.CellEvent();
        }

        if(OnActiveCellsChanged == null)
        {
            OnActiveCellsChanged = new Cell.CellEvent();
        }

        if(OnObjectEnteredCell == null)
        {
            OnObjectEnteredCell = new Cell.CellEvent();
        }

        if(OnObjectLeftCell == null)
        {
            OnObjectLeftCell = new Cell.CellEvent();
        }

        if(OnEnteredCellBorder == null)
        {
            OnEnteredCellBorder = new Cell.CellEvent();
        }

        if(OnLeftCellBorder == null)
        {
            OnLeftCellBorder = new Cell.CellEvent();
        }

        cellDictionary = new Dictionary<string, ICell>();
    }

    protected virtual void Start()
    {
        foreach (var item in GetComponentsInChildren<ICell>())
        {
            cellDictionary[item.CellUUID] = item;
            ((Cell)item).OnEnteredCell.AddListener(OnCellEntered);
            ((Cell)item).OnLeftCell.AddListener(OnLeftCell);
            ((Cell)item).OnCloseToCellBorder.AddListener(OnBorder);
            ((Cell)item).OnNotCloseToCellBorder.AddListener(OnNotBorder);
        }
        
    }

    protected virtual void OnCellEntered(CellEventInfo info)
    {
        // Debug.Log("Grid: OnCellEntered: " + info.cell.gameObject.name + ", " + info.go.name);
        
        if(info.objectType != "Player")
        {
            // if(!objectsInCellCounts.ContainsKey(info.cell.CellUUID))
            // {
            //     objectsInCellCounts[info.cell.CellUUID] = 0;
            // }
            // objectsInCellCounts[info.cell.CellUUID]++;
            // Debug.Log("number of objects in cell: " + objectsInCellCounts[info.cell.CellUUID]);
            // Debug.Log("HexGrid: Invoke OnObjectEnteredCell");
            OnObjectEnteredCell.Invoke(info);
            // if(!activeCells.Contains(info.cell.CellUUID))
            // {
                // activeCells.Add(info.cell.CellUUID);
                // UpdateActiveCells();
            // }

            
            return;
        }
        // Cell cell = (Cell) info.cell;
        
        
        if(PlayerCell != null && info.cell.CellUUID == PlayerCell.CellUUID)
        {
            return;
        }

        var oldPlayerCell = PlayerCell;
        // if(oldPlayerCell != null)
        // {
        //     activeCells.Remove(oldPlayerCell.CellUUID);
        // }

        PlayerCell = info.cell;
        // activeCells.Add(info.cell.CellUUID);
        // if(oldPlayerCell != null)
        // {
            // objectsInCellCounts[oldPlayerCell.CellUUID]--;
            // Debug.Log("number of objects in old player cell: " + objectsInCellCounts[oldPlayerCell.CellUUID]);
            // if(objectsInCellCounts[oldPlayerCell.CellUUID] == 0)
            // {
            //     activeCells.Remove(oldPlayerCell.CellUUID);
            // }
        // }

        // if(!objectsInCellCounts.ContainsKey(info.cell.CellUUID))
        // {
        //     objectsInCellCounts[info.cell.CellUUID] = 0;
        // }
        // objectsInCellCounts[info.cell.CellUUID]++;
        // Debug.Log("number of objects in new player cell: " + objectsInCellCounts[info.cell.CellUUID]);

        if(OnPlayerCellChanged != null)
        {
            OnPlayerCellChanged.Invoke(info);
        }

        // if(!activeCells.Contains(info.cell.CellUUID))
        // {
        //     activeCells.Add(info.cell.CellUUID);
        // }
        // UpdateActiveCells();

        // foreach (HexCell neighbor in cell.Neighbors.Values)
        // {
        //     if(!activeCells.Contains(neighbor))
        //     {
        //         activeCells.Add(neighbor);
        //     }
        // }

        // if(oldPlayerCell != null)
        // {
        //     foreach (HexCell neighbor in oldPlayerCell.Neighbors.Values)
        //     {
        //         if(neighbor != PlayerCell && activeCells.Contains(neighbor) && !PlayerCell.Neighbors.ContainsValue(neighbor))
        //         {
        //             activeCells.Remove(neighbor);
        //         }
        //     }
        // }
        
        

        // UpdateActiveCells();
        
    }

    protected virtual void OnLeftCell(CellEventInfo info)
    {
        // Debug.Log("Cell: OnLeftCell: " + info.cell.gameObject.name);
        if(info.go != null)
        {   
            OnObjectLeftCell.Invoke(info);
        }
        // activeCells.Remove(info.cell.CellUUID);
        // if(info.cell.ObjectsInCell.Count == 0)
        // {
            // UpdateActiveCells();
        // if(info.cell.ObjectsInCell.Count == 0)
        // {

        // }
        // objectsInCellCounts[info.cell.CellUUID]--;
        // Debug.Log("number of objects in cell: " + objectsInCellCounts[info.cell.CellUUID]);
        // if(objectsInCellCounts[info.cell.CellUUID] == 0)
        // {
        //     activeCells.Remove(info.cell.CellUUID);
        //     UpdateActiveCells();
        // } 
    }

    protected virtual void OnBorder(CellEventInfo info)
    {
        // Debug.Log("OnBorder: "+ info.cell.name + ", " + info.go.name);

        // if(!objectsInCellCounts.ContainsKey(info.cell.CellUUID))
        // {
        //     objectsInCellCounts[info.cell.CellUUID] = 0;
        // }
        // objectsInCellCounts[info.cell.CellUUID]++;
        // Debug.Log("number of objects in cell: " + objectsInCellCounts[info.cell.CellUUID]);

        // activeCells.Add(info.cell.CellUUID);
        // UpdateActiveCells();
        // if(info.objectType == "RoomObject")
        // {
        //     info.cell.SetBorderPointsActive(true);
        // }
        
        
        OnEnteredCellBorder.Invoke(info);
    }

    protected virtual void OnNotBorder(CellEventInfo info)
    {
        // Debug.Log("OnNotBorder: "+ info.cell.name + ", " + info.go.name);
        // activeCells.Remove(info.cell.CellUUID);
        // if(info.cell.ObjectsInCell.Count == 0)
        // {
            // UpdateActiveCells();
        // }
        // objectsInCellCounts[info.cell.CellUUID]--;
        // Debug.Log("number of objects in cell: " + objectsInCellCounts[info.cell.CellUUID]);
        // if(objectsInCellCounts[info.cell.CellUUID] == 0)
        // {
        //     activeCells.Remove(info.cell.CellUUID);
        //     UpdateActiveCells();
        // }
       
        // if(activeCells.Contains(info.cell.CellUUID) && PlayerCell.CellUUID != info.cell.CellUUID)
        // {
        //     activeCells.Remove(info.cell.CellUUID);
        //     UpdateActiveCells();
        // }
        OnLeftCellBorder.Invoke(info);

        
    }

    public void UpdateActiveCells()
    {
        // foreach (var cell in cellDictionary.Values)
        // {
        //     ((Cell)cell).gameObject.SetActive(activeCells.Contains(cell.CellUUID));
            
        // }

    }

}
