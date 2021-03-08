using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using ConFormSim.ObjectProperties;

namespace ConFormSim.Sensors
{
    public class ObjectPropertyCameraSensor : ISensor
    {
        Camera m_Camera;
        int m_Width;
        int m_Height;
        string m_Name;
        int[] m_Shape;
        SensorCompressionType m_CompressionType;
        float[] m_PropertyImage;

        /// <summary>
        /// The default feature vector for all pixels that are not covered by
        /// objects with poperty providers.
        /// </summary>
        private float[] m_defaultFeatureVector = new float[256];

        /// <summary>
        /// The Camera used for rendering the sensor observations.
        /// </summary>
        public Camera camera
        {
            get { return m_Camera; }
            set { m_Camera = value; }
        }

        /// <summary>
        /// The compression type used by the sensor.
        /// </summary>
        public SensorCompressionType compressionType
        {
            get { return m_CompressionType;  }
            set { m_CompressionType = value; }
        }

        private ScriptableNoiseFunction m_noiseFunc;

        private FeatureVectorDefinition featureVectorDefinition;

        public static Shader featureVecShader;

        /// <summary>
        /// Creates and returns the camera sensor.
        /// </summary>
        /// <param name="camera">Camera object to capture images from.</param>
        /// <param name="width">The width of the generated visual observation.</param>
        /// <param name="height">The height of the generated visual observation.</param>
        /// <param name="grayscale">Whether to convert the generated image to grayscale or keep color.</param>
        /// <param name="name">The name of the camera sensor.</param>
        /// <param name="compression">The compression to apply to the generated image.</param>
        public ObjectPropertyCameraSensor(
            Camera camera, int width, int height, string name, 
            FeatureVectorDefinition fvd, ScriptableNoiseFunction noiseFunc = null)
        {
            m_Camera = camera;
            m_Width = width;
            m_Height = height;
            m_Name = name;
            m_Shape = GenerateShape(width, height, fvd);
            fvd.GetDefaultFeatureVector().CopyTo(m_defaultFeatureVector, 0);
            this.m_noiseFunc = noiseFunc;
            featureVectorDefinition = fvd;
            int fvLength = fvd.VectorLength;
            m_PropertyImage = new float[width * height * (fvLength + 4 - fvLength % 4)];

            if(!featureVecShader)
                featureVecShader = Shader.Find("Hidden/FeatureVectorShader");

        }
        
        /// <summary>
        /// This method can be used to get a grayscale view of one layer of the
        /// resulting feature vector. For best performance call this in the
        /// Update() method. 
        /// </summary>
        /// <param name="image">The target image used to display the
        /// layer.</param>
        /// <param name="debugLayer">The number of the feature layer to be
        /// displayed, ranging from 0 to (feature vector length - 1).</param>
        public void SetDebugImage(RawImage image, int debugLayer)
        {
            debugLayer %= featureVectorDefinition.VectorLength;
            Texture2D texture2D;
            if (image.texture != null)
            {
                texture2D = (Texture2D) image.texture;
            }
            else
            {
                texture2D = new Texture2D(m_Width, m_Height, TextureFormat.RGBAFloat, false);
                image.texture = texture2D;
            }

            texture2D.filterMode = FilterMode.Point;
            for(int row = 0; row < m_Width; row++)
            {
                for(int col = 0; col < m_Height; col++)
                {
                    int depth = debugLayer % 4;
                    int layer = debugLayer / 4;
                    float value = m_PropertyImage[depth + 4 * (col+ m_Width*(row + m_Height*layer))];
                    texture2D.SetPixel(col, row, new Color(value, value, value, 1));
                }
            }
            
            texture2D.Apply();
        }

        /// <summary>
        /// Accessor for the name of the sensor.
        /// </summary>
        /// <returns>Sensor name.</returns>
        public string GetName()
        {
            return m_Name;
        }

        /// <summary>
        /// Accessor for the size of the sensor data. Will be h x w x 1 for grayscale and
        /// h x w x 3 for color.
        /// </summary>
        /// <returns>Size of each of the three dimensions.</returns>
        public int[] GetObservationShape()
        {
            return m_Shape;
        }

        /// <summary>
        /// Generates a compressed image. This can be valuable in speeding-up training.
        /// </summary>
        /// <returns>Compressed image.</returns>
        public byte[] GetCompressedObservation()
        {
            return null;
        }

        /// <summary>
        /// Writes out the generated, uncompressed image to the provided <see cref="WriteAdapter"/>.
        /// </summary>
        /// <param name="writer">Where the observation is written to.</param>
        /// <returns></returns>
        public int Write(ObservationWriter writer)
        {
            SetupObjectPropertyProviders();
            
            ObservationToPropertyImage();
            
            int index = 0;
            int numLayer = (featureVectorDefinition.VectorLength+3)/4;
            
            // invert rows due to texture format
            for(int row = m_Width-1; row > -1; row--)
            {
                for(int col = 0; col < m_Height; col++)
                {
                    for (int layer = 0; layer < numLayer; layer++)
                    {
                        for(int depth = 0; depth < 4; depth++)
                        {
                            writer[index++] = m_PropertyImage[depth + 4 * (col+ m_Width*(row + m_Height*layer))];
                        }
                    }
                }
            }
            return m_PropertyImage.Length;
        }

        /// <inheritdoc/>
        public void Update() 
        {
            
        }

        /// <inheritdoc/>
        public SensorCompressionType GetCompressionType()
        {
            return SensorCompressionType.None;
        }

        /// <summary>
        /// Finds the Object Property Provider Objects relevant to this sensor
        /// and prepares them by setting the right properties.
        /// </summary>
        private void SetupObjectPropertyProviders()
        {
            // retrieve all ObjectPropertyProvider objects registerd with the
            // given feature vector
            ObjectPropertyProvider[] opps = featureVectorDefinition.RegisteredOPPs;
            
            // Find all opp that have a visible renderer attached to their game
            // object or children of their game object
            HashSet<Renderer> oppRenderers = new HashSet<Renderer>();
            HashSet<ObjectPropertyProvider> initOpps = new HashSet<ObjectPropertyProvider>();
            for(int i = 0; i < opps.Length; i++)
            {
                opps[i].SetFVRenderProperty();
            }
        }   

        /// <summary>
        /// Render the camera with the feature shader for each batch of layers
        /// and store the result in the m_propertyImage array.
        /// </summary>
        private void ObservationToPropertyImage()
        {
            int fvLength = featureVectorDefinition.VectorLength;
            int imgLength =  m_Width * m_Height * (fvLength + 4 - fvLength % 4);
            if (m_PropertyImage.Length != imgLength)
            {
                m_PropertyImage = new float[imgLength];
            }
            
            // save current camera settings
            var oldRec = m_Camera.rect;
            var prevCameraRT = m_Camera.targetTexture;
            var oldCameraClearFlags = m_Camera.clearFlags;
            var oldCameraBackground = m_Camera.backgroundColor;
            // save current active render texture
            var prevActiveRT = RenderTexture.active;

            // setup camera
            m_Camera.rect = new Rect(0f, 0f, 1f, 1f);
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = Color.black;
            m_Camera.enabled = false;
            // setup temporary render texture
            var rtFormat = RenderTextureFormat.ARGBFloat;
            var rtReadWrite = RenderTextureReadWrite.Linear;
            var rtDepth = 24;
            var tempRT =
                RenderTexture.GetTemporary(m_Width, m_Height, rtDepth, rtFormat, rtReadWrite);

            // setup texture to read the render result
            var texture2D = new Texture2D(m_Width, m_Height, TextureFormat.RGBAFloat, false);

            // render to offscreen texture
            RenderTexture.active = tempRT;
            m_Camera.targetTexture = tempRT;

            // set default featureVector
            // float[] defaultFV = new float[fvLength + 4 - fvLength % 4];
            Shader.SetGlobalFloatArray("_DefaultFeatureVector", m_defaultFeatureVector);
            Shader.SetGlobalInt("_CurrentFVDefinitionID", featureVectorDefinition.GetInstanceID());
            // render multiple times, until all properties of the feature vector
            // are read.
            for (int i = 0; i < fvLength; i += 4)
            {
                m_Camera.backgroundColor = new Color(
                    m_defaultFeatureVector[i], 
                    m_defaultFeatureVector[i + 1], 
                    m_defaultFeatureVector[i + 2], 
                    m_defaultFeatureVector[i + 3]);
                Shader.SetGlobalInt("_StartIndex", i);
                m_Camera.RenderWithShader(featureVecShader, "RenderType");
                texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);
                texture2D.filterMode = FilterMode.Point;
                var values = texture2D.GetRawTextureData<float>();
                Unity.Collections.NativeArray<float>.Copy(values, 0, m_PropertyImage, values.Length * i / 4, values.Length);
            }

            // reset all camera settings
            m_Camera.targetTexture = prevCameraRT;
            m_Camera.rect = oldRec;
            m_Camera.clearFlags = oldCameraClearFlags;
            m_Camera.backgroundColor = oldCameraBackground;
            m_Camera.enabled = true;
            RenderTexture.active = prevActiveRT;
            RenderTexture.ReleaseTemporary(tempRT);
            DestroyTexture(texture2D);
        }

        /// <summary>
        /// Computes the observation shape for a camera sensor based on the height, width
        /// and grayscale flag.
        /// </summary>
        /// <param name="width">Width of the image captures from the camera.</param>
        /// <param name="height">Height of the image captures from the camera.</param>
        /// <returns>The observation shape.</returns>
        internal static int[] GenerateShape(int width, int height, FeatureVectorDefinition fvd)
        {
            return new[] { height, width, fvd.VectorLength };
        }

        static void DestroyTexture(Texture2D texture)
        {
            if (Application.isEditor)
            {
                // Edit Mode tests complain if we use Destroy()
                // TODO move to extension methods for UnityEngine.Object?
                UnityEngine.Object.DestroyImmediate(texture);
            }
            else
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        public void Reset()
        {
        }
    }
}