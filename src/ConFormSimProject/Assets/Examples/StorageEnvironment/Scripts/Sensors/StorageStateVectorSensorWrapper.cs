using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using ConFormSim.ObjectProperties;
using ConFormSim.Sensors;

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

    ObjectPropertyRequester propertyRequester;
    int m_NumPropertiesPerObject;
    int maxLengthPropItems = 0;
    int maxLengthPropBases = 0;

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
        propertyRequester = new ObjectPropertyRequester(featureVectorDefinition);

        int maxNumItems = academy.itemSettings.itemTypesToUse 
            * academy.itemSettings.numberPerItemType;
        int maxNumBases = academy.areaSettings.baseTypesToUse
            * academy.areaSettings.numberPerBaseType;
        
        // Properties per item and base: position + properties from requester
        m_NumPropertiesPerObject = 2 + m_Academy.areaSettings.baseTypesToUse;

        // calculate size of the sensor
        int observationSize =
            // Agent position (x, z, rot)  
            3       
            + maxNumItems * (2 + propertyRequester.FeatureVectorLength)
            + maxNumBases * (2 + propertyRequester.FeatureVectorLength);
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

        // add item properties
        int maxNumItems = m_Academy.itemSettings.itemTypesToUse 
            * m_Academy.itemSettings.numberPerItemType;
        for (int i = 0; i < maxNumItems; i++)
        {
            // Debug.Log("Item " + i);
            if (i < m_Area.GetInteractableObjects().Length && m_Area.GetInteractableObjects()[i] != null)
            {
                GameObject item = m_Area.GetInteractableObjects()[i];
                // add position of the item relative to the agent
                Vector3 itemPos = grid
                    .WorldToGridCoordinates(item.transform.position);
                Vector3 itemPosRel = itemPos - agentPos;
                m_VectorSensor.AddObservation( 
                    (itemPosRel.x + gridMaxX) / gridMaxX - 1 );
                m_VectorSensor.AddObservation( 
                    (itemPosRel.z + gridMaxZ) / gridMaxZ - 1 );
                // Debug.Log(((itemPosRel.x + gridMaxX) / gridMaxX - 1 ) + ", " + ((itemPosRel.z + gridMaxZ) / gridMaxZ - 1 ));

                // request properties from properties requester
                m_VectorSensor.AddObservation(propertyRequester.RequestProperties(item)); 
            }
            else 
            {
                // Debug.Log("Empty Item");
                for (int j = 0; j < 2 + propertyRequester.FeatureVectorLength; j++)
                {
                    m_VectorSensor.AddObservation(-1.0f);
                    // Debug.Log(-1.0f);
                }
            }
        }

        // add base properties
        int maxNumBases = m_Academy.areaSettings.baseTypesToUse
            * m_Academy.areaSettings.numberPerBaseType;
        for (int i = 0; i < maxNumBases; i++)
        {
            // Debug.Log("Base " + i);
            if (i < m_Area.GetBaseAreas().Length && m_Area.GetBaseAreas()[i] != null)
            {
                GameObject baseObj = m_Area.GetBaseAreas()[i];
                // add position of the item relative to the agent
                Vector3 basePos = grid
                    .WorldToGridCoordinates(baseObj.transform.position);
                Vector3 basePosRel = basePos - agentPos;
                m_VectorSensor.AddObservation( 
                    (basePosRel.x + gridMaxX) / gridMaxX - 1 );
                m_VectorSensor.AddObservation( 
                    (basePosRel.z + gridMaxZ) / gridMaxZ - 1 );

                // Debug.Log(((basePosRel.x + gridMaxX) / gridMaxX - 1 ) + ", " + ((basePosRel.z + gridMaxZ) / gridMaxZ - 1 ));
                
                m_VectorSensor.AddObservation(propertyRequester.RequestProperties(baseObj)); 
            }
            else 
            {
                // Debug.Log("Empty Base");
                for (int j = 0; j < 2 + maxLengthPropBases; j++)
                {
                    m_VectorSensor.AddObservation(-1.0f);
                //    Debug.Log(-1.0f);
                }
            }
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


