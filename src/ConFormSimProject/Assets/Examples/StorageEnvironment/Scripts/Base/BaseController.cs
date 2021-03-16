using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour
{
    public GameObject edgePrimitivePrefab;
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            other.GetComponent<ObjectController>().isInCorrectBaseArea = false;
        }
    }
}
