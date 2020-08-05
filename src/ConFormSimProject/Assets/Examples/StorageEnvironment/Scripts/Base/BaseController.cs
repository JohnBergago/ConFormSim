using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour
{
    public GameObject agent;
    private RewardCollector agentRc;
    public Color originalColor;
    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            other.GetComponent<ObjectController>().isInCorrectBaseArea = false;
        }
    }
}
