using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConFormSim.Actions;

/// <summary>
/// An implementation of the IObjectDetector interface. This variant checks the
/// space in front of an object for objects tagged with one of the tags in <see
/// cref="detectableTags"/>. Therefore, at every meter (integer) between the
/// object and object.transform.forward * range a volume of 0.8x1x0.8 m is
/// checked for objects containing colliders with the specified tags. This is
/// especially useful for grid world environments. 
/// </summary>
public class NoPhysicsInteractableDetector : MonoBehaviour, IObjectDetector
{
    [SerializeField]
    private float range;
    /// <inheritdoc/>
    public float Range 
    {
        get { return range;  }
        set { range = value; }
    }

    [SerializeField]
    private List<string> detectableTags;
    /// <inheritdoc/>
    public List<string> DetectableTags
    {
        get { return detectableTags;  }
        set { detectableTags = value; }
    }

    private bool hasObjectDetected;
    /// <inheritdoc/>
    public bool HasObjectDetected
    {
        get 
        {
            CheckForObject();
            return hasObjectDetected; 
        }
    }

    private GameObject detectedObject;

    /// <inheritdoc/>
    public GameObject DetectedObject
    {
        get 
        { 
            CheckForObject(); 
            return detectedObject; 
        }
    }

    // On every Update check for reachable object in front of the agent
    void CheckForObject()
    {
        // check if the next steps have any interactable objects
        for (int i = 1; i <= Mathf.RoundToInt(range); i++)
        {
            Vector3 boxPos = 
                transform.position + transform.forward * i;
            Collider[] objectColliders = Utility.OccupiedPosition(
                boxPos,
                new Quaternion(),
                new Vector3(0.4f, 1f, 0.4f),
                detectableTags);

            // objects found?
            if (objectColliders.Length > 0)
            {
                // enable interaction 
                hasObjectDetected = true;
                detectedObject = objectColliders[0].gameObject;
                break;
            }
            else
            {
                hasObjectDetected = false;
                detectedObject = null;
            }
        }
    }
}
