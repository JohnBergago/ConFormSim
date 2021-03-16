using UnityEngine;

namespace ConFormSim.ObjectProperties
{
    [CreateAssetMenu(fileName = "ColorProperty", menuName = "ConFormSim/Object Properties/Color Property", order = 1)]
    public class ColorObjectProperty : ObjectProperty
    {
        private static bool m_isArrayType = false;
        public override bool isArrayType
        {
            get { return m_isArrayType; }
        }

        public override int arrayLength
        {
            get {return 0; }
            set {}
        }

        public Color value = new Color();
        public bool useAlphaChannel;
        public bool useObjectRenderer;

        [Tooltip("Sets the color of the object within a range +- the original value.")]
        public float instanceNoise;
        public bool noiseApplied = false;

        public override float[] GetFeatureVector()
        {  
            if (useAlphaChannel)
            {
                return new float[] { value.r, value.g, value.b, value.a };
            }
            return new float[] { value.r, value.g, value.b };
        }

        public override int length
        {
            get { return useAlphaChannel ? 4 : 3; }
        }

        public override void SetValue(ObjectProperty property)
        {
            if (property.GetType() == this.GetType())
            {
                ColorObjectProperty colorProp = property as ColorObjectProperty;
                this.value = colorProp.value;
                ApplyNoise();
            }
        }

        private void ApplyNoise()
        {
            // convert to HSV
            float H, S, V;
            Color.RGBToHSV(this.value, out H, out S, out V);
            this.value = Color.HSVToRGB(
                H + Random.Range(-Mathf.Abs(instanceNoise), Mathf.Abs(instanceNoise)), 
                S, 
                V);
        }
        public override void SetValueFromGameObject(GameObject attachedGameObject)
        {
            if (useObjectRenderer)
            {
                // try to set the color the same as in the renderers material
                Renderer renderer;
                if (attachedGameObject.TryGetComponent<Renderer>(out renderer))
                {
                    Color color = Color.white;
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    bool noColorProp = true;
                    if (renderer.HasPropertyBlock())
                    {
                        renderer.GetPropertyBlock(mpb);
                        color = mpb.GetColor("_Color");
                        if (!color.Equals(new Color(0,0,0,0)))
                        {
                            noColorProp = false;
                        }
                    }
                    if (noColorProp)
                    {
                        color = renderer.sharedMaterial.GetColor("_Color");
                    }
                    // if noise was applied before, but the color has changed
                    // meanwhile, apply noise again
                    if (value != color)
                    {
                        value = color;
                        ApplyNoise();
                        mpb.SetColor("_Color", this.value);
                        renderer.SetPropertyBlock(mpb);
                    }
                    // Debug.Log("Set color for " + attachedGameObject.name + "
                    // to (" + value.r + ", "+ value.g + ", " + value.b + ")");

                }
                else
                {
                    Debug.Log("The GameObject doesn't have a Renderer. Thus cannot retrieve color property.");
                }
            }
        }
    }
}