using UnityEngine;
using Unity.MLAgents.Sensors;
using ConFormSim.ObjectProperties;
using ConFormSim.Sensors;

/// <summary>
/// A Sensor Component that creates a VectorSensor containing the state of the
/// environment. That is agent and object positions and object types and
/// properties.
/// </summary>
[AddComponentMenu("ConFormSim/Storage State Vector Sensor")]
public class StorageStateVectorSensorComponent : SensorComponent
{
    StorageStateVectorSensorWrapper m_Sensor;

    [SerializeField]
    private StorageAcademy m_Academy;

    /// <summary>
    /// The academy that holds all information about the evironment and the
    /// items in it.
    /// Note that changing this after the sensor is created has no effect.
    /// </summary>
    public StorageAcademy academy
    {
        get { return m_Academy;  }
        set { m_Academy = value; }
    }
    
    [SerializeField]
    private StorageArea m_Area;

    /// <summary>
    /// Area which provides the items and bases.
    /// Note that changing this after the sensor is created has no effect.
    /// </summary>
    public StorageArea area
    {
        get { return m_Area;  }
        set { m_Area = value; }
    }

    [SerializeField]
    private GameObject m_Agent;

    public GameObject agent
    {
        get { return m_Agent;  }
        set { m_Agent = value; }
    }
    string m_SensorName = "StorageStateSensor";

    /// <summary>
    /// Name of the generated VectorSensor object.
    /// changing this at runtime does not affect how the Agent sorts the
    /// sensors. 
    /// </summary>
    public string sensorName
    {
        get { return m_SensorName;  }
        set { m_SensorName = value; }
    }

    /// <summary>
    /// Definition of the feature vector that will be produced for each object.
    /// </summary>
    public FeatureVectorDefinition featureVectorDefinition;

    /// <summary>
    /// A script that implements a noise function. It will be applied on the
    /// complete feature vector.
    /// </summary>
    public ScriptableNoiseFunction noiseFunction;

    public override ISensor CreateSensor()
    {
        m_Sensor = new StorageStateVectorSensorWrapper(
            m_Agent,
            m_Academy,
            m_Area,
            featureVectorDefinition,
            noiseFunction,
            m_SensorName);
        return m_Sensor;
    }

    /// <summary>
    /// Computes the observation shape of the sensor.
    /// </summary>
    /// <returns>The observation shape of the associated <see
    /// cref="StorageStateVectoSensor"/> object.</returns>
    public override int[] GetObservationShape()
    {
        return m_Sensor.GetObservationShape();
    }

}
