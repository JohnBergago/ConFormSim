using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

namespace ConFormSim.ObjectProperties
{
    [System.Serializable]
    public class CodeDict : SerializableDictionaryBase<string, int> {};

    [CreateAssetMenu(fileName = "EncodedStringProperty", menuName = "ConFormSim/Object Properties/Encoded String Property", order = 1)]
    public class EncodedStringProperty : ObjectProperty
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
                if(value != codeBook.Length)
                {
                    string[] newArray = new string[value];
                    for (int i = 0; i < Mathf.Min(codeBook.Length, value); i++)
                    {
                        newArray[i] = codeBook[i];
                    }
                    
                    codeBook = newArray;
                }
                m_ArrayLength = value;
            }
        }

        public string value;
        public string[] codeBook = new string[0];

        public override float[] GetFeatureVector()
        {
            float[] result = new float[m_ArrayLength];
            for(int i = 0; i < m_ArrayLength; i++)
            {
                result[i] = codeBook[i] == value ? 1.0f : 0.0f;
            }
            return result;
        }

        public override int length
        {
            get { return m_ArrayLength; }
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                EncodedStringProperty stringProp = property as EncodedStringProperty;
                this.value = stringProp.value;
            }
        }

        public override void ApplySettings(ObjectPropertySettings settings)
        {
            arrayLength = settings.arrayLength;
            EncodedStringProperty defaultProp = settings.defaultValue as EncodedStringProperty;
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