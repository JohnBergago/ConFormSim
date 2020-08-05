using UnityEngine;

namespace ConFormSim.ObjectProperties
{
    [CreateAssetMenu(fileName = "FloatProperty", menuName = "ConFormSim/Object Properties/Float Property", order = 1)]
    public class FloatObjectProperty : ObjectProperty
    {
        private static bool m_isArrayType = false;
        public override bool isArrayType
        {
            get { return m_isArrayType; }
        }

        public override int arrayLength
        {
            get {return 0; }
            set { }
        }

        public float value;

        public override float[] GetFeatureVector()
        {
            return new float[] { value };
        }

        public override int length
        {
            get { return 1; }
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                FloatObjectProperty floatProp = property as FloatObjectProperty;
                this.value = floatProp.value;
            }
        }
    }
}