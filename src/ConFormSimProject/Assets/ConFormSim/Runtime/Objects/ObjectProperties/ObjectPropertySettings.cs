using UnityEngine;
using UnityEditor;
using RotaryHeart.Lib.SerializableDictionary;

namespace ConFormSim.ObjectProperties
{

    [System.Serializable]
    public class ObjectPropertiesSettingsDictionary : SerializableDictionaryBase<string, ObjectPropertySettings> {}

    /// <summary>
    /// Class containing object property settings, defining the kind of Object
    /// Property and their length (in case of list/array types).
    /// </summary>
    [System.Serializable]
    public class ObjectPropertySettings
    {
        /// <summary>
        /// The ObjectProperty that will be used as template in the <see
        /// cref="ObjectPropertyProvider"/> objects.
        /// </summary>
        public ObjectProperty type;

        [ExtendableScriptableObject(false, false, false, new string[]{"values", "codeBook"}, false, true)]
        public ObjectProperty defaultValue;

        private float[] defaultFeatureVector;
        private bool isPlaying;

        /// <summary>
        /// The number of values a list type can contain. This will usually have no
        /// effect if <see cref="type"/> is not a list property.
        /// </summary>
        public int arrayLength;

        /// <summary>
        /// The length of the feature vector that represents a property based on the
        /// property type and length.
        /// </summary>
        public int lengthInFeatureVector
        {
            get { return CalculateFeatureLength(); }
        }

        private bool copyDefault = false;

        public ObjectPropertySettings()
        {
            UpdateSettings();
        }

        public ObjectPropertySettings(ObjectProperty templateType, bool copyDefault = true)
        {
            this.copyDefault = copyDefault;
            this.type = templateType;
            UpdateSettings();
        }
        /// <summary>
        /// Calculates the length of the feature vector for a object proeprty with
        /// this type and length settings.
        /// </summary>
        /// <returns></returns>
        private int CalculateFeatureLength()
        {
            if (type != null && defaultValue != null)
            {
                defaultValue.ApplySettings(this);
                return defaultValue.length;
            }
            return 0;
        }

        /// <summary>
        /// In case a property does not exist on an object but is required for a
        /// feature vector this default vector will be returned.
        /// </summary>
        /// <returns>Feature vector filled with default values.</returns>
        public float[] GetDefaultFeatureVector()
        {
            if (defaultFeatureVector != null && defaultFeatureVector.Length > 0 && isPlaying)
            {
                return defaultFeatureVector;
            }
            
            if (defaultValue != null)
            {
                defaultValue.ApplySettings(this);
                defaultFeatureVector = defaultValue.GetFeatureVector();
                return defaultFeatureVector;
            }
            if (Application.isPlaying)
            {
                isPlaying = true;
            }
            else
            {
                isPlaying = false;
            }

            return new float[0];
        }

        public void UpdateSettings()
        {
            if (type != null)
            {
                if (defaultValue == null)
                {
                    defaultValue = ScriptableObject.CreateInstance(type.GetType()) as ObjectProperty;
                }
                else if (defaultValue.GetType() != type.GetType())
                {
                    Object.DestroyImmediate(defaultValue, true);
                    defaultValue = ScriptableObject.CreateInstance(type.GetType()) as ObjectProperty;
                }
                else if (copyDefault)
                {
                    Debug.Log("Create copy of existing");
                    defaultValue = Object.Instantiate(defaultValue);
                    copyDefault = false;
                }
                defaultValue.ApplySettings(this);
            }
            else
            {
                defaultValue = null;
            }
        }
    }
}