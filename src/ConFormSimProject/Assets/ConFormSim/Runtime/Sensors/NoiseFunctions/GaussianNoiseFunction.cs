using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Sensors
{
    [CreateAssetMenu(fileName = "GaussianNoise", menuName = "ConFormSim/Noise Function/Gaussian Noise", order = 1)]
    public class GaussianNoiseFunction : ScriptableNoiseFunction
    {
        public float amplitude = 1.0f;
        public override float Apply(float rawValue)
        {
            float absAmp = Mathf.Abs(amplitude);
            return rawValue * (1 + Utility.RandomGaussian(-absAmp, absAmp));
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
