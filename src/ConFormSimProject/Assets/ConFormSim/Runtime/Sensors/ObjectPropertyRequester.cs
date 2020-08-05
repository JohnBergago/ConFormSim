using System.Collections.Generic;
using UnityEngine;
using ConFormSim.ObjectProperties;

namespace ConFormSim.Sensors
{

    [System.Serializable]
    public class ObjectPropertyRequester
    {
        /// <summary>
        /// Definition of the feature vector that will be produced by that requester.
        /// </summary>
        public FeatureVectorDefinition featureVectorDefinition;
        public int FeatureVectorLength
        {
            get 
            { 
                if (featureVectorDefinition != null)
                    return featureVectorDefinition.GetFeatureVectorLength();
                return 0;
            }
        }

        public ObjectPropertyRequester(FeatureVectorDefinition fvd)
        {
            featureVectorDefinition = fvd;
        }

        public float[] RequestProperties(GameObject obj)
        {
            List<float> result = new List<float>(FeatureVectorLength);
            bool createDefaultVector = false;

            // first try to get the ObjectPropertyProvider of that object
            ObjectPropertyProvider objectPropertyProvider;
            if (obj.TryGetComponent<ObjectPropertyProvider>(out objectPropertyProvider))
            {
                if (objectPropertyProvider.AvailableProperties == featureVectorDefinition 
                    && featureVectorDefinition != null)
                {
                    foreach(string propertyName in featureVectorDefinition.GetPropertyOrder())
                    {
                        
                        ObjectProperty prop;
                        if (objectPropertyProvider.TryGetObjectProperty(propertyName, out prop))
                        {
                            result.AddRange(prop.GetFeatureVector());
                            // Debug.Log("found prop: " + propertyName + " fv_length: " +  prop.arrayLength);
                        }
                        else
                        {
                            ObjectPropertySettings settings = featureVectorDefinition.Properties[propertyName];
                            result.AddRange(settings.GetDefaultFeatureVector());
                            // Debug.Log("No prop: " + propertyName  + " fv_length: " +  settings.defaultValue.length);
                        }
                    }
                }
                else
                {
                    createDefaultVector = true;
                    Debug.Log("The feature vector definitions of " + obj.name +
                    " and sensor are different. Will use default Vector.");
                }
            }
            else
            {
                createDefaultVector = true;
                Debug.Log("Obj. doesn't have a Object Property Provider. Will use default.");
            }

            // create complete vector from default values
            if (createDefaultVector)
            {
                return featureVectorDefinition.GetDefaultFeatureVector();
            }
            return result.ToArray();
        }
    }
}