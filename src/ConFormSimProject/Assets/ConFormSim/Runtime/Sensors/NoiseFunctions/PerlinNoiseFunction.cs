using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Sensors
{
    [CreateAssetMenu(fileName = "PerlinNoise", menuName = "ConFormSim/Noise Function/Perlin Noise", order = 1)]
    public class PerlinNoiseFunction : ScriptableNoiseFunction
    {
        public int pixWidth;
        public int pixHeight;

        public float xOrg;
        public float yOrg;

        private float[,] noiseArray;
        private float x = 0.0f;
        private float y = 0.0f;
        public float scale = 1.0f;

        public void OnEnable()
        {

        }
        public override float Apply(float rawValue)
        {
            float noisy = 0;
            if (x >= pixWidth)
            {
                x = 0.0f;
                y ++;
            }
            if (y >= pixHeight)
            {
                y = 0.0f;
            }
            
            float xCoord = xOrg + x / pixWidth * scale;
            float yCoord = yOrg + y / pixHeight * scale;
            float sample = Mathf.PerlinNoise(Time.time * scale + y*pixWidth +x , 0);
            noisy = rawValue + (2f*(sample  + 1f)/2.0f - 1f) * scale;
            x++;
            return noisy;
        }

        public override float[] Apply(float[] rawValues)
        {
            float[] noisy = new float[rawValues.Length];
            for(int i = 0; i < rawValues.Length; i++)
            {
                noisy[i] = Apply(rawValues[i]);
            }
            return noisy;
        }
    }
}
