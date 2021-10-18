using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HexGrid : Grid
{
    public float outerRadius = 20f;

	public float innerRadius 
    {
        get
        {
            return outerRadius * 0.866025404f;
        }
    } 

    public Vector3[] corners
    {
        get
        {
            Vector3[] arr = { 
                            new Vector3(0f, 0f, outerRadius),
                            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
                            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
                            new Vector3(0f, 0f, -outerRadius),
                            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
                            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
                            new Vector3(0f, 0f, outerRadius)
                            };
            return arr;
        }
    }

    public int width = 6;
	public int height = 6;
    public bool showCells = false;
    public bool expanding = false;

	public HexCell cellPrefab;

    


    public Text cellLabelPrefab;

    public GameObject map;

    Vector3 size;

    Vector3 averagePosition;

	override protected void Awake() 
    {
        base.Awake();
	}

    // Start is called before the first frame update
    override protected void Start()
    {
        if(map != null)
        {
            var renderer = map.GetComponent<Renderer>();
            if(renderer != null)
            {
                Debug.Log(renderer.bounds.size);
                size = renderer.bounds.size;
                width = Mathf.CeilToInt(size.x /(innerRadius * 1.5f));
                height = Mathf.CeilToInt(size.z /(innerRadius * 1.5f));
                var collider = map.GetComponent<Collider>();
                if(collider != null)
                {
                    Debug.Log(collider.bounds.size);
                }
            }
            else
            {
                var collider = map.GetComponent<Collider>();
                if(collider != null)
                {
                    Debug.Log(collider.bounds.size);
                    size = collider.bounds.size;
                    width = Mathf.CeilToInt(size.x /(outerRadius * 1.5f));
                    height = Mathf.CeilToInt(size.z /(outerRadius * 1.5f));
                }
            }
        }
        if(height != 0 && width != 0 && !expanding)
        {
            expanding = false;
            CreateFixedSizeGrid();

            averagePosition /= cellDictionary.Count;

            if(map != null)
            {
                Vector3 gridCentre = averagePosition;

                var renderer = map.GetComponent<Renderer>();
                if(renderer != null)
                {
                    Debug.Log(renderer.bounds.center);
                    transform.localPosition = renderer.bounds.center - gridCentre;
                }
                else
                {
                    var collider = map.GetComponent<Collider>();
                    if(collider != null)
                    {
                        Debug.Log(collider.bounds.center);
                        transform.localPosition = collider.bounds.center - gridCentre;
                    }
                }
            }
        }
        else
        {
            expanding = true;
            CreateExpadingGrid();
        }
    }


    void CreateFixedSizeGrid()
    {
        for (int z = 0; z < height; z++) 
        {
            for (int x = 0; x < width; x++) 
            {
                HexCell cell = CreateCell(x, z);
            }
        }
    }

    void CreateExpadingGrid()
    {
        HexCell cell = CreateCell(0, 0);
        foreach (var item in ((HexCoordinates) cell.Coordinates).GetNeighborCoordinates())
        {
            if(!cellDictionary.ContainsKey(item.ToString()))
            {
                HexCell neighbor = CreateCell(item.X + item.Z / 2, item.Z);

                cell.Neighbors[Cell.GetCellUUID(neighbor)] = neighbor;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    HexCell CreateCell (int x, int z) 
    {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (innerRadius * 2f);
		position.y = 0f;
		position.z = z * (outerRadius * 1.5f);

        averagePosition += position;

		HexCell cell = Instantiate<HexCell>(cellPrefab);
        cell.Grid = this;
		cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.name = "Hex Cell " + ((HexCoordinates) cell.Coordinates).ToString();
        var key = Cell.GetCellUUID(cell);
        cellDictionary[key] = cell;

        cell.OnEnteredCell.AddListener(OnCellEntered);
        cell.OnLeftCell.AddListener(OnLeftCell);
        cell.OnCloseToCellBorder.AddListener(OnBorder);
        cell.OnNotCloseToCellBorder.AddListener(OnNotBorder);

        cell.SetVisible(showCells);

        return cell;

	}

    protected override void OnCellEntered(CellEventInfo info)
    {
        // Debug.Log("HexGrid: OnCellEntered: " + info.cell.Name + ", " + info.go.name);

        if(info.objectType != "Player")
        {   
            // Debug.Log("Not player");
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
            // info.cell.SetBorderPointsActive(true);
            
            return;
        }

        HexCell cell = (HexCell) info.cell;    
        
        if(PlayerCell != null && cell.CellUUID == PlayerCell.CellUUID)
        {
            return;
        }
        
        var oldPlayerCell = PlayerCell;
        // if(oldPlayerCell != null)
        // {
        //     activeCells.Remove(oldPlayerCell.CellUUID);
        // }
        PlayerCell = info.cell;

        // if(oldPlayerCell != null)
        // {

        //     objectsInCellCounts[oldPlayerCell.CellUUID]--;
        //     Debug.Log("number of objects in old player cell: " + objectsInCellCounts[oldPlayerCell.CellUUID]);
        //     if(objectsInCellCounts[oldPlayerCell.CellUUID] == 0)
        //     {
        //         activeCells.Remove(oldPlayerCell.CellUUID);
        //     }
        // }

        // if(!objectsInCellCounts.ContainsKey(info.cell.CellUUID))
        // {
        //     objectsInCellCounts[info.cell.CellUUID] = 0;
        // }
        // objectsInCellCounts[info.cell.CellUUID]++;
        // Debug.Log("number of objects in new player cell: " + objectsInCellCounts[info.cell.CellUUID]);

        if(expanding)
        {
            foreach (var item in ((HexCoordinates) cell.Coordinates).GetNeighborCoordinates())
            {
                var key = HexCell.GetCellUUID("Hex Cell " + item.ToString(), item);
                // Debug.Log("Player Cell Neighbor Coordinate" + item.ToString());
                if(!cellDictionary.ContainsKey(key))
                {
                    // Debug.Log("Create new Cell for Player Cell Neighbor Coordinate");
                    HexCell neighbor = CreateCell(item.X + item.Z / 2, item.Z);

                    PlayerCell.Neighbors[key] = neighbor;
                    neighbor.Neighbors[PlayerCell.CellUUID] = (HexCell) PlayerCell;
                }
            }
        }
        
        if(OnPlayerCellChanged != null)
        {
            OnPlayerCellChanged.Invoke(info);
        }

        // if(!activeCells.Contains(info.cell.CellUUID))
        // {
            // activeCells.Add(info.cell.CellUUID);
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
        //         if(neighbor.CellUUID != PlayerCell.CellUUID && activeCells.Contains(neighbor) && !PlayerCell.Neighbors.ContainsValue(neighbor))
        //         {
        //             activeCells.Remove(neighbor);
        //         }
        //     }
        // }
        
        

        // UpdateActiveCells();
        
    }

    
}
