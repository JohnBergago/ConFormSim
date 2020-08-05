using UnityEngine;
using Unity.MLAgents;
using ConFormSim.Actions;
public class PhysicsPickUpActionHandler : MonoBehaviour, IMovable
{
    private bool m_IsHolding;
    public bool isHolding
    {
        get { return m_IsHolding;  }
        set { m_IsHolding = value; }
    }

    /// <summary>
    /// The rigidbody component of the object this component is attached to.
    /// </summary>
    private Rigidbody m_ThisRigidBody = null;

    /// <summary>
    /// The joint to which this object will be attached on the agents guide.
    /// </summary>
    private FixedJoint m_HoldJoint = null;

    private GameObject m_AgentObj = null;

    private void Start()
    {
        m_ThisRigidBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // if holding joint has broken, drop the object
        if(m_HoldJoint == null && m_IsHolding == true)
        {
            m_IsHolding = false;
            m_ThisRigidBody.useGravity = true;
            if(m_AgentObj != null)
            {
                Physics.IgnoreCollision(m_AgentObj.GetComponent<Collider>(), GetComponent<Collider>(), false);
            }
        }
    }

    public void Move(Transform dropReference, Transform guide)
    {
        Transform agentTransform = guide;
        Agent agentScript = null;
        while (agentTransform != null && !agentTransform.gameObject.TryGetComponent(out agentScript))
        {
            agentTransform = agentTransform.parent;
        }
        if (agentScript != null)
        {
            m_AgentObj = agentScript.gameObject;
        }
        
        // is object currently being held?
        if (m_IsHolding)
        {
            Drop();
            if(m_AgentObj != null)
            {
                Physics.IgnoreCollision(m_AgentObj.GetComponent<Collider>(), GetComponent<Collider>(), false);
            }
        }
        else
        {
            m_IsHolding = true;
            m_ThisRigidBody.useGravity = false;

            m_HoldJoint = guide.gameObject.AddComponent<FixedJoint>();
            m_HoldJoint.breakForce = 100000f;
            m_HoldJoint.connectedBody = m_ThisRigidBody;
            if(m_AgentObj != null)
            {
                Physics.IgnoreCollision(m_AgentObj.GetComponent<Collider>(), GetComponent<Collider>(), true);
            }
        }
    }

    private void Drop()
    {
        m_IsHolding = false;
        m_ThisRigidBody.useGravity = true;

        Destroy(m_HoldJoint);
    }

    void OnDestroy()
    {
        if (m_HoldJoint != null)
        {
            Destroy(m_HoldJoint);
        }
    }
}