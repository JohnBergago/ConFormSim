using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConFormSim.Actions;

public class ToolPickUp : MonoBehaviour, IMovable
{
    private bool m_IsHolding = false;
    /// <summary>
    /// boolean to check whether the object is being held or not
    /// </summary>
    public bool isHolding
    {
        get { return m_IsHolding;  }
        set { m_IsHolding = value; }
    }
    
    public void Move(Transform dropReference, Transform guide)
    {
        Destroy(this.gameObject);
        // agent.hasTool = true;
        // if(agent.toolIndicator)
        // {
        //     agent.toolIndicator.SetActive(true);
        // }
    }
}
