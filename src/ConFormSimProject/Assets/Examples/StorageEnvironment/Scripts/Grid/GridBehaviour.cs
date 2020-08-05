using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridBehaviour : MonoBehaviour
{
    public bool findDistance = false;
    public int rows = 10;
    public int columns = 10;
    public int scale = 1;
    public GameObject gridPrefab;
    public Vector3 leftBottomLocation = new Vector3(0, 0, 0);

    public GameObject[,] gridArray;
    public int startX = 0;
    public int startY = 0;
    public int endX = 2;
    public int endY = 2;
    public List<GameObject> path = new List<GameObject>();

    private bool m_updating = false;
    private StorageAcademy m_Academy;


    // Start is called before the first frame update
    void Awake()
    {
        m_Academy = GameObject.Find("Academy").GetComponent<StorageAcademy>();
        
        gridArray = new GameObject[columns, rows];
        if(gridPrefab)
        {
            GenerateGrid();
        }
        else
        {
            Debug.LogError("Missing gridprefab, please assign.");
        }
    }

    public void UpdateGrid()
    {
        m_updating = true;
        foreach (GameObject obj in gridArray)
        {
            Destroy(obj);
        }
        gridArray = new GameObject[columns, rows];
        GenerateGrid();
        m_updating = false;
    }

    public Vector3 GridToWorldCoordinates(int x, int y)
    {
        return new Vector3(
            leftBottomLocation.x + scale * x,
            leftBottomLocation.y,
            leftBottomLocation.z + scale * y);
    }

    public Vector3 WorldToGridCoordinates(Vector3 worldCoord)
    {
        while(m_updating){}
        return new Vector3(
            Mathf.Round((worldCoord.x - leftBottomLocation.x) / scale),
            worldCoord.y,
            Mathf.Round((worldCoord.z - leftBottomLocation.z) / scale));    
    }

    public Vector3 WorldToClostestGridCoordinates(Vector3 worldCoord)
    {
        while(m_updating){}
        Vector3 pos = new Vector3(
            Mathf.Round((worldCoord.x - leftBottomLocation.x) / scale),
            worldCoord.y,
            Mathf.Round((worldCoord.z - leftBottomLocation.z) / scale));

        if(gridArray[(int) pos.x, (int) pos.z])
        {
            return pos;
        }
        else
        {
            int xs = (int) pos.x;
            int zs = (int) pos.z;
            
            int maxDistance = 2;
            for (int d = 1; d < maxDistance; d++)
            {
                for (int i = 0; i < d + 1; i++)
                {
                    int x1 = xs - d + i;
                    int z1 = zs - i;

                    // Check point (x1, y1)
                    if(x1 > -1 && z1 > -1 && x1 < columns && z1 < rows && gridArray[x1, z1])
                    {
                        return new Vector3(x1, worldCoord.y, z1);
                    }

                    int x2 = xs + d - i;
                    int z2 = zs + i;

                    // Check point (x2, y2)
                    if(x2 > -1 && z2 > -1 && x2 < columns && z2 < rows && gridArray[x2, z2])
                    {
                        return new Vector3(x2, worldCoord.y, z2);
                    }
                }


                for (int i = 1; i < d; i++)
                {
                    int x1 = xs - i;
                    int z1 = zs + d - i;

                    // Check point (x1, y1)
                    if(x1 > -1 && z1 > -1 && x1 < columns && z1 < rows && gridArray[x1, z1])
                    {
                        return new Vector3(x1, worldCoord.y, z1);
                    }

                    int x2 = xs + i;
                    int z2 = zs - d + i;

                    // Check point (x2, y2)
                    if(x2 > -1 && z2 > -1 && x2 < columns && z2 < rows && gridArray[x2, z2])
                    {
                        return new Vector3(x2, worldCoord.y, z2);
                    }
                }
            }
        }
        return Vector3.zero;    
    }

    void GenerateGrid()
    {
        for(int i = 0; i < columns; i++)
        {
            for(int j = 0; j < rows; j++)
            {
                GameObject obj;
                Vector3 pos = new Vector3(
                    leftBottomLocation.x + scale * i, 
                    leftBottomLocation.y,
                    leftBottomLocation.z + scale * j);
                if (Utility.OccupiedPosition(pos , Quaternion.identity, 
                    new Vector3 (0.45f, 0.45f, 0.45f), m_Academy.interactableTags.ToList(), 
                    QueryTriggerInteraction.UseGlobal).Length > 0)
                {
                    obj = null;   
                }
                else
                {
                    obj = Instantiate(gridPrefab, pos, Quaternion.identity);
                    obj.transform.SetParent(gameObject.transform);
                    obj.GetComponent<GridStat>().x = i;
                    obj.GetComponent<GridStat>().y = j; 
                }
                gridArray[i, j] = obj;
            }
        }
    }
}
