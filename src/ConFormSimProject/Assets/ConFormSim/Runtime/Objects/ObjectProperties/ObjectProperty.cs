using UnityEngine;

namespace ConFormSim.ObjectProperties
{
    public abstract class ObjectProperty : ScriptableObject
    {
        public abstract bool isArrayType
        {
            get;
        }

        public abstract int arrayLength
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the feature vector that represents this property
        /// </summary>
        /// <returns></returns>
        public abstract float[] GetFeatureVector();

        /// <summary>
        /// The length of the feature vector that will be returned by <see
        /// cref="GetFeatureVector()"/>  
        /// </summary>
        /// <value>Read-Only length of the feature vector.</value>
        public abstract int length 
        {
            get;
        }

        /// <summary>
        /// Adapt the property based on the feature vector settings.
        /// </summary>
        /// <param name="settings">New settings.</param>
        public virtual void ApplySettings(ObjectPropertySettings settings)
        {

        }

        /// <summary>
        /// Set the value the same as from another property but will let
        /// everything else unchanged, except <see cref="arrayLength"/>.
        /// </summary>
        /// <param name="property"></param>
        public abstract void SetValue(ObjectProperty property);

        /// <summary>
        /// Set the property value depending on the GameObject to which the
        /// <see cref="ObjectPropertyProvider"/> is attached to.
        /// </summary>
        /// <param name="attachedGameObject">The GameObject from which the
        /// property will be retrieved.</param>
        public virtual void SetValueFromGameObject(GameObject attachedGameObject)
        {
        }
    }
}