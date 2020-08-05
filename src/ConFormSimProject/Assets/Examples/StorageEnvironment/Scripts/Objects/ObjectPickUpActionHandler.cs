using UnityEngine;
using ConFormSim.Actions;

/// <summary>
/// The object to which this component attached can be picked up by Agent
/// objects. Therefore the Agent has to call the Move() method of this class and
/// pass the Agent and the Transform to which the object should be connected.
/// </summary>
public class ObjectPickUpActionHandler : MonoBehaviour, IMovable
{
    /// <summary>
    /// A RectGrid in the are to which the object will snap as soon as it drops.
    /// </summary>
    private GridBehaviour m_AreaGrid;

    /// <summary>
    /// The bounds of the rendered mesh of this object in order to get the
    /// correct size.
    /// </summary>
    private Bounds m_thisBounds;

    private bool m_IsHolding = false;
    /// <summary>
    /// boolean to check whether the object is being held or not
    /// </summary>
    public bool isHolding
    {
        get { return m_IsHolding;  }
        set { m_IsHolding = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_thisBounds = Utility.GetBoundsOfAllDeepChilds(gameObject);
        m_AreaGrid = transform.parent.FindDeepChild("PathGrid").GetComponent<GridBehaviour>();
        
        // try to place object on top of whats underneath every 10 update steps
        InvokeRepeating("PlaceOnTopIfNotHeld", 0f, 10 * Time.fixedDeltaTime);
    }

    /// <summary>
    /// If the object is not held by an agent it will be placed on the ground or
    /// any object underneath it.
    /// </summary>
    void PlaceOnTopIfNotHeld()
    {
        if (!m_IsHolding)
        {
            PlaceOnTop();
        }
    }

    /// <summary>
    /// Pick up the object, or drop it if it is already being held
    /// </summary>
    /// <param name="dropReference">Transform that is on the level to which the
    /// dropped object will be added. For instance agent.transform. The object
    /// becomes a child of this soon as it is dropped. </param>
    /// <param name="guide">The transform which will be the parent of the object
    /// while it is being carried.</param>
    public void Move(Transform dropReference, Transform guide)
    {     
        // Is the game object currently being held?
        if (m_IsHolding)
        {
            // release the object
            transform.SetParent(dropReference.parent);
            
            // place on top of whatever is underneath the object.
            PlaceOnTop();

            // if a grid exists
            if (m_AreaGrid)
            {
                // snap the object to the grid
                transform.localPosition = m_AreaGrid.WorldToGridCoordinates(transform.position);
            }
            m_IsHolding = false;
        }
        else
        {
            // pick up the object
            transform.SetParent(guide);
            transform.position = guide.position;
            m_IsHolding = true;
        }
    }

    /// <summary>
    /// Places the object on top of whatever is underneath it.
    /// </summary>
    private void PlaceOnTop()
    {
        // set position to the next object that is underneath  
        Vector3 yOff = new Vector3(
            0, 
            -Utility.GetBoundsOfAllDeepChilds(gameObject).extents.y, 
            0);

        // check if any object is underneath the current object position
        RaycastHit hit;
        if (Physics.Raycast(
            transform.position + yOff, 
            Vector3.down, 
            out hit, 
            maxDistance: 10,
            layerMask: ~0,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore))
        {
            // if there is a object underneath
            // Debug.Log("There is a " + hit.collider.name +  " under the object at " + hit.collider.gameObject.transform.position);
            float yExtentsCollider;
            Renderer objRenderer;
            // try to get the size of the object underneath
            if (hit.collider.gameObject.TryGetComponent<Renderer>(out objRenderer))
            {
                yExtentsCollider = objRenderer.bounds.extents.y;
            }
            else
            {
                yExtentsCollider = Utility
                    .GetBoundsOfAllChilds(hit.collider.gameObject)
                    .extents
                    .y;
            }
            
            // set y position to the correct offset value, so that this
            // object is placed on top of it.
            transform.position = new Vector3(
                transform.position.x,
                hit.collider.gameObject.transform.position.y 
                    + yExtentsCollider + m_thisBounds.extents.y,
                transform.position.z);
        }
    }

}
