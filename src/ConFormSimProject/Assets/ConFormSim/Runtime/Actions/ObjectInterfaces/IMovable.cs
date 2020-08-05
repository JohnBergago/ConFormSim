using UnityEngine;

namespace ConFormSim.Actions
{
    public interface IMovable
    {
        bool isHolding{
            get;
            set;
        }
        
        void Move(Transform dropReference, Transform guide);
    }
}