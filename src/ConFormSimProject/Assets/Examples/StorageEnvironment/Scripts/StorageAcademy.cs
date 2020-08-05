using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using ConFormSim.SideChannels;
using TMPro;
using ConFormSim.ObjectProperties;
public enum CamTypes {
    TopDownCamera,
    TopDownFollowCamera,
    Camera,
}

[System.Serializable]
public class ItemSettings
{
    public GameObject itemPrefab;
    public List<GameObject> interactableObjects;

    [Tooltip("Number of item types to be used. It will always begin from the first " +
            "item in the List.")]
    public int itemTypesToUse;
    [Tooltip("Sets a minimum number of items for each interactable type " + 
        "to be present in the area.")]
    [MinTo()]
    public int numberPerItemType;
    public int numberPerItemTypeMin;

}
[System.Serializable]
public class AreaSettings
{
    [MinTo(100)]
    public int gridSizeX;
    public int gridSizeXMin;
    [MinTo(100)]
    public int gridSizeZ;
    public int gridSizeZMin;

    public GameObject basePrefab;
    public bool noBaseFillColor;
    public bool brighterBases;
    public bool fullBaseLine;

    [Tooltip("Prefabs for base elements. They should be Cubes with size 1x1.")]
    public List<GameObject> baseObjects;

    [Tooltip("Number of base types to be used. It will always begin from the first " +
            "base in the List.")]
    public int baseTypesToUse;
    
    [Tooltip("Sets a minimum number of baseAreas for each base type " + 
    "to be present in the area.")]
    [MinTo()]
    public int numberPerBaseType;
    public int numberPerBaseTypeMin;
    [MinTo(10)]
    public int baseSizeX;
    public int baseSizeXMin;
    [MinTo(10)]
    public int baseSizeZ;
    public int baseSizeZMin;

    public bool baseInCornersOnly;
}

public class StorageAcademy : MonoBehaviour
{
    public GameObject areaPrefab;
    public int numTrainAreas;
    public bool useVisual;
    public bool useRayPerception;
    public bool useObjectPropertyCamera;

    public List<Color32> colorPool; 

    [System.NonSerialized]
    public HashSet<string> interactableTags = new HashSet<string>();
    [System.NonSerialized]
    public HashSet<string> baseTags = new HashSet<string>();

    public TextMeshProUGUI timeScaleText;
    public TextMeshProUGUI agentCountText;
    public AreaSettings areaSettings;
    public ItemSettings itemSettings;
    public Material goalReachedMaterial;
    
    public bool noDisplay = false;
    
    [Tooltip("If true the boxes will vanish as soon as they are placed correctly.")]
    public bool boxesVanish = false;

    [Tooltip("If set, the boxes have to be dropped in order to receive reward. "
        + "otherwise reward will be given as soon as a box is within the target "
        + "area.")]
    public bool boxesNeedDrop = true;

    [Tooltip("If true the agent only receives reward for solving the complete" +
            " task. No intermediate rewards. It will still get a time penalty.")]
    public bool sparseRewardOnly = false;

    public int maxStep;

    ///<summary>
    /// Specifies what type of camera to use for the agent's if it is a loop-2 scene 
    /// or above.
    /// <br></br>Types:
    /// <br></br>TopDownCamera
    /// <br></br>TopDownFollowCamera
    /// <br></br>EgoCamera
    /// <br></br>Else: Defaults to TopDownCamera
    /// </summary>
    public CamTypes cameraType;

    ///<summary>
    /// Specifies the task complexity and thus the task itself.
    /// <br></br>Levels:
    /// <br></br>1 - As soon as an object is picked up the agent is done.
    /// <br></br>2 - As soon as an object is placed on the correct target, the
    /// episode ends. 
    /// <br></br>3 - Only when all objects are on the correct base, the episode ends.
    /// <br></br> Max steps is not affected and will have effect.
    /// </summary>
    public int taskLevel;

    private List<GameObject> m_Areas;
    private Camera activeCamera;
    private GameObject activeMonitor;
    private int activeCamIdx;

    // SideChannels to receive settings from Python
    // FloatProperties for basic configuration
    public EnvironmentParameters envPropertiesChannel;
    // IntListProperty for ColorPool properties
    public IntListPropertiesChannel envListPropertiesChannel;
    
    private bool areasInstantiated;

    public FeatureVectorDefinition featureVectorDefinition;
    public void Awake()
    {
        Debug.Log("Timescale: " + Time.timeScale);
        Debug.Log("Screen Width: " + Screen.width);
        Debug.Log("Screen Height: " + Screen.height);
        m_Areas = new List<GameObject>();
        
        // get env properties chaqnnel from academy
        envPropertiesChannel = Academy.Instance.EnvironmentParameters;
        // create new channel for property lists
        envListPropertiesChannel = new IntListPropertiesChannel();
        // register list side channel
        SideChannelsManager.RegisterSideChannel(envListPropertiesChannel);
        
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        //InstantiateAreas();
        featureVectorDefinition = ScriptableObject.CreateInstance<FeatureVectorDefinition>();
    }

    private void InstantiateAreas()
    {
        // update settings
        ReadPropertiesFromSideChannel();

        Debug.Log("Destroy all Areas.");
        // Delete old areas
        foreach (GameObject area in m_Areas)
        {
            if (area)
            {
                DestroyImmediate(area);
            }
        }
        m_Areas.Clear();

        // Instantiate new areas. Arrange them in a Grid
        int squareDim = Mathf.CeilToInt(Mathf.Sqrt(numTrainAreas));
        for (int i = 0; i *  squareDim < numTrainAreas; i++)
        {
            for(int j = 0; (j < squareDim) && (i * squareDim + j < numTrainAreas); j++)
            {
                Vector3 spawnPos = new Vector3(
                    i * (2 * areaSettings.gridSizeX), 
                    0,
                    j * (2 * areaSettings.gridSizeZ));
                GameObject spawned = Instantiate(areaPrefab, spawnPos, Quaternion.identity);
                m_Areas.Add(spawned);
            }
        }

        Debug.Log("Instantiated " + m_Areas.Count() + " new Areas.");
        activeCamIdx = 0;
        activeCamera = SetActiveCameraInArea(m_Areas[activeCamIdx]);
    }

    private void EnvironmentReset()
    {
        Debug.Log("ResetEnvironment");  
        // update settings
        ReadPropertiesFromSideChannel();    

        // Update color Pool
        UpdateColorPoolFromSideChannel();
        // Create Objects from that
        string[] targetTagProps = CreateItems();
        string[] baseTagProps = CreateBases();

        SetupFeatureVectorDefinition(targetTagProps, baseTagProps);

        SetItemProperties();
        SetBaseProperties();

        // reset all tags
        baseTags.Clear();
        interactableTags.Clear();

        HashSet<string> baseTagProperties = new HashSet<string>();
        HashSet<string> targetTagProperties = new HashSet<string>();
        // Gather tags of all objects and bases
        for(int i = 0; i < areaSettings.baseObjects.Count; i++)
        {
            baseTags.Add(areaSettings.baseObjects[i].tag);
            // EncodedStringProperty baseTagProp = 
            //     areaSettings.baseObjects[i]
            //     .GetComponent<ObjectPropertyProvider>()
            //     .GetObjectProperty("baseTag") as EncodedStringProperty;
            // baseTagProperties.Add(baseTagProp.value);
        }
        for(int i = 0; i < itemSettings.interactableObjects.Count; i++)
        {
            interactableTags.Add(itemSettings.interactableObjects[i].tag);
            // EncodedStringProperty targetTagProp = 
            //     itemSettings.interactableObjects[i]
            //     .GetComponent<ObjectPropertyProvider>()
            //     .GetObjectProperty("targetTag") as EncodedStringProperty;
            // targetTagProperties.Add(targetTagProp.value);
        }

        if(!areasInstantiated)
        {
            InstantiateAreas();
            areasInstantiated = true;
        }
    }

    void SetupFeatureVectorDefinition(string[] targetTags, string[] baseTags)
    {
        // init feature vector
        EncodedStringListProperty baseTagDefaultValue = ScriptableObject.CreateInstance<EncodedStringListProperty>();
        EncodedStringListProperty targetTagDefaultValue = ScriptableObject.CreateInstance<EncodedStringListProperty>();
        BoolObjectProperty isAgentProperty = ScriptableObject.CreateInstance<BoolObjectProperty>();
        BoolObjectProperty isWallProperty = ScriptableObject.CreateInstance<BoolObjectProperty>();
        ColorObjectProperty colorProperty = ScriptableObject.CreateInstance<ColorObjectProperty>();
        
        ObjectPropertySettings baseTagSettings = new ObjectPropertySettings(ScriptableObject.CreateInstance<EncodedStringProperty>());
        ObjectPropertySettings targetTagSettings = new ObjectPropertySettings(ScriptableObject.CreateInstance<EncodedStringProperty>());
        ObjectPropertySettings isAgentSetting = new ObjectPropertySettings(ScriptableObject.CreateInstance<BoolObjectProperty>());
        ObjectPropertySettings isWallSetting = new ObjectPropertySettings(ScriptableObject.CreateInstance<BoolObjectProperty>());
        ObjectPropertySettings colorSetting = new ObjectPropertySettings(ScriptableObject.CreateInstance<ColorObjectProperty>());
        
        baseTagSettings.defaultValue = baseTagDefaultValue;
        targetTagSettings.defaultValue = targetTagDefaultValue;

        baseTagDefaultValue.arrayLength = baseTags.Length;
        baseTagDefaultValue.codeBook = baseTags;

        targetTagDefaultValue.arrayLength = targetTags.Length;
        targetTagDefaultValue.codeBook = targetTags;

        baseTagSettings.arrayLength = baseTags.Length;
        targetTagSettings.arrayLength = targetTags.Length;

        isAgentProperty.value = false;
        isAgentSetting.defaultValue = isAgentProperty;

        isWallProperty.value = false;
        isWallSetting.defaultValue = isAgentProperty;

        featureVectorDefinition.AddPropertySettings("color", colorSetting);
        featureVectorDefinition.AddPropertySettings("baseTag", baseTagSettings);
        featureVectorDefinition.AddPropertySettings("targetTag", targetTagSettings);
        featureVectorDefinition.AddPropertySettings("isAgent", isAgentSetting);
        featureVectorDefinition.AddPropertySettings("isWall", isWallSetting);
    }

    public void Update()
    {
         // Update Camera
        if(Input.GetKeyDown(KeyCode.N))
        {
            // deactivate camera, so that it doesn't render on screen
            activeCamera.enabled = false;
            // deactivate the monitor
            activeMonitor.SetActive(false);
            activeCamIdx = (activeCamIdx + 1) % m_Areas.Count; 
            activeCamera = SetActiveCameraInArea(m_Areas[activeCamIdx]);
        }
        
        // update agent count text
        agentCountText.text = "Agent " + (activeCamIdx + 1) + "/" + m_Areas.Count();   
    }

    public void FixedUpdate()
    {
        if (timeScaleText)
        {
            timeScaleText.text = "TimeScale: " + Time.timeScale + "x";
        }
    }

    private Camera SetActiveCameraInArea(GameObject area)
    {
        // find camera and activate it
        Camera cam = area
                    .transform
                    .FindDeepChild(cameraType.ToString())
                    .GetComponent<Camera>();
        Debug.Log(cam.name);
        if(!noDisplay)
        {
            cam.enabled = true;
        }
        // activate stats monitor
        activeMonitor = area
                        .transform
                        .Find("Agent")
                        .GetComponent<StorageAgent>()
                        .statsMonitor;  
        activeMonitor.SetActive(true);

        return cam;
    }

    public void UpdateColorPoolFromSideChannel()
    {
        List<int> hexColorPool = new List<int>();
        foreach(Color32 col in colorPool)
        {
            int hexCol = Utility.GetIntFromColor(col);
            hexColorPool.Add(hexCol);
        }

        hexColorPool = envListPropertiesChannel
            .GetWithDefault("colorPool", hexColorPool);
        
        colorPool.Clear();
        foreach(int hexCol in hexColorPool)
        {
            Color32 col = Utility.GetColorFromInt(hexCol);
            colorPool.Add(col);
        }
    }

    public string[] CreateItems()
    {
        string[] targetTags = new string[colorPool.Count];

        DestroyGameObjectsInList(itemSettings.interactableObjects);
        List<Color32> colList = new List<Color32>(colorPool);
        int numColors = colorPool.Count;
        UnityEngine.Random.InitState(42);
        for(int i = 0; i < numColors; i++)
        {
            int colId = UnityEngine.Random.Range(0, colList.Count);
            GameObject item  = Instantiate(itemSettings.itemPrefab);
            item.SetActive(false);

            Material colMat = new Material(item.GetComponent<Renderer>().sharedMaterial);
            colId = UnityEngine.Random.Range(0, colList.Count);
            Color32 col = colList.ElementAt(colId);
            colMat.SetColor("_Color", col);
            colList.RemoveAt(colId);

            item.GetComponent<Renderer>().material = colMat;
            itemSettings.interactableObjects.Add(item);

            targetTags[i] = "base_" + Convert.ToString(Utility.GetIntFromColor(col), toBase: 16);
        }
        return targetTags;
    }

    void SetItemProperties()
    {
        foreach(GameObject item in itemSettings.interactableObjects)
        {
            // set target tags in object property provider            
            ObjectPropertyProvider opp;
            if (item.TryGetComponent<ObjectPropertyProvider>(out opp))
            {
                Color32 col = item.GetComponent<Renderer>().material.GetColor("_Color");
                string tag = "base_" + 
                    Convert.ToString(Utility.GetIntFromColor(col), toBase: 16);
                List<string> targetTagsList = new List<string>();
                targetTagsList.Add(tag);

                opp.AvailableProperties = featureVectorDefinition;
                EncodedStringListProperty targetTagsProp = ScriptableObject.CreateInstance<EncodedStringListProperty>();
                targetTagsProp.stringValues = targetTagsList.ToArray();
                opp.SetObjectProperty("targetTag", targetTagsProp);
                ColorObjectProperty colorProp = ScriptableObject.CreateInstance<ColorObjectProperty>();
                colorProp.useObjectRenderer = true;
                colorProp.instanceNoise = 0.00f;
                opp.SetObjectProperty("color", colorProp);
            }
        }
    }

    public string[] CreateBases()
    {
        DestroyGameObjectsInList(areaSettings.baseObjects);
        List<Color32> colList = new List<Color32>(colorPool);
        int numColors = Math.Min(areaSettings.baseTypesToUse, colorPool.Count);

        string[] baseTags = new string[numColors];

        for(int i = 0; i < numColors; i++)
        {
            int colId = UnityEngine.Random.Range(0, colList.Count);
            Color32 col = colList.ElementAt(colId);
            colList.RemoveAt(colId);

            GameObject baseArea  = Instantiate(areaSettings.basePrefab);
            baseArea.SetActive(false);
            Material colMat = new Material(baseArea.GetComponent<Renderer>().sharedMaterial);
            Color baseColor = new Color32(col.r, col.g, col.b, col.a);
            BaseController baseContr = baseArea.GetComponent<BaseController>();
            baseContr.originalColor = baseColor;
            if(areaSettings.noBaseFillColor)
            {
                baseColor.a = 0;
            } 
            else if (areaSettings.brighterBases)
            {
                float H, S, V;
                Color.RGBToHSV(baseColor, out H, out S, out V);
                baseColor = Color.HSVToRGB(H, 0.5f, V * 1.2f);
            }
            colMat.SetColor("_Color", baseColor);
            baseArea.GetComponent<Renderer>().material = colMat;

            baseTags[i] = "base_" + 
                    Convert.ToString(Utility.GetIntFromColor(col), toBase: 16);

            areaSettings.baseObjects.Add(baseArea);
        }
        return baseTags;
    }
    
    void SetBaseProperties()
    {
        foreach(GameObject baseArea in areaSettings.baseObjects)
        {
            // set base tags
            ObjectPropertyProvider opp;
            if (baseArea.TryGetComponent<ObjectPropertyProvider>(out opp))
            {
                Color32 col = baseArea.GetComponent<Renderer>().material.GetColor("_Color");
                string tag = "base_" + 
                    Convert.ToString(Utility.GetIntFromColor(col), toBase: 16);
                List<string> baseTagsList = new List<string>();
                baseTagsList.Add(tag);

                opp.AvailableProperties = featureVectorDefinition;
                EncodedStringListProperty baseTagsProp = ScriptableObject.CreateInstance<EncodedStringListProperty>();
                baseTagsProp.ApplySettings(opp.AvailableProperties.Properties["baseTag"]);
                baseTagsProp.stringValues = baseTagsList.ToArray();
                opp.SetObjectProperty("baseTag", baseTagsProp);
                ColorObjectProperty colorProp = ScriptableObject.CreateInstance<ColorObjectProperty>();
                colorProp.useObjectRenderer = true;
                colorProp.instanceNoise = 0f;
                opp.SetObjectProperty("color", colorProp);
            }
        }
    }

    private void DestroyGameObjectsInList(List<GameObject> gameObjects)
    {
        foreach(GameObject go in gameObjects)
        {
            Destroy(go);
        }
        gameObjects.Clear();
    }

    private void ReadPropertiesFromSideChannel()
    {
        // --------------------------------------------------------------------------
        // general settings
        float noDisplayDefault = noDisplay ? 1.0f : 0.0f;
        noDisplay = envPropertiesChannel
            .GetWithDefault(
                "noDisplay", 
                noDisplayDefault) == 0.0f ? false : true;
        float useVisualDefault = useVisual ? 1.0f : 0.0f;
        useVisual = envPropertiesChannel
            .GetWithDefault(
                "useVisual",
                useVisualDefault) == 0.0f ? false : true;
        float useRayPerceptionDefault = useRayPerception ? 1.0f : 0.0f;
        useRayPerception = envPropertiesChannel
            .GetWithDefault(
                "useRayPerception", 
                useRayPerceptionDefault) == 0.0f ? false : true;
        float useObjectPropertyCameraDefault = useObjectPropertyCamera ? 1.0f : 0.0f;
        useObjectPropertyCamera = envPropertiesChannel
            .GetWithDefault(
                "useObjectPropertyCamera", 
                useObjectPropertyCameraDefault) == 0.0f ? false : true;
        numTrainAreas = (int) envPropertiesChannel
            .GetWithDefault("numTrainAreas", numTrainAreas);
        taskLevel = (int) envPropertiesChannel
            .GetWithDefault("taskLevel", taskLevel);
        maxStep = (int) envPropertiesChannel
            .GetWithDefault("maxSteps", maxStep);
        //---------------------------------------------------------------------------
        // area settings
        areaSettings.gridSizeXMin = (int) envPropertiesChannel
            .GetWithDefault(
                "minGridSizeX", 
                areaSettings.gridSizeXMin);
        areaSettings.gridSizeX = (int) envPropertiesChannel
            .GetWithDefault(
                "maxGridSizeX", 
                areaSettings.gridSizeX);
        areaSettings.gridSizeZMin = (int) envPropertiesChannel
            .GetWithDefault(
                "minGridSizeZ", 
                areaSettings.gridSizeZMin);
        areaSettings.gridSizeZ = (int) envPropertiesChannel
            .GetWithDefault(
                "maxGridSizeX", 
                areaSettings.gridSizeZ);
        areaSettings.numberPerBaseType = (int) envPropertiesChannel
            .GetWithDefault(
                "numberPerBaseTypeMax", 
                (float) areaSettings.numberPerBaseType);
        areaSettings.numberPerBaseTypeMin = (int) envPropertiesChannel
            .GetWithDefault(
                "numberPerBaseTypeMin", 
                (float) areaSettings.numberPerBaseTypeMin);
        areaSettings.baseSizeX = (int) envPropertiesChannel
            .GetWithDefault(
                "baseSizeXMax", 
                (float) areaSettings.baseSizeX);
        areaSettings.baseSizeXMin = (int) envPropertiesChannel
            .GetWithDefault(
                "baseSizeXMin", 
                (float) areaSettings.baseSizeXMin);
        areaSettings.baseSizeZ = (int) envPropertiesChannel
            .GetWithDefault(
                "baseSizeZMax", 
                (float) areaSettings.baseSizeZ);
        areaSettings.baseSizeZMin = (int) envPropertiesChannel
            .GetWithDefault(
                "baseSizeZMin", 
                (float) areaSettings.baseSizeZMin);
        float baseInCornersDefault = areaSettings.baseInCornersOnly ? 1.0f : 0.0f;
        areaSettings.baseInCornersOnly = envPropertiesChannel
            .GetWithDefault(
                "baseInCornersOnly", 
                baseInCornersDefault) == 0.0f ? false: true;
        float vanishDefault = boxesVanish ? 1.0f : 0.0f;
        boxesVanish = envPropertiesChannel
            .GetWithDefault(
                "boxesVanish", 
                vanishDefault) == 0.0f ? false : true;
        float boxesNeedDropDefault = boxesNeedDrop ? 1.0f : 0.0f;
        boxesNeedDrop = envPropertiesChannel
            .GetWithDefault(
                "boxesNeedDrop", 
                boxesNeedDropDefault) == 0.0f ? false : true;
        float sparseRewardDefault = sparseRewardOnly ? 1.0f : 0.0f;
        sparseRewardOnly = envPropertiesChannel
            .GetWithDefault(
                "sparseReward", 
                sparseRewardDefault) == 0.0f ? false : true;
        float noBaseFillColorDefault = areaSettings.noBaseFillColor ? 1.0f: 0.0f;
        areaSettings.noBaseFillColor = envPropertiesChannel
            .GetWithDefault(
                "noBaseFillColor", 
                noBaseFillColorDefault) == 0.0f ? false : true;
        float brighterBasesDefault = areaSettings.brighterBases ? 1.0f: 0.0f;
        areaSettings.brighterBases = envPropertiesChannel
            .GetWithDefault(
                "brighterBases", 
                brighterBasesDefault) == 0.0f ? false : true;
        float fullBaseLineDefault = areaSettings.fullBaseLine ? 1.0f: 0.0f;
        areaSettings.fullBaseLine = envPropertiesChannel
            .GetWithDefault(
                "fullBaseLine", 
                fullBaseLineDefault) == 0.0f ? false : true;
        areaSettings.baseTypesToUse = (int) envPropertiesChannel
            .GetWithDefault(
                "numBaseTypesToUse", 
                (float) areaSettings.baseTypesToUse);       
        //---------------------------------------------------------------------------
        // item settings
        itemSettings.numberPerItemType = (int) envPropertiesChannel
            .GetWithDefault(
                "numberPerItemTypeMax", 
                (float) itemSettings.numberPerItemType);
        itemSettings.numberPerItemTypeMin = (int) envPropertiesChannel
            .GetWithDefault(
                "numberPerItemTypeMin", 
                (float) itemSettings.numberPerItemTypeMin);
        itemSettings.itemTypesToUse = (int) envPropertiesChannel
            .GetWithDefault(
                "numItemTypesToUse", 
                (float) itemSettings.itemTypesToUse);
        //---------------------------------------------------------------------------
    }
}
