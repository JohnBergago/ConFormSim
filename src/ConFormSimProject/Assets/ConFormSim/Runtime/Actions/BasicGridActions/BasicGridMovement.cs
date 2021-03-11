using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Actions
{
    public enum MovementType
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
    /// An action of type BasicGridMovement can perform simple movements such as
    /// going forward, backward, left and right or rotating to the left (ccw) or to
    /// the right (cw).
    /// </summary>
    [CreateAssetMenu(fileName = "BasicGridMovement", menuName = "ConFormSim/Agent Actions/BasicGridMovement", order = 1)]
    public class BasicGridMovement : DiscreteAgentAction
    {
        internal Vector3 targetPos;
        internal Quaternion targetRot;

        /// <summary>
        /// The type of movement that should be performed by this action. There are
        /// Go actions translating the agent on a grid by stepSize, and rotation
        /// actions rotating the agent by rotationAngle degree.
        /// </summary>
        [Header("Movement Options")]
        [SerializeField]
        [Help("Choose the movement type, that action should provide. Specify the " + 
            "settings if any. Hint: A Go action cannot have a negative step size.")]
        public MovementType movement;
        
        /// <summary>
        /// The step size for basic forward, backward, left and right movements.
        /// </summary>
        public float stepSize = 1.0f;

        /// <summary>
        /// The angle for basic rotation movements. In degree.
        /// </summary>
        public float rotationAngle = 90.0f;

        /// <summary>
        /// List of tags that will prevent the agent from moving into objects with
        /// that tag.
        /// </summary>
        /// <typeparam name="string"></typeparam>
        public List<string> obstacleTags = new List<string>();

        /// <summary>
        /// Whether to pick up objects the agent collides with. If an agent is
        /// already carrying an object, it will drop the object on the previous
        /// position and pickup the new one on the next position.
        /// </summary>
        [Header("Pick Up Options")]
        [Help("If turned on, the agent will pick up any object that implements the "
            + "IMovable interface.")]
        public bool pickUpOnCollision;
        
        /// <summary>
        /// The position of the guide to which the object will be attached.
        /// </summary>
        public Vector3 guidePosition = new Vector3();

        /// <summary>
        /// Calculate the result of the actual movement depending on the movement
        /// type.
        /// </summary>
        private void Move()
        {
            switch (movement)
            {
                case MovementType.None:
                    // just do nothing
                    break;
                case MovementType.GoForward:
                    // go forward
                    targetPos += 
                        targetRot * (stepSize * Vector3.forward);
                    break;
                case MovementType.GoBackward:
                    // go backward
                    targetPos += 
                        targetRot * (stepSize * Vector3.back);
                    break;
                case MovementType.GoLeft:
                    // go left
                    targetPos += 
                        targetRot * (stepSize * Vector3.left);
                    break;
                case MovementType.GoRight:
                    // go right
                    targetPos += 
                        targetRot * (stepSize * Vector3.right);
                    break;
                case MovementType.RotateLeft:
                    // rotate left
                    targetRot.eulerAngles += Vector3.up * -rotationAngle;
                    break;
                case MovementType.RotateRight:
                    // rotate left
                    targetRot.eulerAngles += Vector3.up * rotationAngle;
                    break;
                default: 
                    break;
            }
        }

        /// <summary>
        /// <inheritdoc cref="Execute"/>
        /// </summary>
        public override ActionOutcome Execute(
            GameObject agent, 
            Dictionary<string, object> kwargs = null)
        {
            // based on the current position of the agent calculate the position
            // after the action
            targetPos = agent.transform.position;
            targetRot = agent.transform.rotation;

            // calculate result of the movement
            Move();

            if (pickUpOnCollision)
            {
                // retrieve guide from parameter dictionary
                object arg;
                Transform guide;
                if (kwargs == null)
                {
                    return new ActionOutcome(false, "No guide parameter passed.");
                }
                else if (kwargs.TryGetValue("guide_transform", out arg))
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
                guide.localPosition = guidePosition;
                // check if the agent would collide with any object in the next
                // position
                List<GameObject> collisionObjects = GetCollidingObjects(agent);
                
                // try to find an object that implements the IMovable interface
                IMovable movableComponent = null;
                foreach(GameObject obj in collisionObjects)
                {
                    movableComponent = obj.GetComponent<IMovable>();
                    if (movableComponent != null)
                    {
                        break;
                    }
                }
                // in case there is a object the agent would collide with and it is
                // movable 
                if (movableComponent != null)
                {
                    // first get rid of any object we carry already
                    if (guide.childCount > 0)
                    {
                        guide
                            .GetChild(0)
                            .GetComponent<IMovable>()
                            .Move(agent.transform, guide);
                    }
                    // then pick up the new object
                    movableComponent.Move(agent.transform, guide);
                }

            }
            
            ActionOutcome outcome = new ActionOutcome();
            // if anything happened for that action
            if (targetPos != agent.transform.position ||targetRot != agent.transform.rotation)
            {
                // check if the action result is valid
                outcome.success = IsValidMove(agent);
            }
            else
            {
                // else a None action is always successful
                outcome.success = true;
            }
            // if the action was successful, execute.
            if (outcome.success)
            {
                agent.transform.position = targetPos;
                agent.transform.rotation = targetRot;
            }
            // otherwise add a comment about failure
            else
            {
                outcome.comment = "Agent couldn't move. There was an obstacle.";
            }
            return outcome;
        } 

        private List<GameObject> GetCollidingObjects(GameObject agent)
        {
            // Get size of agent. This includes all carried objects, as they are
            // temporarily part of the agent
            Bounds agentBounds = Utility.GetBoundsOfAllDeepChilds(agent);

            // get all bound objects of an agent. This includes carried objects, as
            // they are attached to a child of an agent.
            List<Bounds> agentChilds = Utility.GetBoundsOfAllDeepChildObjects(agent);

            List<Collider> allObstacleColliders = new List<Collider>();

            foreach (Bounds objBounds in agentChilds)
            {
                Vector3 v_agentObject = objBounds.center - agent.transform.position;
                Quaternion rotDiff = new Quaternion();
                    rotDiff.eulerAngles = targetRot.eulerAngles 
                        - agent.transform.rotation.eulerAngles; 
                    v_agentObject = rotDiff * v_agentObject;

                allObstacleColliders.AddRange(
                    Utility.OccupiedPosition(
                        targetPos + v_agentObject,
                        rotDiff,
                        objBounds.extents * 0.8f,
                        obstacleTags)
                );

                // Debug Bounding Box for checked positions
                // GameObject bounding = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // bounding.transform.position = targetPos + v_agentObject;
                // bounding.transform.rotation = rotDiff;
                // bounding.transform.localScale = objBounds.size * 0.8f;
            }

            List<GameObject> collidingObjects = new List<GameObject>();
            
            for (int i = 0; i < allObstacleColliders.Count; i++)
            {
                // check if agent doesn't carry the object
                if (!allObstacleColliders[i].transform.IsChildOf(agent.transform))
                {
                    collidingObjects.Add(allObstacleColliders[i].gameObject);
                }
            }
            return collidingObjects;
        }

        /// <summary>
        /// A method that will be called from the Execute() method in order to check
        /// if a performed movement is allowed or not. Depending on the result the
        /// movement will be performed or not.
        /// </summary>
        /// <param name="agent">The agent GameObject that is going to perform this
        /// action. </param>
        /// <returns>True if the Movement is valid, otherwise false.</returns>
        private bool IsValidMove(GameObject agent)
        {
            if (GetCollidingObjects(agent).Count > 0)
            {
                return false;
            }
            return true;
        }

    }
}