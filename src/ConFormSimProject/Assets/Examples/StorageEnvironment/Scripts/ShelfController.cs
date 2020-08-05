using System.Collections;
using System.Collections.Generic;
using System; 
using UnityEngine;

public class ShelfController : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }


    
    void OnTriggerStay(Collider other)
    {
        gameObject.tag = "Untagged";
    }

    void OnTriggerExit(Collider other)
    {
        gameObject.tag = "Interactable";
    }
}
