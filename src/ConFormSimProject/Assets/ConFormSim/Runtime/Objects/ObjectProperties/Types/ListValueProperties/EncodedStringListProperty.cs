using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using System.Linq;

namespace ConFormSim.ObjectProperties
{
    [CreateAssetMenu(fileName = "EncodedStringListProperty", menuName = "ConFormSim/Object Properties/Encoded String List Property", order = 1)]
    public class EncodedStringListProperty : ObjectProperty
    {
        [SerializeField]
        private static bool m_isArrayType = true;
        public override bool isArrayType
        {
            get { return m_isArrayType; }
        }

        [Tooltip("Defines the length of the Code Book")]
        private int m_ArrayLength;
        public override int arrayLength
        {
            get 
            { 
                m_ArrayLength = codeBook.Length;
                return m_ArrayLength; 
            }
            set 
            { 
                // set new codebook length
                if(value != m_ArrayLength)
                {
                    string[] newCodeBook = new string[value];
                    for (int i = 0; i < Mathf.Min(m_ArrayLength, value); i++)
                    {
                        newCodeBook[i] = codeBook[i];
                    }
                    
                    codeBook = newCodeBook;
                }
                m_ArrayLength = value;
            }
        }

        public string[] stringValues;
        public string[] codeBook = new string[0];

        public override float[] GetFeatureVector()
        {
            float[] result = new float[m_ArrayLength];
            if (stringValues != null)
            {
                for(int i = 0; i < m_ArrayLength; i++)
                {
                    result[i] = stringValues.Contains(codeBook[i]) ? 1.0f : 0.0f;
                }
            }
            return result;
        }

        public override int length
        {
            get { return codeBook.Length; }
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                EncodedStringListProperty stringListProp = property as EncodedStringListProperty;
                stringValues = stringListProp.stringValues;
            }
        }

        public override void ApplySettings(ObjectPropertySettings settings)
        {
            arrayLength = settings.arrayLength;
            EncodedStringListProperty defaultProp = settings.defaultValue as EncodedStringListProperty;
            if(defaultProp != null)
            {
                this.codeBook = defaultProp.codeBook;
            }
            else{
                Debug.Log("Default value in settings is null. Cannot set code book");
            }
            
        }

    }
}