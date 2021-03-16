using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameGenerator : MonoBehaviour
{
    public WorldSettings worldSettings;
    public RawImage obsDebugImage;

    // floor and Walls
    Transform m_Plane;
    Transform m_Sn;
    Transform m_Ss;
    Transform m_Se;
    Transform m_Sw;

    // top down camera of the area
    Camera m_TopDown;

    private GameObject[,] m_Grid;

    private List<GameObject> m_Keys = new List<GameObject>();
    private List<GameObject> m_Locks = new List<GameObject>();
    private List<Vector2> distractorPositions; 

    public GameObject agentObj;
    private GameObject m_Inventory;


    // CONSTANTS
    private const int maxPlacementTries = 200;
    private const int maxGenerationTries = 200;



    
    // Start is called before the first frame update
    void Start()
    {
        // get the corresponding cam for this area
        m_TopDown = transform.FindDeepChild("TopDownCamera").GetComponent<Camera>();
        
        // find the walls and the floor belonging to this area
        m_Plane = transform.FindDeepChild("Plane");
        m_Sn = transform.FindDeepChild("sN");
        m_Ss = transform.FindDeepChild("sS");
        m_Sw = transform.FindDeepChild("sW");
        m_Se = transform.FindDeepChild("sE");

        SetupBoxWorld();

        MakeGame(
            worldSettings.gridSize,
            worldSettings.solutionLength,
            worldSettings.numForward,
            worldSettings.numBackward,
            worldSettings.branchLength);

    }

    GameObject CreateObject(GameObject prefab, Color color, Vector3 localPosition)
    {
        GameObject newObj = Instantiate(prefab, 
            transform.TransformPoint(localPosition), 

            Quaternion.identity, 
            transform);
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        materialPropertyBlock.SetColor("_Color", color);
        newObj.GetComponent<Renderer>().SetPropertyBlock(materialPropertyBlock);
        return newObj;
    }

    void SetupBoxWorld()
    {
        int gridSize = worldSettings.gridSize;

        // scale floor plane
        m_Plane.transform.localScale = new Vector3(
            gridSize / 10.0f, 
            1f, 
            gridSize / 10.0f);
        m_Plane.transform.localPosition = new Vector3(
            (gridSize - 1) / 2f, 
            0.0f, 
            (gridSize - 1) / 2f);

        // adjust north wall
        m_Sn.transform.localScale = new Vector3(1, 0.5f, gridSize + 2);
        m_Sn.transform.localPosition = new Vector3(
            (gridSize - 1) / 2f,
            0.25f, 
            gridSize);

        // adjust south wall
        m_Ss.transform.localScale = new Vector3(1, 0.5f, gridSize + 2);
        m_Ss.transform.localPosition = new Vector3(
            (gridSize - 1) / 2f, 
            0.25f, 
            -1);

        // adjust east wall
        m_Se.transform.localScale = new Vector3(1, 0.5f, gridSize + 2);
        m_Se.transform.localPosition = new Vector3(
            gridSize, 
            0.25f, 
            (gridSize - 1) / 2f);

        // adjust west wall
        m_Sw.transform.localScale = new Vector3(1, 0.5f, gridSize + 2);
        m_Sw.transform.localPosition = new Vector3(-1, 0.25f, (gridSize - 1) / 2f);
        
        // position camera
        m_TopDown.orthographicSize = (gridSize) / 2 + 1;
        m_TopDown.transform.localPosition = new Vector3(
            (gridSize - 1) / 2f, 
            12f, 
            (gridSize - 1) / 2f);
    }
    
    int SampleKeysLocksLong( 
        int[] solutionLengthRange, 
        int[] numForwardRange, 
        int[] numBackwardRange,
        out int[,] locksKeysArray, 
        int branchLength = 1)
    {
        int idx = Random.Range(0, solutionLengthRange.Length);
        int solutionLength = Mathf.Min(solutionLengthRange[idx], worldSettings.colors.Length - 1);

        idx = Random.Range(0, numForwardRange.Length);
        int numForward = numForwardRange[idx];

        idx = Random.Range(0, numBackwardRange.Length);
        int numBackward = numBackwardRange[idx];

        List<int> locks = Enumerable.Range(0, solutionLength + 1).ToList();
        List<int> keys = Enumerable.Range(1, solutionLength).ToList();
        keys.Add(-1);


        // Forward distractors
        for(int i = 0; i < numForward; i++)
        {
            int lockI = Random.Range(1, solutionLength + 1);
            for(int j = 0; j < branchLength; j++)
            {
                int keyI = -2;
                while (keyI == -2 || keyI == lockI)
                {
                    keyI = Random.Range(solutionLength + 1, worldSettings.MaxNumKeys);
                }
                locks.Add(lockI);
                keys.Add(keyI);
                lockI = keyI;
            }
        }

        // Backward distractors. Branch length is not implemented here.
        for(int i = 0; i < numBackward; i++)
        {
            int keyI = Random.Range(1, solutionLength + 1);
            int lockI = Random.Range(solutionLength + 1, worldSettings.MaxNumKeys);
            
            locks.Add(lockI);
            keys.Add(keyI);
        }

        int[,] locks_keys = new int[locks.Count, 2];
        for (int i = 0; i < locks.Count; i++)
        {
            locks_keys[i, 0] = locks[i];
            locks_keys[i, 1] = keys[i]; 
        }
        locksKeysArray = locks_keys;
        return solutionLength;
    }

    bool CheckSpacing(GameObject[,] grid, int x, int y)
    {
        bool spaceForKey = true;
        for (int i = Mathf.Max(y - 1, 0); i < y + 2 && i < grid.GetLength(0); i++)
        {
            for (int j = Mathf.Max(x - 1, 0); j < x + 2 && j < grid.GetLength(1); j++)
            {
                spaceForKey &= grid[i, j] == null;
            }
        }
        bool alsoSpaceForBox = true;
        for (int i = Mathf.Max(y - 1, 0); i < y + 2 && i < grid.GetLength(0); i++)
        {
            alsoSpaceForBox &= grid[i, x + 2] == null;
        }

        return spaceForKey && alsoSpaceForBox;
    }

    void ClearGrid()
    {
        if (m_Grid != null)
        {
            foreach(GameObject obj in m_Grid)
            {
                DestroyImmediate(obj);
            }
        }
    }
    bool GenerateRandomLevel(
        int gridSize, 
        int[] solutionLengthRange, 
        int[] numForwardRange, 
        int[] numBackwardRange, 
        int branchLength)
    {
        // sample new problem
        int[,] locksKeys; 
        int solutionLength = SampleKeysLocksLong(solutionLengthRange, 
                                                numForwardRange, 
                                                numBackwardRange, 
                                                out locksKeys, 
                                                branchLength);
        int[] colorPoolIdxs = Enumerable.Range(0, worldSettings.colors.Length).ToArray();
        Shuffle(colorPoolIdxs);

        // Clear old lists
        ClearGrid();
        m_Keys.Clear();
        m_Locks.Clear();

        m_Grid = new GameObject[gridSize, gridSize];

        distractorPositions = new List<Vector2>();
        int placementTries = 0;
        bool placed = false;

        // Place items necessary for the sampled problem
        for(int i = 0; i < locksKeys.GetLength(0); i++)
        {
            int l = locksKeys[i, 0];
            int k = locksKeys[i, 1];
            bool isDistractor = false;
            if (i > solutionLength)
            {
               isDistractor = true; 
            }
            placed = false;
            while (!placed)
            {
                if (placementTries > maxPlacementTries)
                {
                    return false;
                }
                int x = Random.Range(0, gridSize - 2);
                int y = Random.Range(1, gridSize);
                if (CheckSpacing(m_Grid, x, y))
                { 
                    placed = true;
                    // check if box contains gem
                    if (k == -1)
                    {
                        GameObject gem = CreateObject(
                            worldSettings.gemPrefab,
                            worldSettings.gemColor,
                            new Vector3(x, 0, y));
                        m_Grid[y, x] = gem;
                        gem.tag = "locked";
                    }
                    else
                    {
                        GameObject key = CreateObject(
                            worldSettings.keyPrefab,
                            worldSettings.colors[colorPoolIdxs[k-1]],
                            new Vector3(x, 0, y));
                        m_Grid[y, x] = key;
                        if (k == 1)
                        {
                            key.tag = "unlocked";
                        }
                        else
                        {
                            key.tag = "locked";
                        }
                        key.GetComponent<BoxData>().id = k - 1;
                        m_Keys.Add(key);
                    }
                    // check if box has a lock
                    if (l != 0)
                    {
                        GameObject lockObj = CreateObject(
                            worldSettings.lockPrefab,
                            worldSettings.colors[colorPoolIdxs[l-1]],
                            new Vector3(x + 1, 0, y));
                        m_Grid[y, x + 1] = lockObj;
                        lockObj.tag = "locked";
                        lockObj.GetComponent<BoxData>().id = l - 1;
                        m_Locks.Add(lockObj);
                        if (isDistractor)
                        {
                            distractorPositions.Add(new Vector2( x + 1, y));
                        }
                    }
                }
                else
                {
                    placementTries++;
                }
            }
        }

        // Place player
        placed = false;
        while(!placed)
        {
            if (placementTries > maxPlacementTries)
            {
                return false;
            }
            int x = Random.Range(0, gridSize);
            int y = Random.Range(1, gridSize);
            if (m_Grid[y, x] == null)
            {
                if (agentObj == null)
                {
                    agentObj = CreateObject(
                        worldSettings.agentPrefab,
                        worldSettings.agentColor,
                        new Vector3(x, 0, y));
                }
                else 
                {
                    agentObj.transform.localPosition = new Vector3(x, 0, y);
                }
                placed = true;
            }
            else
            {
                placementTries++;
            }
        }
        return placed;
    }

    public bool MakeGame(
        int gridSize, 
        int[] solutionLengthRange, 
        int[] numForwardRange, 
        int[] numBackwardRange, 
        int branchLength)
    {
        // Create new BoxWorld Game level
        bool game = false;
        int tries = 0;
        while (tries < maxGenerationTries && !game)
        {
            game = GenerateRandomLevel(
                gridSize,
                solutionLengthRange,
                numForwardRange,
                numBackwardRange,
                branchLength);
            tries++;
        }   

        if (!game)
        {
            Debug.LogError("Could not generate game in MAX_GENERATION_TRIES tries.");
        }
        return game;
    }

    public void ResetGame()
    {
        if (m_Inventory != null)
        {
            DestroyImmediate(m_Inventory);
        } 

        MakeGame(
            worldSettings.gridSize,
            worldSettings.solutionLength,
            worldSettings.numForward,
            worldSettings.numBackward,
            worldSettings.branchLength);
    }

    /// <summary>
    /// Unlocks all items that have the same id as the key.
    /// </summary>
    /// <param name="key">GameObject with <see cref="BoxData"/> component.
    /// </param>
    public void UnlockItems(GameObject key)
    {
        BoxData keyData;
        if (key.TryGetComponent<BoxData>(out keyData))
        {
            foreach(GameObject lockObj in m_Locks)
            {
                if (lockObj.GetComponent<BoxData>().id == keyData.id)
                {
                    lockObj.tag = "unlocked";
                }
            }
        }
    }

    public void UpdateOnAgentStep()
    {
        Vector3 agentPos = agentObj.transform.localPosition;
        int agentPosX = Mathf.RoundToInt(agentPos.x);
        int agentPosY = Mathf.RoundToInt(agentPos.z);
        GameObject currentObj = m_Grid[agentPosY, agentPosX];
        if (currentObj != null)
        {
            BoxData boxData = currentObj.GetComponent<BoxData>();
            if (boxData.isLock)
            {
                // destroy lock
                m_Grid[agentPosY, agentPosX] = null;
                m_Locks.Remove(currentObj);
                Destroy(currentObj);
                
                // Unlock item next to it
                m_Grid[agentPosY, agentPosX - 1].tag = "unlocked";
                
                // lock all other locks
                foreach(GameObject lockObj in m_Locks)
                {
                    lockObj.tag = "locked";
                }

                // clean up inventory as key was used
                if (m_Inventory != null)
                {
                    Destroy(m_Inventory);
                    m_Inventory = null;
                }
            }
            else if (boxData.isKey)
            {
                // unlock items matching the key
                UnlockItems(currentObj);
                m_Keys.Remove(currentObj);
                // put new key to inventory
                if (m_Inventory != null)
                    Destroy(m_Inventory);
                m_Inventory = currentObj;
                currentObj.transform.localPosition = new Vector3(-1, 0, worldSettings.gridSize);
                m_Grid[agentPosY, agentPosX] = null;
            }
        }
    }

    public bool PositionIsDistractor(int x, int y)
    {
        if (distractorPositions.Contains(new Vector2(x, y)))
        {
            Debug.Log("is distractor");
            return true;
        }
        return false;
    }

    public bool PositionIsKey(int x, int y)
    {
        
        GameObject currentObj = m_Grid[y, x];
        if (currentObj != null)
        {
            BoxData boxData = currentObj.GetComponent<BoxData>();
            return boxData.isKey;
        }
        return false;
    }

    public bool PositionIsGem(int x, int y)
    {
        GameObject currentObj = m_Grid[y, x];
        if (currentObj != null)
        {
            BoxData boxData = currentObj.GetComponent<BoxData>();
            return boxData.isGem;
        }
        return false;
    }

    /// <summary>
    /// Shuffles an Array randomly.
    /// /// <summary>
    /// Based on Code from Matt Howells on StackOverflow
    /// https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
    /// </summary>
    /// <param name="array">Array to be shuffled.</param>
    public static void Shuffle<T> (T[] array)
    {
        int n = array.Length;
        while (n > 1) 
        {
            int k = Random.Range(0, n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }


}

