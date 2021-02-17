using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversionTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(gameObject.GetInstanceID());
        Color test = new Color(gameObject.GetInstanceID(), 0,0,1);
        Debug.Log(test.r);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
