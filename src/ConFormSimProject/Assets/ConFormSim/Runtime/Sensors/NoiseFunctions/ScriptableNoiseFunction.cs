using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Sensors
{
    public class ScriptableNoiseFunction : ScriptableObject
    {
        public virtual float Apply(float rawValue)
        {
            throw new NotImplementedException();
        }

        public virtual float[] Apply(float[] rawValues)
        {
            throw new NotImplementedException();
        }
    }
}