using UnityEngine;

namespace ConFormSim.ObjectProperties
{
    [CreateAssetMenu(fileName = "BoolListProperty", menuName = "ConFormSim/Object Properties/Bool List Property", order = 1)]
    public class BoolListObjectProperty : ObjectProperty
    {
        private static bool m_isArrayType = true;
        public override bool isArrayType
        {
            get { return m_isArrayType; }
        }

        private int m_ArrayLength;
        public override int arrayLength
        {
            get 
            { 
                m_ArrayLength = values.Length;
                return m_ArrayLength; 
            }
            set 
            { 
                if(value != m_ArrayLength)
                {
                    bool[] newArray = new bool[value];
                    for (int i = 0; i < Mathf.Min(m_ArrayLength, value); i++)
                    {
                        newArray[i] = values[i];
                    }
                    m_ArrayLength = value;
                    values = newArray;
                }
            }
        }

        public bool[] values = new bool[0];

        public override float[] GetFeatureVector()
        {
            float[] result = new float[values.Length];
            for(int i = 0; i < values.Length; i++)
            {
                result[i] = values[i] ? 1.0f : 0.0f;
            }
            return result;
        }

        public override int length
        {
            get { return m_ArrayLength; }
        }

        public override void ApplySettings(ObjectPropertySettings settings)
        {
            arrayLength = settings.arrayLength;
            BoolListObjectProperty defaultProp = settings.defaultValue as BoolListObjectProperty;
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                BoolListObjectProperty boolProp = property as BoolListObjectProperty;
                for (int i = 0; i < arrayLength; i++)
                {
                    this.values[i] = boolProp.values[i];
                }
            }
        }
    }
}