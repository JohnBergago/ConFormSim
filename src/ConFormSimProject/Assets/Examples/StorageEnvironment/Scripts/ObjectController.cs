using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConFormSim.Actions;

public class ObjectController : MonoBehaviour
{
    public bool isInShelf = false;
    public bool isInCorrectBaseArea = false;

    public bool gotDeliveredReward = false;
    // variables for reward functionality
    public bool wasPicked = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(GetComponent<IMovable>().isHolding)
        {
            isInCorrectBaseArea = false;
        }
    }
}
