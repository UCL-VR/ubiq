using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates: ICellCoordinates
{

    [SerializeField]
    private int x,z;

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
            return -X - Z;
        }
    }

	public int Z 
    { 
        get
        {
            return z;
        }
    }

	public HexCoordinates (int x, int z) 
    {
		this.x = x;
		this.z = z;
	}

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
		return new HexCoordinates(x - z / 2, z);
	}

    public override string ToString() 
    {
		return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public string ToStringOnSeparateLines() 
    {
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}

    public List<HexCoordinates> GetNeighborCoordinates()
    {
        List<HexCoordinates> neighbrorCoords = new List<HexCoordinates>();

        neighbrorCoords.Add(new HexCoordinates(this.X, this.Z+1));
        neighbrorCoords.Add(new HexCoordinates(this.X+1, this.Z));
        neighbrorCoords.Add(new HexCoordinates(this.X+1, this.Z-1));
        neighbrorCoords.Add(new HexCoordinates(this.X, this.Z-1));
        neighbrorCoords.Add( new HexCoordinates(this.X-1, this.Z));
        neighbrorCoords.Add(new HexCoordinates(this.X-1, this.Z+1));


        return neighbrorCoords;

    }
}

public class HexCell : Cell
{
    public Vector3 gameObjectPosition;
    private HexMesh hexMesh;
    private HexGrid grid
    {
        get
        {
            return (HexGrid) Grid;
        }
    }

    public CellBorderPoint cellBorderPointPrefab;

    override protected void Awake()
    {
        SetupCell();

        hexMesh = GetComponentInChildren<HexMesh>();
        // cellCanvas = GetComponentInChildren<Canvas>();   
        if(Grid != null && !(Grid is HexGrid))
        {
            Coordinates = new CellCoordinates((int) transform.position.x, (int) transform.position.y, (int) transform.position.z);
            name = "Hex Cell " + Coordinates.ToString();
        }
        gameObjectPosition = transform.position;
        
    }

    // Start is called before the first frame update
    override protected void Start()
    {
        // base.Start();
        
        // CellUUID = HexCell.GetCellUUID(this);
        // Debug.Log("Hex Cell Start: " + name + " uuid: " + CellUUID);
       
        if(Grid != null && Grid is HexGrid)
        {
            hexMesh.Triangulate(this, grid);
            // Debug.Log("Hex Cell Start: " + name + " cell dictionary count: " + grid.cellDictionary.Count);
            foreach (var item in ((HexCoordinates) Coordinates).GetNeighborCoordinates())
            {
                string key = Cell.GetCellUUID("Hex Cell " + item.ToString(), item);
                
                if(grid.Cells.ContainsKey(key))
                {
                    neighbors[key] = (HexCell) grid.Cells[key];
                }
                
            }

            int idx = 1;
            foreach (var trigger in GetComponentsInChildren<CellTrigger>())
            {
                trigger.transform.localPosition = new Vector3(0, 2.5f, 0);
                trigger.transform.localScale = new Vector3(grid.outerRadius, 5.0f, grid.innerRadius * 2 - 0.5f);
                trigger.name = this.name + " Trigger " + idx;
                trigger.SetupTrigger();
                CreateBorderPoints(trigger);
                trigger.OnTriggerEntered.AddListener(CellTriggerEntered);
                trigger.OnTriggerExited.AddListener(CellTriggerExited);
                idx++;
            } 
            if(cellCanvas != null)
            {
                cellLabel.text = ((HexCoordinates) Coordinates).ToStringOnSeparateLines();
            }
        }
        else
        {
            SetVisible(false);
            foreach (Cell cell in NeighborCellObjects)
            {
                neighbors[cell.CellUUID] = cell;
            }

            foreach (var trigger in GetComponentsInChildren<CellTrigger>())
            {
                trigger.SetupTrigger();
                trigger.OnTriggerEntered.AddListener(CellTriggerEntered);
                trigger.OnTriggerExited.AddListener(CellTriggerExited);

                if(BorderPointObjects.Count == 0 && cellBorderPointPrefab != null)
                {
                    CreateBorderPoints(trigger);
                }
            }

            if(cellCanvas != null)
            {
                cellLabel.text = ((CellCoordinates) Coordinates).ToStringOnSeparateLines();
            }
        }
    }

    void CreateBorderPoints(CellTrigger trigger)
    {
        Vector3[] borderPoints = trigger.GetBorderPoints();
        foreach (var item in borderPoints)
        {
            var distanceToNeighborCell = float.MaxValue;
            
            foreach (var neighborCell in neighbors)
            {
                var dist = Vector3.Distance(((HexCell) neighborCell.Value).gameObject.transform.position, item);
                if(dist < distanceToNeighborCell)
                {
                    CellBorderPoint cellBorderPoint;
                    if(BorderPoints.ContainsKey(neighborCell.Key) && BorderPoints[neighborCell.Key].distanceToCell < dist)
                    {
                        continue;
                    }
                    if(BorderPoints.ContainsKey(neighborCell.Key))
                    {
                        cellBorderPoint = BorderPoints[neighborCell.Key];
                    }
                    else
                    {
                        cellBorderPoint = GameObject.Instantiate<CellBorderPoint>(cellBorderPointPrefab);
                        cellBorderPoint.enabled = false;
                        cellBorderPoint.fromCell = this;
                        cellBorderPoint.OnBorderTriggerEntered.AddListener(OnBorderEnter);
                        cellBorderPoint.OnBorderTriggerExited.AddListener(OnBorderExit);
                        cellBorderPoint.gameObject.AddComponent<BoxCollider>();
                    }
                    
                    cellBorderPoint.transform.parent = transform;
                    
                    BoxCollider collider = cellBorderPoint.gameObject.GetComponent<BoxCollider>();
                    collider.enabled = false;
                    Vector3 colliderSize = trigger.gameObject.GetComponent<BoxCollider>().size;
                    colliderSize.x *= trigger.gameObject.transform.localScale.x * 1.0f;
                    colliderSize.y *= trigger.gameObject.transform.localScale.y;
                    colliderSize.z *= (trigger.gameObject.transform.localScale.z / 5);
                    collider.size = colliderSize;
                    cellBorderPoint.transform.position = Vector3.MoveTowards(item, transform.position, colliderSize.z/2);
                    cellBorderPoint.transform.position = new Vector3(cellBorderPoint.transform.position.x, 
                                                                    trigger.transform.position.y, 
                                                                    cellBorderPoint.transform.position.z);
                    collider.transform.rotation = trigger.transform.rotation;

                    cellBorderPoint.toCell = (Cell)neighborCell.Value;
                    cellBorderPoint.name = "Border To Cell " + cellBorderPoint.toCell.Name;
                    cellBorderPoint.distanceToCell = dist;
                    distanceToNeighborCell = dist;
                    BorderPoints[neighborCell.Key] = cellBorderPoint;
                    SetBorderPointsActive(false);
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        hexMesh.GetComponent<MeshRenderer>().enabled = visible;
    }
}
