using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents.Sensors;
using ConFormSim.ObjectProperties;

namespace ConFormSim.Sensors
{
    /// <summary>
    /// A SensorComponent that creates a <see cref="ObjectPropertyCameraSensor"/>.
    /// </summary>
    [AddComponentMenu("ConFormSim/Object Property Camera Sensor")]
    public class ObjectPropertyCameraSensorComponent : SensorComponent
    {
        [HideInInspector, SerializeField]
        Camera m_Camera;

        ObjectPropertyCameraSensor m_Sensor;

        /// <summary>
        /// Camera object that provides the data to the sensor.
        /// </summary>
        public new Camera camera
        {
            get { return m_Camera;  }
            set { m_Camera = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField]
        string m_SensorName = "ObjectPropertyCameraSensor";

        /// <summary>
        /// Name of the generated <see cref="ObjectPropertyCameraSensor"/> object.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string sensorName
        {
            get { return m_SensorName;  }
            set { m_SensorName = value; }
        }

        public RawImage debugImg;
        public int featureLayer;

        [HideInInspector, SerializeField] 
        int m_Width = 84;

        /// <summary>
        /// Width of the generated observation.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int width
        {
            get { return m_Width;  }
            set { m_Width = value; }
        }

        [HideInInspector, SerializeField] 
        int m_Height = 84;

        /// <summary>
        /// Height of the generated observation.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int height
        {
            get { return m_Height;  }
            set { m_Height = value;  }
        }

        /// <summary>
        /// The feature vector, that will be requested for each object.
        /// </summary>
        public FeatureVectorDefinition featureVectorDefinition;

        /// <summary>
        /// The noise function which will be applied to the feature vector of
        /// the sensor.
        /// </summary>
        public ScriptableNoiseFunction noiseFunction;

        /// <summary>
        /// Creates the <see cref="ObjectPropertyCameraSensor"/>
        /// </summary>
        /// <returns>The created <see cref="ObjectPropertyCameraSensor"/> object for this component.</returns>
        public override ISensor CreateSensor()
        {
            m_Sensor = new ObjectPropertyCameraSensor(m_Camera, m_Width, m_Height, m_SensorName, featureVectorDefinition, noiseFunction);
            return m_Sensor;
        }

        /// <summary>
        /// Computes the observation shape of the sensor.
        /// </summary>
        /// <returns>The observation shape of the associated <see cref="ObjectPropertyCameraSensor"/> object.</returns>
        public override int[] GetObservationShape()
        {
            return ObjectPropertyCameraSensor.GenerateShape(m_Width, m_Height, featureVectorDefinition);
        }

        /// <summary>
        /// Update fields that are safe to change on the Sensor at runtime.
        /// </summary>
        public void UpdateSensor()
        {
            if (m_Sensor != null)
            {
                m_Sensor.camera = m_Camera;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                featureLayer = (featureLayer + 1) % featureVectorDefinition.VectorLength;
            }
            if (debugImg != null)
            {
                m_Sensor.SetDebugImage(debugImg, featureLayer);
            }
        }

    }
}