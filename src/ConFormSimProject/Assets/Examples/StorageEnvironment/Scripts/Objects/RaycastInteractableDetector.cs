using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConFormSim.Actions;

/// <summary>
/// This implementation of the <see cref="ConFormSim.Actions.IObjectDetector"/> interface uses
/// raycasts to detect objects.
/// </summary>
public class RaycastInteractableDetector : MonoBehaviour, IObjectDetector
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
        get { return hasObjectDetected; }
    }

    [Header("Raycast Settings")]
    /// <summary>
    /// The offset of the casted ray in y-direction from the transform it is
    /// this script is attached to.
    /// </summary>
    public float yOffset;

    /// <summary>
    /// Rotation around the x-axis of the objects transform. So the ray won't be
    /// parallel to the x-z-plane.
    /// </summary>
    public float xRotation;

    /// <summary>
    /// Rotation around the y axis of the object. So the ray can go sideways
    /// from the object.
    /// </summary>
    public float yRotation;

    /// <summary>
    /// Whether to use a sphere cast instead of a plain raycast. 
    /// </summary>
    public bool useSphereCast;

    /// <summary>
    /// The radius of the sphere in case spherecast was chosen.
    /// </summary>
    public float sphereRadius = 0.2f;

    private GameObject detectedObject;

    /// <inheritdoc/>
    public GameObject DetectedObject
    {
        get { return detectedObject; }
    }

    /// <summary>
    /// variable fr drawing the gizmo in the editor
    /// </summary>
    private float rayCastHitDistance;

    void FixedUpdate()
    {
        Quaternion rayRotation = new Quaternion();
        rayRotation.eulerAngles = new Vector3(xRotation, yRotation, 0);
        Vector3 rayStart = transform.position + yOffset * Vector3.up;
        Vector3 rayDir = (rayRotation * transform.forward).normalized * range;
        
        RaycastHit hit;
        bool hitSomething = false; 

        if (useSphereCast)
        {
            if (Physics.SphereCast(rayStart, sphereRadius, rayDir, out hit, range))
            {
                if (detectableTags.Contains(hit.collider.tag))
                {
                    hitSomething = true;
                }
            }
        }
        else
        {
            if (Physics.Raycast(rayStart, rayDir, out hit, range))
            {
                if (detectableTags.Contains(hit.collider.tag))
                {
                    hitSomething = true;
                }
            }
        }

        if (hitSomething)
        {
            detectedObject = hit.collider.gameObject;
            rayCastHitDistance = hit.distance;
            hasObjectDetected = true;
        }
        else
        {
            detectedObject = null;
            rayCastHitDistance = range;
            hasObjectDetected = false;
        }

    }

    void OnDrawGizmos()
    {
        // Draws nice little Gizmos into the editor scene. So debugging is
        // easier.
        Quaternion rayRotation = new Quaternion();
        rayRotation.eulerAngles = new Vector3(xRotation, yRotation, 0);
        Vector3 rayStart = transform.position + yOffset * Vector3.up;
        Vector3 rayDir = (rayRotation * transform.forward).normalized * rayCastHitDistance;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(rayStart, rayDir);
        if (useSphereCast)
        {
            Gizmos.DrawWireSphere(rayStart + rayDir, sphereRadius);
        }
    }

}
