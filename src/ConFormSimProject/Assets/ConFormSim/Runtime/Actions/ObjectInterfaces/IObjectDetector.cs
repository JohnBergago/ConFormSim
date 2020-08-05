using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Actions
{
    /// <summary>
    /// Interface for components or classes detecting objects around the GameObject
    /// they are attached to. They provide one object (usually the first detected)
    /// to the interface.
    /// </summary>
    public interface IObjectDetector
    {
        /// <summary>
        /// The distance up until which objects should be detectable.
        /// </summary>
        /// <value></value>
        float Range 
        {
            get;
            set;
        }

        /// <summary>
        /// The tags of the objects to be detected.
        /// </summary>
        /// <value></value>
        List<string> DetectableTags
        {
            get;
            set;
        }

        /// <summary>
        /// True if an object has been detected, false otherwise. (Read Only)
        /// </summary>
        /// <value></value>
        bool HasObjectDetected
        {
            get;
        }

        /// <summary>
        /// The object that has been detected. If there is no object this value will
        /// be null. (Read only)
        /// </summary>
        /// <value></value>
        GameObject DetectedObject
        {
            get;
        }
    }
}