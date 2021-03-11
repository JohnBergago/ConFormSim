using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Actions
{
    /// <summary>
    /// An action of this type can pick up and drop objects. Basically it calls the
    /// Move() method of an IMovable object and controls the position of the guide
    /// transform. Depending on the implementation of the Move() method the IMovable
    /// object will be attached to the guide transform.
    /// </summary>
    /// <remarks>
    /// The Execute() method requires 2 additional arguments in the kwargs parameter
    /// of the function. Usually these arguments will be passed to execute by the
    /// ActionProvider. To do that provide a dictionary to the kwargs parameter
    /// containing a 'interactable_gameobject' key and a 'guide_transform'.
    /// 
    /// The 'interactable_gameobject' must be a GameObject with a component
    /// implementing the IMovable interface.
    /// 
    /// The 'guide_transform' must be a Transform. It will be passed to the Move()
    /// method of the interactable.
    /// </remarks>
    [CreateAssetMenu(fileName = "PhysicsPickUp", menuName = "ConFormSim/Agent Actions/PhysicsPickUp", order = 1)]
    public class PhysicsPickUp : DiscreteAgentAction
    {
        /// <summary>
        /// Local Position of the guide when dropping an object. This influences the
        /// drop/release position of an interactable object.
        /// </summary>
        // [Tooltip("Local position of the guide, relative to the agent when an " +
        //     "object is released.")]
        // public Vector3 guideDropPosition = new Vector3();

        /// <summary>
        /// Local Position of the guide when picking the object up and carrying it.
        /// </summary>
        // [Tooltip("Local position of the guide, relative to the agent when an " +
        //     "object is carried.")]
        // public Vector3 guideCarryPosition = new Vector3();

        /// <summary>
        /// <inheritdoc cref="Execute"/>
        /// </summary>
        public override ActionOutcome Execute(
            GameObject agent, 
            Dictionary<string, object> kwargs = null)
        {
            GameObject interactable;
            Transform guide;
        
            // helper variable
            object arg;
            // check if any arguments were passed
            if (kwargs == null)
            {
                return new ActionOutcome(false, "No parameters passed."); 
            }
            // try to retrieve the interactable object from the arguments dict
            if (kwargs.TryGetValue("interactable_gameobject", out arg) && arg != null)
            {
                if (arg == null)
                {
                    return new ActionOutcome(false, "No interactable object available."); 
                }
                interactable = (GameObject) arg;
            } 
            else
            {
                Debug.Log("Couldn't perform Pick up, as no interactable object"
                    + " was passed to the action. Assign it to the "
                    + "'interactable_gameobject' key in the kwargs dict.");
                return new ActionOutcome(false, "No interactable object available.");   
            }
            
            // try to get the transform to which the object will be connected while
            // carrying
            if (kwargs.TryGetValue("guide_transform", out arg))
            {
                guide = (Transform) arg;
            }
            else
            {
                Debug.Log("Couldn't perform Pick up, as no guide transform "
                    + "was passed to the action. Assign a Transform object to "
                    + "the 'guide_transform' key in the kwarg dict.");
                return new ActionOutcome(false, "No guide transform assigned.");
            }

            // now that we have all necessary objects perform the actual action
            // first check if the interactable has a component implementing an
            // IMovable
            IMovable movableComponent = interactable.GetComponent<IMovable>();
            if (movableComponent == null || interactable == null)
            {
                Debug.LogError("Couldn't perform Pick Up. The passed interactable " 
                    + " doesn't have a component implementing the IMovable" 
                    + " interface.");
                return new ActionOutcome(false, "Interactable has no IMovable " 
                    + "component");
            }

            // in case there is already an object attached to the guide
            FixedJoint joint;
            if (guide.childCount > 0 || guide.gameObject.TryGetComponent<FixedJoint>(out joint))
            {
                    // Drop the object
                    // guide.localPosition = guideDropPosition;
                    movableComponent.Move(agent.transform, guide);
                    return new ActionOutcome(true, "Object released.");
            }
            // else if the guide doesn't has a object attached
            else if (guide.childCount == 0 || !guide.gameObject.TryGetComponent<FixedJoint>(out joint))
            {
                // pick up the object
                // guide.localPosition = guideCarryPosition;
                movableComponent.Move(agent.transform, guide);
                return new ActionOutcome(true, "Object picked.");
            }
            
            return new ActionOutcome(false, "No free space to release object.");
        } 

    }
}