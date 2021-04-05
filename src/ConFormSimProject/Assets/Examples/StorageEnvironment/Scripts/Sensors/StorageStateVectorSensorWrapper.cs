using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using ConFormSim.ObjectProperties;
using ConFormSim.Sensors;
using System.Linq;

/// <summary>
/// A wrapper class that wraps a VectorSensor and fills it with information
/// regrding the storage environment.
/// </summary>
public class StorageStateVectorSensorWrapper : ISensor
{
    StorageAcademy m_Academy;
    StorageArea m_Area;
    GameObject m_Agent;

    VectorSensor m_VectorSensor;
    int m_NumPropertiesPerObject;
    int m_numTargetTags = 0;
    int m_numBaseTags = 0;
    FeatureVectorDefinition m_FeatureVectorDefinition;

    /// <summary>
    /// For each target tag there is one index in the one hot vector.
    /// </summary>
    /// <typeparam name="string">target tag</typeparam>
    /// <typeparam name="int">index in hot encoded vector</typeparam>
    /// <returns></returns>
    Dictionary<string, int> m_TargetTagIdxDict = new Dictionary<string, int>();

    /// <summary>
    /// Initializes the VectorSensor
    /// </summary>
    /// <param naem="agent"> The agent whose position should be set in the
    /// observation. </param>
    /// <param name="academy">The StorageAcademy that holds all the environment
    /// configuration parameters</param>
    /// <param name="area">The StorageArea in which the agent is.</param>
    /// <param name="name">Name of the sensor.</param>
    public StorageStateVectorSensorWrapper(
        GameObject agent,
        StorageAcademy academy, 
        StorageArea area, 
        FeatureVectorDefinition featureVectorDefinition,
        ScriptableNoiseFunction noiseFunction,
        string name = null)
    {
        m_Academy = academy;
        m_Area = area;
        m_Agent = agent;
        m_FeatureVectorDefinition = featureVectorDefinition;


        int maxNumItems = academy.itemSettings.itemTypesToUse 
            * academy.itemSettings.numberPerItemType;
        int maxNumBases = academy.areaSettings.baseTypesToUse
            * academy.areaSettings.numberPerBaseType;
        
        // Properties per item and base: position + properties from requester
        m_NumPropertiesPerObject = 2 + m_Academy.areaSettings.baseTypesToUse;


        m_numTargetTags = featureVectorDefinition.Properties["targetTag"].lengthInFeatureVector;
        m_numBaseTags = featureVectorDefinition.Properties["baseTag"].lengthInFeatureVector;


        // calculate size of the sensor
        int observationSize =
            // Agent position (x, z, rot)  
            3       
            + maxNumItems * (2 + m_numTargetTags)
            + maxNumBases * (2 + m_numBaseTags);
        Debug.Log("Observation Size: " + observationSize);
        m_VectorSensor = new VectorSensor(observationSize, name);
    }

    public byte[] GetCompressedObservation()
    {
        return ((ISensor)m_VectorSensor).GetCompressedObservation();
    }

    public SensorCompressionType GetCompressionType()
    {
        return ((ISensor)m_VectorSensor).GetCompressionType();
    }

    public string GetName()
    {
        return ((ISensor)m_VectorSensor).GetName();
    }

    public int[] GetObservationShape()
    {
        return ((ISensor)m_VectorSensor).GetObservationShape();
    }

    public void Update()
    {
        // Debug.Log("=========================");
        // clears the sensor
        ((ISensor)m_VectorSensor).Update();

        // add new observations
        GridBehaviour grid = m_Area.GetPathGrid();

        // get grid size for scaling positions
        float gridMaxX = grid.columns - 1;
        float gridMaxZ = grid.rows - 1;

        Vector3 agentPos = grid
            .WorldToGridCoordinates(m_Agent.transform.position);
        
        // Add agent positions to observation
        // Debug.Log("Agent Pos.");
        m_VectorSensor.AddObservation( 2 * agentPos.x / gridMaxX - 1 );
        m_VectorSensor.AddObservation( 2 * agentPos.z / gridMaxZ - 1 );
        m_VectorSensor.AddObservation(
            (m_Agent.transform.rotation.eulerAngles.y % 360) / 180.0f - 1 );

        // Debug.Log((2 * agentPos.x / gridMaxX - 1) + ", " + (2 * agentPos.z / gridMaxZ - 1) + ", "  + ((m_Agent.transform.rotation.eulerAngles.y % 360) / 180.0f - 1));        

        GameObject[] interactableObjects = m_Area.GetInteractableObjects();
        int interObjIdx = 0;
        for (int i = 0; i < m_Academy.itemSettings.itemTypesToUse; i++)
        {
            string currentType = m_Academy.itemSettings.interactableObjects[i]
                .GetComponent<ObjectController>().typeName;
            for(int j = 0; j < m_Academy.itemSettings.numberPerItemType; j++)
            {
                if (interObjIdx < interactableObjects.Length && 
                    interactableObjects[interObjIdx]
                        .GetComponent<ObjectController>().typeName == currentType)
                {
                    GameObject item = interactableObjects[interObjIdx];
                    interObjIdx++;
                    // add position of the item relative to the agent
                    Vector3 itemPos = grid
                        .WorldToGridCoordinates(item.transform.position);
                    Vector3 itemPosRel = itemPos - agentPos;
                    m_VectorSensor.AddObservation( 
                        (itemPosRel.x + gridMaxX) / gridMaxX - 1 );
                    m_VectorSensor.AddObservation( 
                        (itemPosRel.z + gridMaxZ) / gridMaxZ - 1 );
                    // Debug.Log(((itemPosRel.x + gridMaxX) / gridMaxX - 1 ) + ", " + ((itemPosRel.z + gridMaxZ) / gridMaxZ - 1 ));

                    // request target tag
                    m_VectorSensor.AddObservation(RequestFeatureFromObject(item, "targetTag"));
                }
                else
                {
                    // Debug.Log("Empty Item");
                    for (int k = 0; k < 2 + m_numTargetTags; k++)
                    {
                        m_VectorSensor.AddObservation(-1.0f);
                        // Debug.Log(-1.0f);
                    }
                }
            }
        }

        GameObject[] baseObjects = m_Area.GetBaseAreas();
        int baseObjIdx = 0;
        for (int i = 0; i < m_Academy.areaSettings.baseTypesToUse; i++)
        {
            string currentType = m_Academy.areaSettings.baseObjects[i]
                .GetComponent<BaseController>().typeName;
            for(int j = 0; j < m_Academy.areaSettings.numberPerBaseType; j++)
            {
                if (baseObjIdx < baseObjects.Length &&
                    baseObjects[baseObjIdx]
                        .GetComponent<BaseController>().typeName == currentType)
                {
                    GameObject baseArea = baseObjects[baseObjIdx];
                    baseObjIdx++;
                    // add position of the item relative to the agent
                    Vector3 baseAreaPos = grid
                        .WorldToGridCoordinates(baseArea.transform.position);
                    Vector3 baseAreaPosRel = baseAreaPos - agentPos;
                    m_VectorSensor.AddObservation( 
                        (baseAreaPosRel.x + gridMaxX) / gridMaxX - 1 );
                    m_VectorSensor.AddObservation( 
                        (baseAreaPosRel.z + gridMaxZ) / gridMaxZ - 1 );
                    // Debug.Log(((baseAreaPosRel.x + gridMaxX) / gridMaxX - 1 ) + ", " + ((baseAreaPosRel.z + gridMaxZ) / gridMaxZ - 1 ));

                    // request base tag
                    m_VectorSensor.AddObservation(RequestFeatureFromObject(baseArea, "baseTag"));
                }
                else
                {
                    // Debug.Log("Empty Item");
                    for (int k = 0; k < 2 + m_numBaseTags; k++)
                    {
                        m_VectorSensor.AddObservation(-1.0f);
                        // Debug.Log(-1.0f);
                    }
                }
            }
        }
    }

    float[] RequestFeatureFromObject(GameObject gameObject, string featureName)
    {
        // request featurevector
        ObjectPropertyProvider objectPropertyProvider;
        if (gameObject.TryGetComponent<ObjectPropertyProvider>(out objectPropertyProvider))
        {
            return objectPropertyProvider.GetObjectProperty(featureName).GetFeatureVector();
        }
        else
        {
            return m_FeatureVectorDefinition.Properties[featureName].defaultValue.GetFeatureVector();
        }
    }

    public int Write(ObservationWriter adapter)
    {
        return ((ISensor)m_VectorSensor).Write(adapter);
    }

    public void Reset()
    {
    }
}


