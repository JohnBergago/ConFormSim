using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using ConFormSim.SideChannels;

public class AreaSpawner : MonoBehaviour
{
    public WorldSettings worldSettings;

    public GameObject areaPrefab;
    
    private GameObject[] areas;

    /// <summary>
    /// Side channel to receive environment properties from python
    /// </summary>
    public EnvironmentParameters envParams;

    public IntListPropertiesChannel envListParams;

    private bool wasInit = false;

    void OnEnable()
    {
        Random.InitState(42);
        envParams = Academy.Instance.EnvironmentParameters;
        envListParams = new IntListPropertiesChannel();
        SideChannelsManager.RegisterSideChannel(envListParams);
        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
    }
    
    public void OnEnvironmentReset()
    {
        // Reset parameters from side channel
        worldSettings.numAreas = Mathf.RoundToInt(envParams.GetWithDefault("numTrainAreas", worldSettings.numAreas));
        worldSettings.gridSize = Mathf.RoundToInt(envParams.GetWithDefault("gridSize", worldSettings.gridSize));
        worldSettings.maxNumSteps = Mathf.RoundToInt(envParams.GetWithDefault("maxSteps", worldSettings.maxNumSteps));
        worldSettings.solutionLength = envListParams.GetWithDefault("solutionLength", worldSettings.solutionLength.ToList()).ToArray();
        worldSettings.numForward = envListParams.GetWithDefault("numForward", worldSettings.numForward.ToList()).ToArray();
        worldSettings.numBackward = envListParams.GetWithDefault("numBackward", worldSettings.numBackward.ToList()).ToArray();
        worldSettings.branchLength = Mathf.RoundToInt(envParams.GetWithDefault("branchLength", worldSettings.branchLength));
        
        UpdateColorPoolFromSideChannel();
        if (! wasInit)
        {
            InitTrainingAreas();
            wasInit = true;
        }
        
    }

    public void UpdateColorPoolFromSideChannel()
    {
        List<int> hexColorPool = new List<int>();
        foreach(Color32 col in worldSettings.colors)
        {
            int hexCol = Utility.GetIntFromColor(col);
            hexColorPool.Add(hexCol);
        }

        hexColorPool = envListParams
            .GetWithDefault("colors", hexColorPool);
        
        List<Color> colorList = new List<Color>(); 
        foreach(int hexCol in hexColorPool)
        {
            Color32 col = Utility.GetColorFromInt(hexCol);
        colorList.Add(col);
        }
        worldSettings.colors = colorList.ToArray();
    }

    void InitTrainingAreas()
    {
        // spawn areas
        areas = new GameObject[worldSettings.numAreas];
        // Instantiate new areas. Arrange them in a Grid
        int squareDim = Mathf.CeilToInt(Mathf.Sqrt(worldSettings.numAreas));
        for (int i = 0; i *  squareDim < worldSettings.numAreas; i++)
        {
            for(int j = 0; (j < squareDim) && (i * squareDim + j < worldSettings.numAreas); j++)
            {
                Vector3 spawnPos = new Vector3(
                    i * (2 * worldSettings.gridSize), 
                    0,
                    j * (2 * worldSettings.gridSize));
                GameObject spawned = Instantiate(areaPrefab, spawnPos, Quaternion.identity);
                areas[i * squareDim + j] = spawned;
                spawned.SetActive(true);
                if (i * squareDim + j != 0)
                {
                    spawned.transform.FindDeepChild("Monitor").gameObject.SetActive(false);
                    spawned.transform.FindDeepChild("TopDownCamera").gameObject.GetComponent<Camera>().enabled = false;
                }
            }
        }

    }
}
