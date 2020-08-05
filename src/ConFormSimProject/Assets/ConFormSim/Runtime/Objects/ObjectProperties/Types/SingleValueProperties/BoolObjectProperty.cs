using UnityEngine;

namespace ConFormSim.ObjectProperties
{
    [CreateAssetMenu(fileName = "BoolProperty", menuName = "ConFormSim/Object Properties/Bool Property", order = 1)]
    public class BoolObjectProperty : ObjectProperty
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

        public bool value;

        public override float[] GetFeatureVector()
        {
            return new float[] { value ? 1.0f : 0f };
        }

        public override int length
        {
            get { return 1; }
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                BoolObjectProperty boolProp = property as BoolObjectProperty;
                this.value = boolProp.value;
            }
        }
    }
}