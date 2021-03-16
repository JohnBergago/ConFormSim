using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using ConFormSim.ObjectProperties;

public class StorageArea : MonoBehaviour
{
    public GameObject agent;

    // position generation
    public GridBehaviour m_PathGrid;
    private const float k_YOffset = 0.75f;

    StorageAcademy m_Academy;

    // top down camera of the area
    Camera m_TopDown;

    // floor and Walls
    Transform m_Plane;
    Transform m_Sn;
    Transform m_Ss;
    Transform m_Se;
    Transform m_Sw;

    // these variables store the random generated values for the grid sizes
    public int m_GridSizeX;
    public int m_GridSizeZ;

    private List<GameObject> m_objects;
    private int m_NumItemTypes;
    private List<GameObject> m_baseAreas;
    private int m_NumBaseTypes;
    private List<GameObject> m_baseAreaObjs;

    void Awake()
    {
        // find the academy to retrieve area size settings
        m_Academy = FindObjectOfType(typeof(StorageAcademy)) as StorageAcademy;

        // get the corresponding cam for this area
        m_TopDown = transform.FindDeepChild("TopDownCamera").GetComponent<Camera>();

        // find the walls and the floor belonging to this area
        m_Plane = transform.FindDeepChild("Plane");
        m_Sn = transform.FindDeepChild("sN");
        m_Ss = transform.FindDeepChild("sS");
        m_Sw = transform.FindDeepChild("sW");
        m_Se = transform.FindDeepChild("sE");

        // get the corresponding grid for this area
        m_PathGrid = transform.FindDeepChild("PathGrid").GetComponent<GridBehaviour>();

        m_objects = new List<GameObject>();
        m_baseAreas = new List<GameObject>();
        m_baseAreaObjs = new List<GameObject>();

    }
    /// <summary>
    /// Each environment will be created based on the values set in the academy.
    /// The grid size is generated randomly for every area.
    /// </summary>
    public void SetEnvironment()
    {
        var resetProperties = m_Academy.envPropertiesChannel;
        // generate grid size values
        m_GridSizeX = Random.Range(
            (int)resetProperties.GetWithDefault(
                "minGridSizeX", 
                m_Academy.areaSettings.gridSizeXMin), 
            (int)resetProperties.GetWithDefault(
                "maxGridSizeX", 
                m_Academy.areaSettings.gridSizeX));
        m_GridSizeZ = Random.Range(
            (int)resetProperties.GetWithDefault(
                "minGridSizeY", 
                m_Academy.areaSettings.gridSizeZMin), 
            (int)resetProperties.GetWithDefault(
                "maxGridSizeY", 
                m_Academy.areaSettings.gridSizeZ));
        
        m_NumItemTypes = m_Academy.itemSettings.itemTypesToUse;
        
        // initialize path grid
        m_PathGrid.columns = m_GridSizeX;
        m_PathGrid.rows = m_GridSizeZ;
        m_PathGrid.leftBottomLocation = transform.position;
        m_PathGrid.UpdateGrid();

        // scale floor plane
        m_Plane.transform.localScale = new Vector3(
            m_GridSizeX / 10.0f, 
            1f, 
            m_GridSizeZ / 10.0f);
        m_Plane.transform.localPosition = new Vector3(
            (m_GridSizeX - 1) / 2f, 
            0.0f, 
            (m_GridSizeZ - 1) / 2f);

        // adjust north wall
        m_Sn.transform.localScale = new Vector3(1, 4, m_GridSizeX + 2);
        m_Sn.transform.localPosition = new Vector3(
            (m_GridSizeX - 1) / 2f,
            2.0f, 
            m_GridSizeZ);

        // adjust south wall
        m_Ss.transform.localScale = new Vector3(1, 4, m_GridSizeX + 2);
        m_Ss.transform.localPosition = new Vector3(
            (m_GridSizeX - 1) / 2f, 
            2.0f, 
            -1);

        // adjust east wall
        m_Se.transform.localScale = new Vector3(1, 4, m_GridSizeZ + 2);
        m_Se.transform.localPosition = new Vector3(
            m_GridSizeX, 
            2.0f, 
            (m_GridSizeZ - 1) / 2f);

        // adjust west wall
        m_Sw.transform.localScale = new Vector3(1, 4, m_GridSizeZ + 2);
        m_Sw.transform.localPosition = new Vector3(-1, 2.0f, (m_GridSizeZ - 1) / 2f);

        int maxGridSize = Mathf.Max(m_GridSizeX, m_GridSizeZ);
        m_TopDown.orthographicSize = Mathf.Max(m_GridSizeX, m_GridSizeZ) / 2 + 1;
        m_TopDown.transform.localPosition = new Vector3(
            (maxGridSize - 1) / 2f, 
            12f, 
            (maxGridSize - 1) / 2f);

        
        // Set wall object Property
        List<GameObject> walls = new List<GameObject>(){m_Sn.gameObject, m_Se.gameObject, m_Ss.gameObject, m_Sw.gameObject};
        foreach(GameObject wall in walls)
        {
            ObjectPropertyProvider opp = wall.GetComponent<ObjectPropertyProvider>();
            opp.AvailableProperties = m_Academy.featureVectorDefinition;
            BoolObjectProperty isWallProp = ScriptableObject.CreateInstance<BoolObjectProperty>();
            isWallProp.value = true;
            opp.SetObjectProperty("isWall", isWallProp);

            // set color property
            ColorObjectProperty colorProp = ScriptableObject.CreateInstance<ColorObjectProperty>();
            colorProp.useObjectRenderer = true;
            colorProp.instanceNoise = 0.00f;
            opp.SetObjectProperty("color", colorProp);
        }

        // set color prop for plane
        ObjectPropertyProvider oppPlane = m_Plane.gameObject.GetComponent<ObjectPropertyProvider>();
        oppPlane.AvailableProperties = m_Academy.featureVectorDefinition;
        ColorObjectProperty colorPropPlane = ScriptableObject.CreateInstance<ColorObjectProperty>();
        colorPropPlane.useObjectRenderer = true;
        colorPropPlane.instanceNoise = 0.00f;
        oppPlane.SetObjectProperty("color", colorPropPlane);
    }

    /// <summary>
    /// Adds the transform of the given game object as a child to the transform of
    /// the area.
    /// </summary>
    /// <param name="obj"> GameObject whose transform will be set as a child of the
    /// area transform </param>
    private void AddObjectToAreaTransform(GameObject obj)
    {
        if (obj != null)
        {
            // Set item to be a child of the area transform
            obj.transform.SetParent(transform);
        }
    }
    
    /// <summary>
    /// Spawns a minimum number of each item in the scene/area.
    /// </summary>
    /// <param name="minNoPerItem"> Minimum number of objects to spawn from each item
    /// <param name="objectIds"> List of Ids, which provides the spawn order of the
    /// object types
    /// type. </param> 
    public void SpawnMinItems(int minNoPerItem, List<int> objectIds)
    {
        
        for (int i = 0; i < m_Academy.itemSettings.itemTypesToUse; i++)
        {
            for (int j = 0; j < minNoPerItem; j++)
            {
                GameObject spawned = SpawnObject(
                    m_Academy.itemSettings.interactableObjects[objectIds[i]]);

                m_objects.Add(spawned);
                // Set spawned item to be a child of the area transform
                AddObjectToAreaTransform(spawned);
            }
        }
    }

    /// <summary>
    /// Spawns a number of items. These items have to be defined in the academy.
    /// The spawn positions are random in the area.
    /// </summary>
    /// <param name="numObjects"> Number of items that should spawn. </param> 
    public void SpawnItems(int numItems)
    {
        // get random spawn order for object types 
        List<int> objectIds = new List<int>(
            Enumerable.Range(0, m_Academy.itemSettings.interactableObjects.Count));
        objectIds.Shuffle();
        // discard all objectIds we cannot use due to itemTypesToUse
        objectIds.RemoveRange(
            m_Academy.itemSettings.itemTypesToUse, 
            objectIds.Count - m_Academy.itemSettings.itemTypesToUse);

        // First spawn all required items to fulfill the minimum requirement
        SpawnMinItems(m_Academy.itemSettings.numberPerItemTypeMin, objectIds);

        // calculate the number of items that's left to be spawned
        int rest = numItems 
                    - m_NumItemTypes 
                    * m_Academy.itemSettings.numberPerItemTypeMin;

        for (int i = 0; i < rest; i++)
        {
            int objId = Random.Range(
                0, 
                objectIds.Count);
            Debug.Log(objectIds.Count);
            GameObject spawned = SpawnObject(
                m_Academy.itemSettings.interactableObjects[objectIds[objId]]);
            m_objects.Add(spawned);

            // Set spawned item to be a child of the area transform
            AddObjectToAreaTransform(spawned);
        }
        m_PathGrid.UpdateGrid();
    }

    /// <summary>
    /// Spawns one baseArea. 
    /// </summary>
    /// <param name="baseTile"> Number of boards that should spawn. </param> 
    /// <param name="xSize"> Area size in x direction (in baseTiles). </param> 
    /// <param name="ySize"> Area size in y direction (in baseTiles). </param> 
    private void SpawnBaseTile(GameObject baseTile, int xSize, int zSize)
    {
        Vector3[] positions;
        if (m_Academy.areaSettings.baseInCornersOnly)
        {
            positions = GetPlaneCornerPositions(baseTile, xSize, zSize);
        }
        else
        {
            positions = GetPlanePositions(baseTile, xSize, zSize);
        }
        // if there are no valid positions don't do anything
        if (positions == null)
            return;
        
        // create a parent base object
        List<GameObject> baseArea = new List<GameObject>();
        Vector3 centerPos = new Vector3();
        foreach(Vector3 pos in positions)
        {
            centerPos += pos;
            GameObject spawned = InstantiateWithMaterialPropertyBlock(
                                    baseTile, 
                                    pos, 
                                    Quaternion.identity);
            spawned.SetActive(true);
            // Set spawned item to be a child of the area transform
            AddObjectToAreaTransform(spawned);
            baseArea.Add(spawned);
        }
        centerPos /= positions.Length;
        // add the new created areas to the global list of base area elements
        m_baseAreas.AddRange(baseArea);
        // create an empty parent base object
        GameObject baseAreaObj = new GameObject("Base");
        baseAreaObj.transform.position = centerPos;
        baseAreaObj.transform.SetParent(transform);
        m_baseAreaObjs.Add(baseAreaObj);

        foreach(GameObject ba in baseArea)
        {
            ba.transform.SetParent(baseAreaObj.transform);
        }

        // Create color for edges based on the original color
        MaterialPropertyBlock mpbEdge = new MaterialPropertyBlock();
        Renderer baseTileRenderer = baseTile.GetComponent<Renderer>();
        Color baseColor = Color.white;
        bool noColorProp = true;
        if (baseTileRenderer.HasPropertyBlock())
        {
            baseTileRenderer.GetPropertyBlock(mpbEdge);
            baseColor = mpbEdge.GetColor("_Color");
            if (!baseColor.Equals(new Color(0,0,0,0)))
            {
                noColorProp = false;
            }
        }
        if (noColorProp)
        {
            baseColor = baseTileRenderer.sharedMaterial.GetColor("_Color");
        }
        float H, S, V;
        Color.RGBToHSV(baseColor, out H, out S, out V);
        // if the base is transparent keep the original colors for the edges
        if (m_Academy.areaSettings.noBaseFillColor)
        {
            mpbEdge.SetColor(
                "_Color", 
                new Color(baseColor.r, baseColor.g, baseColor.b, 1.0f));
        }
        // if brighter bases set, keep original color on the edges
        // else if (m_Academy.areaSettings.brighterBases)
        // {
        //     edgeColor.color = baseTile.GetComponent<BaseController>().originalColor;
        // }
        else
        {
            mpbEdge.SetColor(
                "_Color", 
                Color.HSVToRGB(H, S * 1.5f, V / 2.0f));
        }
        
        // draw border lines
        float lineWidth = 0.1f;
        float lineLengthx = 0.25f;
        float lineLengthz = 0.25f;
        if (m_Academy.areaSettings.fullBaseLine)
        {
            lineLengthx = baseTile.transform.localScale.x * xSize / 2.0f;
            lineLengthz = baseTile.transform.localScale.z * zSize / 2.0f;
        }

        // for each corner
        for (int i = 0; i < 4; i++)
        {
            int signX = (i / 2) * 2 - 1;
            int signZ = (i % 2) * 2 - 1;

            // calculate corner positions
            Vector3 cornerPos = centerPos
                        + new Vector3(
                            signX * xSize / 2.0f * baseTile.transform.localScale.x,
                            baseTile.transform.position.y + 0.01f - centerPos.y,
                            signZ * zSize / 2.0f * baseTile.transform.localScale.z);
            cornerPos.y = baseTile.transform.position.y + 0.01f;

            // calculate x position for each edge
            Vector3 posX = cornerPos
                    - signZ * lineWidth / 2.0f * Vector3.forward
                    - signX * (lineWidth + lineLengthx / 2.0f) * Vector3.right
                    + signZ * 0.01f * Vector3.forward + 0.01f * signX * Vector3.right; 
    
            // create cubes of which the elements will be made of
            GameObject cornerObjX = Instantiate(
                    baseTile.GetComponent<BaseController>().edgePrimitivePrefab,
                    posX,
                    Quaternion.identity,
                    baseAreaObj.transform); 
            cornerObjX.name = "corner " + i + "_x";
            // scale the x cube
            cornerObjX.transform.localScale = new Vector3(
                    lineLengthx,
                    baseTile.transform.localScale.y,
                    lineWidth);

            // repeat for the z-edge object
            Vector3 posZ = cornerPos
                    - signZ * (lineWidth + lineLengthz) / 2.0f * Vector3.forward
                    - signX * lineWidth / 2.0f * Vector3.right
                    + signZ * 0.01f * Vector3.forward + 0.01f * signX * Vector3.right; 
            GameObject cornerObjZ = Instantiate(
                    baseTile.GetComponent<BaseController>().edgePrimitivePrefab,
                    posZ,
                    Quaternion.identity,
                    baseAreaObj.transform); 
            cornerObjZ.name = "corner " + i + "_z";
            // scale the z cube
            cornerObjZ.transform.localScale = new Vector3(
                    lineWidth,
                    baseTile.transform.localScale.y,
                    lineLengthz + lineWidth);

            cornerObjX.GetComponent<Renderer>().SetPropertyBlock(mpbEdge);
            cornerObjZ.GetComponent<Renderer>().SetPropertyBlock(mpbEdge);
        }
    }


    /// <summary>
    /// Spawns a number of bases. 
    /// </summary>
    /// <param name="numBaseAreas"> Number of boards that should spawn. </param> 
    public void SpawnBaseAreas(int numBaseAreas)
    {
        // min size is 1
        int minX = 0;
        if (m_Academy.areaSettings.baseSizeXMin > 0)
            minX = m_Academy.areaSettings.baseSizeXMin;
        
        int maxX = m_GridSizeX;
        if (m_Academy.areaSettings.baseSizeX < m_GridSizeX)
             maxX = m_Academy.areaSettings.baseSizeX;
        
        int minZ = 0;
        if (m_Academy.areaSettings.baseSizeZMin > 0)
            minZ = m_Academy.areaSettings.baseSizeZMin;
        
        int maxZ = m_GridSizeZ;
        if (m_Academy.areaSettings.baseSizeZ < m_GridSizeZ)
            maxZ = m_Academy.areaSettings.baseSizeZ;

        // first spawn the min base number per each type 
        for(int i = 0; i < m_Academy.areaSettings.baseObjects.Count; i++)
        {
            int countBaseParts = 0;
            while(m_Academy.areaSettings.numberPerBaseTypeMin - countBaseParts > 0)
            {
                int xSize = Random.Range(minX, maxX + 1);   
                int zSize = Random.Range(minZ, maxZ + 1);
                countBaseParts += xSize * zSize;
                SpawnBaseTile(m_Academy.areaSettings.baseObjects[i], xSize, zSize);
            }
        }
        numBaseAreas -= m_baseAreas.Count();
        while (numBaseAreas > 0)
        {
            int type = Random.Range(0, m_Academy.areaSettings.baseObjects.Count);
            GameObject basePrefab = m_Academy.areaSettings.baseObjects[type];
            int xSize = Random.Range(minX, maxX + 1);
            int zSize = Random.Range(minZ, maxZ + 1);
            numBaseAreas -= (xSize * zSize);
            SpawnBaseTile(basePrefab, xSize, zSize);
        }
    }

    /// <summary>
    /// Spawns an Object at a random position in this area. Therfore it checks if the
    /// position is already occupied by any other object.
    /// </summary>
    /// <param name="obj"> GameObject that will be spawned </param>
    /// <returns> The spawned GameObject.false </returns>
    private GameObject SpawnObject(GameObject obj)
    {
        // find a spawn position and rotation that is not already occluded
        Vector3 spawnPos;
        Bounds bounds;
        Quaternion spawnRot = new Quaternion();
        int attempts = 0;   // number of attempts to place the object
        do
        {
            bounds = Utility.GetBoundsOfAllDeepChilds(obj);
            spawnPos = GenerateSpawnPos(bounds.extents.y);
            spawnRot.eulerAngles = new Vector3(0, Random.Range(0, 4) * 90.0f, 0);
            attempts ++;
            if (attempts > 200)
            {
                Debug.LogError("Couldn't place object " + obj.name + ". No free space");
                return null;
            }
        } while (
            Utility.OccupiedPosition(
                spawnPos, 
                spawnRot, 
                bounds.extents, 
                agent.GetComponent<StorageAgent>().collisionTags,
                QueryTriggerInteraction.Collide).Length > 0 
            || (IntersectsBaseArea(spawnPos, bounds.extents.x * 2, bounds.extents.z * 2))
            );
        GameObject spawnedObj = InstantiateWithMaterialPropertyBlock(
                                    obj, 
                                    spawnPos, 
                                    spawnRot);
        spawnedObj.SetActive(true);
        return spawnedObj;
    }

    /// <summary>
    /// Generates a random position on the grid within the walls of the area. 
    /// </summary>
    /// <return> Randomly generated position on the grid </return>
    private Vector3 GenerateSpawnPos(float yOffset)
    {
        Vector3 gridPos = m_PathGrid.GridToWorldCoordinates(
            Random.Range(0, m_GridSizeX),
            Random.Range(0, m_GridSizeZ));

        Vector3 spawnPos = new Vector3(gridPos.x, yOffset, gridPos.z);

        return spawnPos;
    }

    /// <summary>
    /// Resets the area by deleting all existing items and and reinitializing the
    /// environment. That means it sets new plane sizes.
    /// </summary>
    public void ResetArea()
    {
        // delete all existing objects
        foreach (GameObject obj in m_objects)
        {
            if (obj)
            {
                obj.SetActive(false);
                Destroy(obj);
            }
        }

        m_objects.Clear();
        // delete all area tiles
        foreach (GameObject baseArea in m_baseAreas)
        {
            if (baseArea)
            {
                baseArea.SetActive(false);
                Destroy(baseArea);
            }
        }
        m_baseAreas.Clear();

        // delete all area objects
        foreach (GameObject baseAreaObj in m_baseAreaObjs)
        {
            if (baseAreaObj)
            {
                DestroyImmediate(baseAreaObj);
            }
        }
        m_baseAreaObjs.Clear();
        
        // Reinit the environment
        SetEnvironment();

        // create new bases
        int numBases = Random.Range(
            m_Academy.areaSettings.baseTypesToUse
            * m_Academy.areaSettings.numberPerBaseTypeMin,
            m_Academy.areaSettings.baseTypesToUse 
            * m_Academy.areaSettings.numberPerBaseType);
        SpawnBaseAreas(numBases);

        // create new items
        int numItems = Random.Range(
            m_NumItemTypes * m_Academy.itemSettings.numberPerItemTypeMin,
            m_NumItemTypes * m_Academy.itemSettings.numberPerItemType);
        SpawnItems(numItems);
    }

    /// <summary>
    /// Returns a list of position in a random corner for a plane made of cube objects.
    /// So that the planes size is xSize x zSize.
    /// </summary>
    /// <param name="cube"> Flat cube that should be fitted to a corner </param>
    /// <param name="xSize"> Size in x dimension for the plane </param>
    /// <param name="zSize"> Size in z dimension for the plane </param>
    /// <returns> List of positions for one of the four corners of the area </returns>
    private Vector3[] GetPlaneCornerPositions(GameObject cube, int xSize, int zSize)
    {
        float xOffset = xSize / 2.0f;
        float zOffset = zSize / 2.0f;

        int numCubesX = (int) (xSize / cube.transform.localScale.x);
        int numCubesZ = (int) (zSize / cube.transform.localScale.z);

        Vector3[] positions;
        int trials = 0;
        bool intersects;
        do{
            intersects = false;
            // choose a corner
            Vector3 centerPos;
            int randNum = Random.Range(0,4);
            int xc = randNum / 2;
            int zc = randNum % 2;
            centerPos = m_PathGrid.GridToWorldCoordinates(
                xc * (m_GridSizeX - 1),
                zc * (m_GridSizeZ - 1));
            int xSign = xc * 2 + -1;
            int zSign = zc * 2 + -1;
            centerPos.x -= xSign * xOffset;
            centerPos.y = cube.transform.position.y;
            centerPos.z -= zSign * zOffset;

            centerPos += new Vector3(xSign * 0.5f, 0, zSign * 0.5f);
            
            float cubeOffsetX = cube.transform.localScale.x / 2.0f;
            float cubeOffsetZ = cube.transform.localScale.z / 2.0f;

            positions = new Vector3[xSize * zSize];
            for(int i = 0; i < zSize; i++)
            {
                for(int j = 0; j < xSize; j++)
                {
                    positions[j + xSize * i] = new Vector3(
                        centerPos.x - numCubesX / 2.0f + cubeOffsetX + j * cube.transform.localScale.x,
                        centerPos.y,
                        centerPos.z - numCubesZ / 2.0f + cubeOffsetZ + i * cube.transform.localScale.z
                    );  
                }
            }

            foreach(Vector3 pos in positions)
            {
                if (IntersectsBaseArea(
                    pos, cube.transform.localScale.x, cube.transform.localScale.z))
                {
                    intersects = true;   
                }
            }
        }while(trials < 100 && intersects);
        if (intersects)
        {
            Debug.Log("Couldn't place base area, due to a lack of space.");
            return null;
        }

        return positions;
    }

    /// <summary>
    /// Returns a list of positions on a random spot for a plane made of cube objects.
    /// So that the planes size is xSize x zSize.
    /// </summary>
    /// <param name="cube"> Flat cube that should be fitted to a corner </param>
    /// <param name="xSize"> Size in x dimension for the plane </param>
    /// <param name="zSize"> Size in z dimension for the plane </param>
    /// <returns> List of positions. </returns>
    private Vector3[] GetPlanePositions(GameObject cube, int xSize, int zSize)
    {
        float xOffset = xSize / 2.0f;
        float zOffset = zSize / 2.0f;

        int numCubesX = (int) (xSize / cube.transform.localScale.x);
        int numCubesZ = (int) (zSize / cube.transform.localScale.z);
        Vector3[] positions;

        int trials = 0;
        bool intersects;
        do
        {
            intersects = false;

            Vector3 centerPos;
            centerPos = m_PathGrid.GridToWorldCoordinates(
                Random.Range((int) xOffset, (int) (m_GridSizeX - xOffset)),
                Random.Range((int) zOffset, (int) (m_GridSizeZ - zOffset)));
            if (xSize % 2 == 0)
            {
                centerPos += new Vector3(0.5f, 0, 0);
            }
            if (zSize % 2 == 0)
            {
                centerPos += new Vector3(0, 0, 0.5f);
            }
    
            float cubeOffsetX = cube.transform.localScale.x/2.0f;
            float cubeOffsetZ = cube.transform.localScale.z/2.0f;
            
            positions = new Vector3[xSize * zSize];
            
            for(int i = 0; i < zSize; i++)
            {
                for(int j = 0; j < xSize; j++)
                {
                    positions[j + xSize * i] = new Vector3(
                        centerPos.x - numCubesX / 2.0f + cubeOffsetX + j * cube.transform.localScale.x,
                        centerPos.y,
                        centerPos.z - numCubesZ / 2.0f + cubeOffsetZ + i * cube.transform.localScale.z
                    );  
                }
            }
            trials++;
            
            foreach(Vector3 pos in positions)
            {
                if (IntersectsBaseArea(
                    pos, cube.transform.localScale.x, cube.transform.localScale.z))
                {
                    intersects = true;   
                }
            }
        }while(trials < 100 && intersects);
        if (intersects)
        {
            Debug.Log("Couldn't place base area, due to a lack of space.");
            return null;
        }
        
        return positions;
    }

    /// <summary>
    /// Checks if the given position lies within the dimensions on the x and z axis
    /// of the given base area.
    /// </summary>
    /// <param name="baseArea"> Area that should be checked.</param>
    /// <param name="pos">Position to be checked whether its inside the dimensions of
    /// base area.</param>
    /// <returns> True if pos is in the base area.</returns>
    public bool IsInBaseArea(GameObject baseArea, Vector3 pos)
    {
        float areaHalfExtentX = baseArea.transform.localScale.x / 2;
        float areaHalfExtentZ = baseArea.transform.localScale.z / 2;
        if (pos.x > baseArea.transform.position.x - areaHalfExtentX &&
            pos.x < baseArea.transform.position.x + areaHalfExtentX &&
            pos.z > baseArea.transform.position.z - areaHalfExtentZ &&
            pos.z < baseArea.transform.position.z + areaHalfExtentZ)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the given object overlaps with any of the baseAreas in this Area.
    /// If it overlaps, the corresponding baseArea will be returned.
    /// </summary>
    /// <param name="obj"> Object to be checked, whether it overlaps an base area</param>
    /// <returns> The overlapped baseArea, or null if there is no overlap.</returns>
    public GameObject IntersectsBaseArea(GameObject obj)
    {
        float objectHalfExtentX = obj.transform.localScale.x / 2.0f;
        float objectHalfExtentZ = obj.transform.localScale.z / 2.0f;
        foreach(GameObject baseArea in m_baseAreas)
        {
            float areaHalfExtentX = baseArea.transform.localScale.x / 2.0f;
            float areaHalfExtentZ = baseArea.transform.localScale.z / 2.0f;
            
            // to calculate if the object overlaps with the basearea we need to left 
            // corner positions of the object and the area, as well as their 
            // size in x and y direction.
            float objX = obj.transform.position.x - objectHalfExtentX;
            float objZ = obj.transform.position.z - objectHalfExtentZ;
            float areaX = baseArea.transform.position.x - areaHalfExtentX;
            float areaZ = baseArea.transform.position.z - areaHalfExtentZ;

            if(AreasIntersect(
                objX, objZ, obj.transform.localScale.x, obj.transform.localScale.z,
                areaX, areaZ, baseArea.transform.localScale.x, baseArea.transform.localScale.z
            ))
            {
                return baseArea;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if the given object overlaps with any of the baseAreas in this Area.
    /// If it overlaps, the corresponding baseArea will be returned.
    /// </summary>
    /// <param name="objPos"> Object to be checked, whether it overlaps an base area</param>
    /// <returns> The overlapped baseArea, or null if there is no overlap.</returns>
    public GameObject IntersectsBaseArea(Vector3 objPos, float xSize, float zSize)
    {
        float objectHalfExtentX = xSize / 2.0f;
        float objectHalfExtentZ = zSize / 2.0f;
        foreach(GameObject baseArea in m_baseAreas)
        {
            float areaHalfExtentX = baseArea.transform.localScale.x / 2.0f;
            float areaHalfExtentZ = baseArea.transform.localScale.z / 2.0f;
            
            // to calculate if the object overlaps with the basearea we need the left 
            // corner positions of the object and the area, as well as their 
            // size in x and y direction.
            float objX = objPos.x - objectHalfExtentX;
            float objZ = objPos.z - objectHalfExtentZ;
            float areaX = baseArea.transform.position.x - areaHalfExtentX;
            float areaZ = baseArea.transform.position.z - areaHalfExtentZ;
            
            if(AreasIntersect(
                objX, objZ, xSize, zSize, 
                areaX, areaZ, baseArea.transform.localScale.x, baseArea.transform.localScale.z
            ))
            {
                return baseArea;
            }
        }
        return null;
    }

    /// <summary>
    /// Chick if the given areas overlap each other.
    /// </summary>
    /// <param name="x1"> X position (left bottom corner) of the first region.</param>
    /// <param name="y1"> Y position (left bottom corner) of the first region.</param>
    /// <param name="w1"> Width (x-direction) of the first region.</param>
    /// <param name="h1"> Height (y direction) of the first region.</param>
    /// <param name="x2"> X position (left bottom corner) of the second region.</param>
    /// <param name="y2"> Y position (left bottom corner) of the second region.</param>
    /// <param name="w2"> Width (x-direction) of the second region.</param>
    /// <param name="h2"> Height (y direction) of the second region.</param>
    /// <returns> True if regions overlap, else false.</returns>
    private bool AreasIntersect(
        float x1, float y1, float w1, float h1,
        float x2, float y2, float w2, float h2)
    {
        // Calculate coordinates of the overlap area
        float left = Mathf.Max(x1, x2);
        float right = Mathf.Min(x1 + w1, x2 + w2);
        float bottom = Mathf.Max(y1, y2); 
        float top = Mathf.Min(y1 + h1, y2 + h2); 

        // get width and height of the area
        float height = top - bottom;
        float width = right - left;

        if(height > 0 && width > 0)
        {
            return true;
        }
        return false;
    }

    public void ResetAgentPosition()
    {
        // find a spawn position and rotation that is not already occluded
        Vector3 spawnPos;
        Bounds bounds;
        Quaternion spawnRot = new Quaternion();
        List<string> forbiddenPlaces = 
            new List<string>(agent.GetComponent<StorageAgent>().collisionTags);
        forbiddenPlaces.Add("base");
        int attempts = 0;   // number of attempts to place the object
        do
        {
            bounds = agent.GetComponent<Collider>().bounds;
            spawnPos = GenerateSpawnPos(0.5f);
            spawnRot.eulerAngles = new Vector3(0, Random.Range(0, 4) * 90.0f, 0);
            attempts ++;
            if (attempts > 100)
            {
                Debug.LogError("Couldn't place the agent. No free space");
                return ;
            }
        } while (
            Utility.OccupiedPosition(
                spawnPos, 
                spawnRot, 
                bounds.extents, 
                forbiddenPlaces,
                QueryTriggerInteraction.Collide).Length > 0);
        string forbiddenString = "";
        foreach(string p in forbiddenPlaces)
            forbiddenString += p + ", ";
        // Debug.Log(forbiddenString);
        // as GenerateSpawnPos generates global coordinates we have to set the global
        // position
        agent.transform.position = spawnPos;
        agent.transform.rotation = spawnRot;
    }

    public GameObject[] GetInteractableObjects()
    {
        return m_objects.ToArray();
    }

    public GameObject[] GetBaseAreas()
    {
        return m_baseAreas.ToArray();
    }

    public void DestroyInteractableObject(GameObject obj)
    {
        m_objects.Remove(obj);
        Destroy(obj);
    }

    public GridBehaviour GetPathGrid()
    {
        return m_PathGrid;
    }

    /// <summary>
    /// Instantiates a new copy of a game object and copies its renderer
    /// material property blocks, if any exist.
    /// </summary>
    /// <param name="gameObject">Original object of which a copy is instantiated.</param>
    /// <param name="pos">Spawn position.</param>
    /// <param name="rotation">Spawn Rotation.</param>
    /// <returns>Spawned GameObject</returns>
    public GameObject InstantiateWithMaterialPropertyBlock(
        GameObject gameObject, 
        Vector3 pos, 
        Quaternion rotation)
    {
        GameObject spawnedObj = Instantiate(gameObject, pos, rotation);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        Renderer gameObjectRenderer;
        if (gameObject.TryGetComponent<Renderer>(out gameObjectRenderer))
        {
            gameObject.GetComponent<Renderer>().GetPropertyBlock(mpb);
            spawnedObj.GetComponent<Renderer>().SetPropertyBlock(mpb);
        }
        return spawnedObj;
    }
}
