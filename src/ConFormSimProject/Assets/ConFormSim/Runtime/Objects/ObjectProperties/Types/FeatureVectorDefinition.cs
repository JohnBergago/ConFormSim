using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using RotaryHeart.Lib.SerializableDictionary;

namespace ConFormSim.ObjectProperties
{
    /// <summary>
    /// Scriptable Object that defines the properties included in a feature vector.
    /// This will be used in <see cref="ObjectPropertyProvider"/> objects and 
    /// <see cref="ObjectPropertyRequester"> objects to control and set object
    /// properties and feature vectors.  
    /// </summary>
    [CreateAssetMenu(fileName = "FeatureVecDefinition", menuName = "ConFormSim/Object Properties/Feature Vector Definition", order = 1)]
    public class FeatureVectorDefinition : ScriptableObject
    {
        /// <summary>
        /// The Dictionary that holds all the properties and the necessary settings.
        /// </summary>
        [Header("Object Feature Vector Definition")]
        [SerializeField]
        private ObjectPropertiesSettingsDictionary properties = 
            new ObjectPropertiesSettingsDictionary();

        public ObjectPropertiesSettingsDictionary Properties
        {
            get {return properties; }
        }

        /// <summary>
        /// A Bidirectional dictionary storing a mapping of property names and
        /// indexes, describing the posiiton in the defined feature vector.
        /// </summary>
        private Map<string, int> keyIndexMap = new Map<string, int>();
        
        /// <summary>
        /// Serializable keys of the <see cref="keyIndexmap"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private List<string> mapKeys = new List<string>();

        /// <summary>
        /// Serializable indices of the <see cref="keyIndexmap"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private List<int> mapValues = new List<int>();

        /// <summary>
        /// Length of the resulting feature vector from this definition.
        /// </summary>
        private int vectorLength = -1;
        
        /// <summary>
        /// Get length of feature vector resulting from this definition.
        /// </summary>
        /// <value></value>
        public int VectorLength
        {
            get
            {
                if (vectorLength < 0)
                {
                    vectorLength = GetFeatureVectorLength();
                }
                return vectorLength;
            }
        }
        

        /// <summary>
        /// The object property providers registered with this feature vector
        /// definition.
        /// </summary>
        /// <typeparam name="ObjectPropertyProvider"></typeparam>
        private HashSet<ObjectPropertyProvider> registeredOPPs= new HashSet<ObjectPropertyProvider>();

        /// <summary>
        /// Registers an object property provider on that feature vector
        /// definition.
        /// </summary>
        /// <param name="opp">The object property provider to be
        /// registered.</param>
        public void RegisterOPP(ObjectPropertyProvider opp)
        {
            registeredOPPs.Add(opp);
        }
        
        /// <summary>
        /// Property to return an array of all currently registered object
        /// property providers or adding new ones. It can be used to set
        /// multiple property providers at once.
        /// </summary>
        /// <value>
        /// Object property providers to be registered at this feature
        /// vector.
        /// </value>
        public ObjectPropertyProvider[] RegisteredOPPs
        {
            get
            {
                return registeredOPPs.ToArray<ObjectPropertyProvider>();
            } 
            set
            {
                foreach(ObjectPropertyProvider opp in value)
                {
                    registeredOPPs.Add(opp);
                }
            }
        }

        /// <summary>
        /// Removes an object property provider from the set of registered OPPs.
        /// </summary>
        /// <param name="opp">The objectproperty provider to be removed.</param>
        public void RemoveOPP(ObjectPropertyProvider opp)
        {
            registeredOPPs.Remove(opp);
        }

        /// <summary>
        /// Adds a new Property setting to the properties dictionary or updates it.
        /// </summary>
        /// <param name="name">Name of the property to add or update</param>
        /// <param name="settings">Settings value of the property.</param>
        public void AddPropertySettings(string name, ObjectPropertySettings settings)
        {
            properties[name] = settings;
            UpdateOrder();
        }

        /// <summary>
        /// Get the order of the properties in the feature vector.
        /// </summary>
        /// <returns>Order of the properties.</returns>
        public string[] GetPropertyOrder()
        {
            string[] keyOrder = new string[keyIndexMap.Forward.Keys.Count()];
            foreach(KeyValuePair<string, int> element in keyIndexMap)
            {
                keyOrder[element.Value] = element.Key;
            }
            return keyOrder;
        }

        /// <summary>
        /// Sets a new order for the properties in the final feature vector. This
        /// method is used to apply changes from the Editor. 
        /// </summary>
        /// <remarks>
        /// While setting a new order from code works, it is not recommended. The
        /// changes won't be applied by the editor and will be overwritten as soon
        /// as somehing in the editor changes.
        /// </remarks>
        /// <param name="orderedKeys">Keys in their new order. Keys that are not in
        /// the properties dictionary will be ignored. For missing keys a index is
        /// assigned automatically.</param>
        public void SetNewOrder(List<string> orderedKeys)
        {
            keyIndexMap.Clear();
            for(int i = 0; i < orderedKeys.Count; i++)
            {
                if (properties.ContainsKey(orderedKeys[i]))
                {
                    keyIndexMap.Add(orderedKeys[i], i);
                }
            }
            UpdateOrder();
        }

        /// <summary>
        /// Adds a new key to the <see cref="keyIndexmap"/> and assigns an index to it.
        /// </summary>
        /// <param name="key">Key to be added.</param>
        private void AddKeyToKeyIndexMap(string key)
        {
            if (!keyIndexMap.Forward.Contains(key))
            {
                for(int i = 0; i < properties.Keys.Count; i++)
                {
                    if (!keyIndexMap.Reverse.Contains(i))
                    {
                        keyIndexMap.Add(key, i);
                        return;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates entries in the <see cref="keyIndexmap"/> for all properties that
        /// are missing.
        /// </summary>
        public void UpdateOrder()
        {
            foreach (string key in properties.Keys)
            {
                AddKeyToKeyIndexMap(key);
            }
            SaveOrder();
        }

        /// <summary>
        /// Restores the <see cref="keyIndexmap"/> from the serializable lists 
        /// <see cref="mapKeys"/> and <see cref="mapValues"/>.
        /// </summary>
        void InitOrder()
        {
            for(int i = 0; i < mapKeys.Count; i++)
            {
                keyIndexMap.Add(mapKeys[i], mapValues[i]);
            }
        }

        /// <summary>
        /// Saves the <see cref="keyIndexmap"/> to serializable lists. That way the
        /// order stays consistent throughout sessions.
        /// </summary>
        void SaveOrder()
        {
            mapKeys = keyIndexMap.Forward.Keys.ToList();
            mapValues = keyIndexMap.Forward.Values.ToList();
        }

        /// <summary>
        /// Calculates the total length of the feature vector
        /// </summary>
        /// <returns>Length of the feature vector.</returns>
        private int GetFeatureVectorLength()
        {
            int length = 0;
            foreach(KeyValuePair<string, ObjectPropertySettings> element in properties)
            {
                length += element.Value.lengthInFeatureVector;
            }
            return length;
        }

        /// <summary>
        /// Get the default feature vector according to the default settings.
        /// </summary>
        /// <returns>The default feature vector for this definition.</returns>
        public List<float> GetDefaultFeatureVector()
        {
            List<float> result = new List<float>();
            foreach(string propertyName in GetPropertyOrder())
            {
                ObjectPropertySettings settings = Properties[propertyName];
                result.AddRange(settings.GetDefaultFeatureVector());
            }
            return result;
        }

        void Awake()
        {
            vectorLength = -1;
        }

        void OnEnable()
        {
            InitOrder();
            UpdateAllSettings();
        }

        public void UpdateAllSettings()
        {
            foreach(KeyValuePair<string, ObjectPropertySettings> element in properties)
            {
                element.Value.UpdateSettings();
            }
        }

        void OnDisable()
        {
            SaveOrder();
            SaveDefaultsToAsset();
        }

        public void ElementAdded(ReorderableList list)
        {
            ObjectPropertySettings[] listValues = properties.Values.ToArray();
            Debug.Log(listValues.Length + " elements in list");
            properties[properties.Keys.ToArray()[properties.Count - 2]] = new ObjectPropertySettings(properties.Values.ToArray()[properties.Count - 2].type, true);
            properties[properties.Keys.ToArray()[properties.Count - 1]] = new ObjectPropertySettings(properties.Values.ToArray()[properties.Count - 1].type, true);
            Debug.Log("element added");
            SaveDefaultsToAsset();
        }
        public void SaveDefaultsToAsset()
        {
            UpdateAllSettings();
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // first find out where our object is stored
                string fvdAssedPath = AssetDatabase.GetAssetPath(this);
                foreach(KeyValuePair<string, ObjectPropertySettings> element in properties)
                {
                    ObjectProperty defaultProp = element.Value.defaultValue;
                    if(defaultProp != null)
                    {
                        defaultProp.name = "default_" + element.Key;
                        defaultProp.hideFlags = HideFlags.HideInHierarchy;
                        if (!AssetDatabase.Contains(defaultProp))
                        {
                            AssetDatabase.AddObjectToAsset(defaultProp, fvdAssedPath);
                        }
                        //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((ObjectProperty)defaultProp));
                        // Debug.Log("saved "+ defaultProp.name);
                    }
                }
                AssetDatabase.Refresh();
                // foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(fvdAssedPath))
                // {
                //     if(AssetDatabase.IsSubAsset(obj))
                //     {
                //         DestroyImmediate(obj, true);
                //     }
                // }
            }
            #endif
        }
    }
}