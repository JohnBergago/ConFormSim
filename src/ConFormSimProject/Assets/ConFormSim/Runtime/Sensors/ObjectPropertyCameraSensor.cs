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
        bool m_Grayscale;
        string m_Name;
        int[] m_Shape;
        SensorCompressionType m_CompressionType;

        /// <summary>
        /// debug texture in order to see the id separation
        /// </summary>
        public RawImage debugImg;
        private int featureLayer;

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

        private ScriptableNoiseFunction noiseFunc;

        public static Shader uberReplacementShader;

        /// <summary>
        /// The requester object, that retrieves properties from observed
        /// objects. It will be passed from the component to the constructor.
        /// </summary>
        ObjectPropertyRequester m_PropertyRequester;

        Dictionary<int, GameObject> IDtoObjectDict = new Dictionary<int, GameObject>();
        Dictionary<int, float[]> IDToFeatureVecDict = new Dictionary<int, float[]>();

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
            Camera camera, int width, int height, bool grayscale, string name, 
            FeatureVectorDefinition fvd, ScriptableNoiseFunction noiseFunc = null, 
            RawImage image=null)
        {
            m_Camera = camera;
            m_Width = width;
            m_Height = height;
            m_Grayscale = grayscale;
            m_Name = name;
            m_PropertyRequester = new ObjectPropertyRequester(fvd);
            m_Shape = GenerateShape(width, height, fvd);
            debugImg = image;
            this.noiseFunc = noiseFunc;

            if(!uberReplacementShader)
                uberReplacementShader = Shader.Find("Hidden/UberReplacement");

            UpdateDictionaries();
        }

        private void UpdateDictionaries()
        {
            IDtoObjectDict.Clear();
            foreach (ObjectPropertyProvider obj in Object.FindObjectsOfType<ObjectPropertyProvider>())
            {
                IDtoObjectDict[obj.gameObject.GetInstanceID()] = obj.gameObject;
            }
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
        /// <param name="adapter">Where the observation is written to.</param>
        /// <returns></returns>
        public int Write(ObservationWriter adapter)
        {
            var texture = ObservationToTexture(m_Camera, m_Width, m_Height);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            Texture2D debugTex = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false);
            //debugImg.texture = texture;
            float[,,] features = new float[m_Height, m_Width, m_PropertyRequester.FeatureVectorLength];
            Color[] debugImgPixels = new Color[m_Height * m_Width];   

            // remove old feature vectors and fill new for this frame
            IDToFeatureVecDict.Clear();
            // for debug img we go through from bottom to top
            for (int row = texture.height - 1; row >= 0; row--)
            {
                // from left to right
                for(int col = 0; col < texture.width; col++)
                {
                    int id = ObjectIDColorEncoding.ColorToID((Color32)texture.GetPixel(col, row));
                    // check if we already have a vector for this id
                    float[] featureVec;
                    if (!IDToFeatureVecDict.TryGetValue(id, out featureVec))
                    {
                        // if there is no entry, we need to request it from the
                        // object
                        if(!IDtoObjectDict.ContainsKey(id) && id != 0)
                        {
                            UpdateDictionaries();
                        }

                        GameObject obj;
                        if (IDtoObjectDict.TryGetValue(id, out obj))
                        {
                            featureVec = m_PropertyRequester.RequestProperties(obj);
                        }
                        else
                        {
                            featureVec = m_PropertyRequester.featureVectorDefinition.GetDefaultFeatureVector();
                        }
                        IDToFeatureVecDict.Add(id, featureVec);
                    }  

                    for( int i = 0; i < featureVec.Length; i++)
                    {
                        //adapter.AddRange(featureVec, col + row * m_Width);
                        // we need to flip the feature, otherwise it's upside
                        // down. Apply noise while we're at it. 
                        // if (noiseFunc != null)
                        // {
                        //     featureVec[i] = noiseFunc.Apply(featureVec[i]);
                        // }
                        
                        adapter[m_Height - row - 1, col, i] = featureVec[i];
                    }

                    if (featureLayer < featureVec.Length)
                    {
                        if (noiseFunc != null)
                        {
                            float noisyVal = noiseFunc.Apply(featureVec[featureLayer]);
                            debugImgPixels[col + row * m_Width] = new Color(
                                noiseFunc.Apply(featureVec[featureLayer]), 
                                noiseFunc.Apply(featureVec[(featureLayer) % featureVec.Length]), 
                                noiseFunc.Apply(featureVec[(featureLayer) % featureVec.Length]));
                        }
                        else {
                            debugImgPixels[col + row * m_Width] = new Color(
                            featureVec[featureLayer], 
                            featureVec[(featureLayer) % featureVec.Length], 
                            featureVec[(featureLayer) % featureVec.Length]);
                        }
                        
                    }
                }
            }
            if (debugImg != null)
            {
                debugTex.SetPixels(debugImgPixels);
                debugTex.filterMode = FilterMode.Point;
                debugTex.Apply();
                debugImg.texture = debugTex; 
            }

            DestroyTexture(texture);
            return m_Width * m_Height;
        }

        /// <inheritdoc/>
        public void Update() {}

        /// <inheritdoc/>
        public SensorCompressionType GetCompressionType()
        {
            return SensorCompressionType.None;
        }

        /// <summary>
        /// Renders a Camera instance to a 2D texture at the corresponding resolution.
        /// </summary>
        /// <returns>The 2D texture.</returns>
        /// <param name="obsCamera">Camera.</param>
        /// <param name="width">Width of resulting 2D texture.</param>
        /// <param name="height">Height of resulting 2D texture.</param>
        /// <returns name="texture2D">Texture2D to render to.</returns>
        public static Texture2D ObservationToTexture(Camera obsCamera, int width, int height)
        {
            var texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var oldRec = obsCamera.rect;
            obsCamera.rect = new Rect(0f, 0f, 1f, 1f);
            var depth = 24;
            var format = RenderTextureFormat.Default;
            var readWrite = RenderTextureReadWrite.Default;

            var tempRt =
                RenderTexture.GetTemporary(width, height, depth, format, readWrite);

            var prevActiveRt = RenderTexture.active;
            var prevCameraRt = obsCamera.targetTexture;

            // render to offscreen texture (readonly from CPU side)
            RenderTexture.active = tempRt;
            obsCamera.targetTexture = tempRt;

            obsCamera.RenderWithShader(uberReplacementShader, "");

            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);

            obsCamera.targetTexture = prevCameraRt;
            obsCamera.rect = oldRec;
            RenderTexture.active = prevActiveRt;
            RenderTexture.ReleaseTemporary(tempRt);
            return texture2D;
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

        public void SetFeatureLayer(int layer)
        {
            if (layer < m_PropertyRequester.featureVectorDefinition.GetFeatureVectorLength())
            {
                featureLayer = layer; 
            }
        }

        public void Reset()
        {
            
        }

    }
}