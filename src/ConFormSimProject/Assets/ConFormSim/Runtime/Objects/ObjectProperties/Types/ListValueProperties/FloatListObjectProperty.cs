using UnityEngine;

namespace ConFormSim.ObjectProperties
{
    [CreateAssetMenu(fileName = "FloatListProperty", menuName = "ConFormSim/Object Properties/Float List Property", order = 1)]
    public class FloatListObjectProperty : ObjectProperty
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
                    float[] newArray = new float[value];
                    for (int i = 0; i < Mathf.Min(m_ArrayLength, value); i++)
                    {
                        newArray[i] = values[i];
                    }
                    m_ArrayLength = value;
                    values = newArray;
                }
            }
        }

        public float[] values = new float[0];

        public override float[] GetFeatureVector()
        {
            return values;
        }

        public override int length
        {
            get { return m_ArrayLength; }
        }

        public override void ApplySettings(ObjectPropertySettings settings)
        {
            arrayLength = settings.arrayLength;
            FloatListObjectProperty defaultProp = settings.defaultValue as FloatListObjectProperty;
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                FloatListObjectProperty floatListProp = property as FloatListObjectProperty;
                for (int i = 0; i < arrayLength; i++)
                {
                    this.values[i] = floatListProp.values[i];
                }
            }
        }
    }
}