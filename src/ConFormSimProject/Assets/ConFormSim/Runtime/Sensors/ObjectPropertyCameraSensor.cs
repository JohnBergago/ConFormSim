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

        /// <summary>
        /// The default feature vector for all pixels that are not covered by
        /// objects with poperty providers.
        /// </summary>
        private List<float> m_defaultFeatureVector;

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
            m_defaultFeatureVector = fvd.GetDefaultFeatureVector();
            this.m_noiseFunc = noiseFunc;
            featureVectorDefinition = fvd;

            if(!featureVecShader)
                featureVecShader = Shader.Find("Hidden/FeatureVectorShader");

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
            int fvLength = featureVectorDefinition.GetFeatureVectorLength();
            float[] propertyImage = ObservationToPropertyImage(
                m_Camera, 
                m_Width, 
                m_Height,
                fvLength);
            
            int index = 0;
            int numLayer = (fvLength+3)/4;
            
            // invert rows due to texture format
            for(int row = m_Width-1; row > -1; row--)
            {
                for(int col = 0; col < m_Height; col++)
                {
                    for (int layer = 0; layer < numLayer; layer++)
                    {
                        for(int depth = 0; depth < 4; depth++)
                        {
                            writer[index++] = propertyImage[depth + 4 * (col+ m_Width*(row + m_Height*layer))];
                        }
                    }
                }
            }
            // for(int i = 0; i < fvLength; i++)
            // {
            //     writer.AddList();
            // }
            return propertyImage.Length;
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
            
            // save rect setting of the camera
            Rect oldRect = m_Camera.rect;
            m_Camera.rect = new Rect(0f, 0f, 1f, 1f);

            // Find all opp that have a visible renderer attached to their game
            // object or children of their game object
            HashSet<Renderer> oppRenderers = new HashSet<Renderer>();
            HashSet<ObjectPropertyProvider> initOpps = new HashSet<ObjectPropertyProvider>();
            foreach(var opp in opps)
            {
                // only check relevant opps
                if (opp.AvailableProperties != featureVectorDefinition)
                {
                    continue;
                }
                Transform oppTransform = opp.gameObject.transform;
                Transform[] oppChildsTransform = new Transform[oppTransform.childCount + 1];
                oppChildsTransform[0] = oppTransform;
                for(int i = 1; i <= oppTransform.childCount; i++)
                    oppChildsTransform[i] = oppTransform.GetChild(i-1);
                foreach(Transform childTransform in oppChildsTransform)
                {     
                    // find the renderer of the child
                    Renderer renderer;
                    if (childTransform.gameObject.TryGetComponent<Renderer>(out renderer))
                    {
                        if (IsVisible(renderer, m_Camera))
                        {
                            // Prepare the object property provider to be rendered
                            opp.SetFVRenderProperty();
                            break;
                        }
                    }
                }
            }

            // restore old camera setting
            m_Camera.rect = oldRect;
        }   

        static bool IsVisible(Renderer renderer, Camera camera)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obsCamera"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fvLength"></param>
        /// <returns name="float[]"></returns>
        public float[] ObservationToPropertyImage(Camera obsCamera, int width, int height, int fvLength)
        {
            // setup resulting property image array
            float[] propertyImage = new float[width * height * (fvLength + 4 - fvLength % 4)];
            
            // save current camera settings
            var oldRec = obsCamera.rect;
            var prevCameraRT = obsCamera.targetTexture;
            var oldCameraClearFlags = obsCamera.clearFlags;
            var oldCameraBackground = obsCamera.backgroundColor;
            // save current active render texture
            var prevActiveRT = RenderTexture.active;

            // setup camera
            obsCamera.rect = new Rect(0f, 0f, 1f, 1f);
            obsCamera.clearFlags = CameraClearFlags.SolidColor;
            obsCamera.backgroundColor = Color.black;
            obsCamera.enabled = false;
            // setup temporary render texture
            var rtFormat = RenderTextureFormat.ARGBFloat;
            var rtReadWrite = RenderTextureReadWrite.Linear;
            var rtDepth = 24;
            var tempRT =
                RenderTexture.GetTemporary(width, height, rtDepth, rtFormat, rtReadWrite);

            // setup texture to read the render result
            var texture2D = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

            // render to offscreen texture
            RenderTexture.active = tempRT;
            obsCamera.targetTexture = tempRT;

            // set default featureVector
            float[] defaultFV = new float[fvLength + 4 - fvLength % 4];
            m_defaultFeatureVector.CopyTo(defaultFV, 0);
            // render multiple times, until all properties of the feature vector
            // are read.
            for (int i = 0; i < fvLength; i += 4)
            {
                obsCamera.backgroundColor = new Color(defaultFV[i], defaultFV[i + 1], defaultFV[i + 2], defaultFV[i + 3]);
                Shader.SetGlobalInt("_StartIndex", i);
                obsCamera.RenderWithShader(featureVecShader, "RenderType");
                texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);
                texture2D.filterMode = FilterMode.Point;
                var values = texture2D.GetRawTextureData<float>();
                Unity.Collections.NativeArray<float>.Copy(values, 0, propertyImage, values.Length * i / 4, values.Length);
            }

            // reset all camera settings
            obsCamera.targetTexture = prevCameraRT;
            obsCamera.rect = oldRec;
            obsCamera.clearFlags = oldCameraClearFlags;
            obsCamera.backgroundColor = oldCameraBackground;
            obsCamera.enabled = true;
            RenderTexture.active = prevActiveRT;
            RenderTexture.ReleaseTemporary(tempRT);
            DestroyTexture(texture2D);

            return propertyImage;
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
            return new[] { height, width, fvd.GetFeatureVectorLength() };
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