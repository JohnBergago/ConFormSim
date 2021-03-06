using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RotaryHeart.Lib.SerializableDictionary;
using Unity.MLAgents;

namespace ConFormSim.ObjectProperties
{
    [System.Serializable]
    public class NamedObjectProperty
    {
        [HideInInspector]
        public string name;

        [ExtendableScriptableObject(true, true, false, new string[]{"values", "codeBook"}, false, true)]
        public ObjectProperty property;

        public NamedObjectProperty(string name, ObjectProperty propertySO=null)
        {
            this.name = name;
            this.property = propertySO;
        }
    }

    [System.Serializable]
    public class StringNamedPropDict:SerializableDictionaryBase<string, NamedObjectProperty>{}

    /// <summary>
    /// This class provides object properties to GameObjects. Property instances can
    /// be set and retrieved via this class. 
    /// </summary>
    public class ObjectPropertyProvider : MonoBehaviour
    {
        [SerializeField]
        private FeatureVectorDefinition availableProperties;

        /// <summary>
        /// The Scriptable Object that holds the available property types and their
        /// settings. This defines the feature vector that is returned.
        /// </summary>
        public FeatureVectorDefinition AvailableProperties
        {
            get { return availableProperties; }
            set { availableProperties = value; Reset(); }
        }
        
        /// <summary>
        /// The ObjectPropertyCamera calls <see cref="SetFVRenderProperty"/>
        /// with every sensor step in order to update all render materials
        /// belonging to this property provider. If the number of child objects
        /// with renderers changes in between steps, enable this flag. Otherwise
        /// disable it to improve performance. 
        /// </summary>
        public bool updateChildRenderers = false;

        /// <summary>
        /// All renderers belonging to this object property provider. The list
        /// will be filled on Start(). If the number of renderers (child objects
        /// of the object property provider gameobject) changes during runtime,
        /// enable <see cref="updateChildRenderers"/>.
        /// </summary>
        private List<Renderer> oppRenderers = new List<Renderer>();

        /// <summary>
        /// This List will be used to display the object properties in the Unity
        /// Inspector. 
        /// </summary>
        /// <typeparam name="NamedObjectProperty">The object that will be shown.</typeparam>
        [SerializeField]
        private List<NamedObjectProperty> propertyList = new List<NamedObjectProperty>();

        /// <summary>
        /// The dictionary that prevents properties to be added twice.
        /// </summary>
        /// <typeparam name="string"></typeparam>
        /// <typeparam name="NamedObjectProperty"></typeparam>
        [SerializeField, HideInInspector]
        private StringNamedPropDict m_PropertyDict = new StringNamedPropDict();

        [SerializeField, HideInInspector]
        private StringNamedPropDict m_runtimePropertyDict = new StringNamedPropDict();
        
        private int currentTotalStepCount;
        private List<float> currentStepFeatureVector;
        public StringNamedPropDict PropertyDictionary
        {
            get
            {
                // when playing use runtime dictionary
                if (Application.isPlaying)
                {
                    // if the runtime dict is empty fill it with values from the
                    // init dict
                    if (m_runtimePropertyDict.Count == 0)
                    {
                        foreach(KeyValuePair<string, NamedObjectProperty> element in m_PropertyDict)
                        {
                            if (element.Value.property != null)
                            {
                                ObjectProperty runtimeProp = (ObjectProperty) Instantiate(element.Value.property);
                                runtimeProp.SetValue(element.Value.property);
                                NamedObjectProperty namedRuntimeProp = new NamedObjectProperty(element.Key, runtimeProp);
                                m_runtimePropertyDict.Add(element.Key, namedRuntimeProp);
                            }
                            else 
                            {
                                NamedObjectProperty namedRuntimeProp = new NamedObjectProperty(element.Key, null);
                                m_runtimePropertyDict.Add(element.Key, namedRuntimeProp);
                            }
                        }
                    }
                    return m_runtimePropertyDict;
                }
                else
                {
                    m_runtimePropertyDict.Clear();
                    return m_PropertyDict;
                }
            }
            set
            {
                if (Application.isPlaying)
                    m_runtimePropertyDict = value;
                else
                    m_PropertyDict = value;
            }
        }

        void Awake()
        {
            // so in case this object was copied, we need to create new Instances
            // for the runtime property dict. That way each object can keep it's own
            // values.
            if (Application.isPlaying)
            {
                StringNamedPropDict newDict = new StringNamedPropDict();
                foreach(KeyValuePair<string, NamedObjectProperty> element in PropertyDictionary)
                {
                    NamedObjectProperty copy = new NamedObjectProperty(element.Key, (ObjectProperty) Instantiate(element.Value.property));
                    copy.property.SetValue(element.Value.property);
                    copy.property.SetValueFromGameObject(this.gameObject);
                    ObjectPropertySettings settings;
                    if (availableProperties.Properties.TryGetValue(element.Key, out settings))
                    {
                        copy.property.ApplySettings(settings);
                    }
                    newDict.Add(element.Key, copy);
                }
                m_runtimePropertyDict = newDict;
            }
        }

        public void Start()
        {
            // register this object to the corresponding feature vector
            // definition
            availableProperties.RegisterOPP(this);
            GetChildRenderers();
        }

        public void OnDestroy()
        {
            // when this provider is destroyed unregister from the feature
            // vector definition.
            availableProperties.RemoveOPP(this);
        }

        public void Reset()
        {
            // delete all property assets
            foreach(NamedObjectProperty nOnjProp in propertyList)
            {
                DestroyImmediate(nOnjProp.property, true);
            }
            PropertyDictionary.Clear();
        }

        /// <summary>
        /// This method updates the <see cref="m_PropertyDict"/> and <see
        /// cref="propertyList"/> according to changes on <see
        /// cref="availableProperties" />.
        /// </summary>
        public void UpdateProperties()
        {
            propertyList = PropertyDictionary.Values.ToList();
            SaveDefaultsToAsset();
        }

        /// <summary>
        /// Sets the Feature Vector as a material property block in
        /// attached renderers of the GameObjects and possible child objects, except
        /// they have a <see cref="ObjectPropertyProvider"/> attached to themselves.
        /// </summary>
        public void SetFVRenderProperty()
        {
            // retrieve all relevant properties           
            int fvLength = availableProperties.VectorLength;
            List<float> featureVector = GetFeatureVector();
            int fvInstanceID = availableProperties.GetInstanceID();

            // set material property block for all renders belonging to this
            // object property provider
             MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            if(updateChildRenderers)
            {
                GetChildRenderers();
            }
            for(int i = 0; i < oppRenderers.Count; i++)
            {
                if (oppRenderers[i].HasPropertyBlock())
                {
                    oppRenderers[i].GetPropertyBlock(mpb);
                }
                mpb.SetInt("_FeatureVectorLength", fvLength);
                mpb.SetFloatArray("_FeatureVector", featureVector);  
                mpb.SetInt("_FVDefinitionID", fvInstanceID);
                oppRenderers[i].SetPropertyBlock(mpb);
            }
        }

        void GetChildRenderers()
        {
            oppRenderers.Clear();
            Transform[] childs = new Transform[transform.childCount + 1];
            childs[0] = transform;
            for(int i = 1; i <= transform.childCount; i++)
                childs[i] = transform.GetChild(i-1);
            foreach(Transform child in childs)
            {     
                // if a child has an object property provider itself, don't add
                // it to the list
                ObjectPropertyProvider opp;
                if (!child.gameObject.TryGetComponent<ObjectPropertyProvider>(out opp) || child == transform)
                {
                    // find the renderer of the child
                    Renderer renderer;
                    if (child.gameObject.TryGetComponent<Renderer>(out renderer))
                    {
                        oppRenderers.Add(renderer);
                    }
                }
            }
        }

        public List<float> GetFeatureVector()
        {
            // if the last time we updated the feature vector was in a past
            // step, perform update
            if(Academy.Instance.TotalStepCount != currentTotalStepCount)
            {
                List<float> fv = new List<float>(availableProperties.VectorLength);
                foreach(string propertyName in availableProperties.GetPropertyOrder())
                {
                    ObjectProperty prop;
                    if (TryGetObjectProperty(propertyName, out prop))
                    {
                        fv.AddRange(prop.GetFeatureVector());
                        // Debug.Log("found prop: " + propertyName + " fv_length: " +  prop.arrayLength);
                    }
                    else
                    {
                        ObjectPropertySettings settings = availableProperties.Properties[propertyName];
                        fv.AddRange(settings.GetDefaultFeatureVector());
                        // Debug.Log("No prop: " + propertyName  + " fv_length: " +  settings.defaultValue.length);
                    }
                }
                currentStepFeatureVector = fv;
            }            
            return currentStepFeatureVector;
        }
        
        /// <summary>
        /// Try to get an <see cref="ObjectProperty"/> instance.
        /// </summary>
        /// <param name="name">The name of the property to find.</param>
        /// <param name="property">The variable to store the property in case of
        /// success. Otherwise it will be null.</param>
        /// <returns>True if the property exists, else false.</returns>
        public bool TryGetObjectProperty(string name, out ObjectProperty property)
        {
            NamedObjectProperty namedObjectProperty;
            if (PropertyDictionary.TryGetValue(name, out namedObjectProperty))
            {
                property = namedObjectProperty.property;
                if(property != null)
                {
                    // in case the property updates based on the game object set the
                    // value
                    property.SetValueFromGameObject(this.gameObject);
                }
                return true;
            }
            property = null;
            return false;
        }
        
        /// <summary>
        /// Get an ObjectProperty by its name. 
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <returns>The ObjectProperty for name or null if it does not
        /// exist.</returns> 
        public ObjectProperty GetObjectProperty(string name)
        {
            ObjectProperty objectProperty = null;
            TryGetObjectProperty(name, out objectProperty);
            return objectProperty;
        }

        /// <summary>
        /// Add or updates a property in <see cref="m_PropertDict"/> if a settings
        /// entry in <see cref="availableProperties"/> exists. Otherwise a warning
        /// exception will be thrown.
        /// </summary>
        /// <param name="name">Name of the property to add.</param>
        /// <param name="property">Property Object to be added.</param>
        public void SetObjectProperty(string name, ObjectProperty property)
        {
            // Check if the property is valid and settings exist.
            if (availableProperties.Properties.ContainsKey(name))
            {
                // Apply settings to the new property
                ObjectPropertySettings settings = availableProperties.Properties[name];
                property.ApplySettings(settings);
                NamedObjectProperty newProp = new NamedObjectProperty(name, property);
                PropertyDictionary[name] = newProp;
                propertyList = PropertyDictionary.Values.ToList();
            }
            else
            {
                throw new WarningException("There is no settings entry for the key "
                    + name + " in AvailableProperties. Cannot add property.");
            }
        }

        public void SaveDefaultsToAsset()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
            {
                // first find out where our object is stored
                string prefabPath = AssetDatabase.GetAssetPath(this);
                foreach(KeyValuePair<string, NamedObjectProperty> element in PropertyDictionary)
                {
                    ObjectProperty prop = element.Value.property;
                    element.Value.property.hideFlags = HideFlags.HideInHierarchy;
                    if(prop != null)
                    {
                        prop.name = gameObject.name + "_" + element.Key;
                        prop.hideFlags = HideFlags.HideInHierarchy;
                        if (!AssetDatabase.Contains(prop))
                        {
                            AssetDatabase.AddObjectToAsset(prop, prefabPath);
                        }      
                    }
                }   
                // foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(prefabPath))
                // {
                //     if(AssetDatabase.IsSubAsset(obj))
                //     {
                //         DestroyImmediate(obj, true);
                //     }
                // }
                AssetDatabase.Refresh();
            }
            #endif
        }

    }
}