using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Actions
{
    /// <summary>
    /// An action of type BasicGridMovement can perform simple movements such as
    /// going forward, backward, left and right or rotating to the left (ccw) or to
    /// the right (cw).
    /// </summary>
    [CreateAssetMenu(fileName = "BasicPhysicsMovement", menuName = "ConFormSim/Agent Actions/BasicPhysicsMovement", order = 1)]
    public class BasicPhysicsMovement : DiscreteAgentAction
    {
        internal Vector3 targetPos;
        internal Quaternion targetRot;

        public enum PhysicsMovementType
        {
            None,
            GoForward,
            GoBackward,
            GoLeft,
            GoRight,
            RotateLeft,
            RotateRight
        }

        /// <summary>
        /// The type of movement that should be performed by this action. There are
        /// Go actions translating the agent on a grid by stepSize, and rotation
        /// actions rotating the agent by rotationAngle degree.
        /// </summary>
        [Header("Movement Options")]
        [SerializeField]
        [Help("Choose the movement type, that action should provide. Specify the " + 
            "settings if any. Hint: A Go action cannot have a negative step size.")]
        public PhysicsMovementType movement;
        
        public float rotationSpeed = 200f;
        public float translationForce = 2f;

        /// <summary>
        /// <inheritdoc cref="Execute"/>
        /// </summary>
        public override ActionOutcome Execute(
            GameObject agent, 
            Dictionary<string, object> kwargs = null)
        {
            Vector3 dirToGo = Vector3.zero;
            Vector3 rotateDir = Vector3.zero;

            switch (movement)
            {
                case PhysicsMovementType.None:
                    // just do nothing
                    break;
                case PhysicsMovementType.GoForward:
                    // go forward
                    dirToGo = agent.transform.forward * 1f;
                    break;
                case PhysicsMovementType.GoBackward:
                    // go backward
                    dirToGo = agent.transform.forward * -1f;
                    break;
                case PhysicsMovementType.RotateLeft:
                    // rotate left
                    rotateDir = agent.transform.up * -1f;
                    break;
                case PhysicsMovementType.RotateRight:
                    // rotate left
                    rotateDir = agent.transform.up * 1f;
                    break;
                case PhysicsMovementType.GoRight:
                    // go right
                    dirToGo = agent.transform.right * 1f;
                    break;
                case PhysicsMovementType.GoLeft:
                    // go left
                    dirToGo = agent.transform.right * -1f;
                    break;
                default: 
                    break;
            }

            ActionOutcome outcome = new ActionOutcome();

            agent.transform.Rotate(rotateDir, Time.deltaTime * rotationSpeed);
            Rigidbody agentRb;
            if (agent.TryGetComponent<Rigidbody>(out agentRb))
            {
                agentRb.AddForce(dirToGo * translationForce, ForceMode.VelocityChange);
                outcome.success = true;
            }
            else
            {
                outcome.success = false;
                outcome.comment = "Cannot perform action. No Rigidbody found on agent.";
            }

            return outcome;
        } 

    }
}
