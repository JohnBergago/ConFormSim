using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownFollowCamera : MonoBehaviour
{
    public GameObject agent;
    public bool rotateWithAgent = true;
    public float yOffset = 12;

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = agent.transform.position + Vector3.up * yOffset;
        if(rotateWithAgent)
        {
            transform.rotation = Quaternion.AngleAxis(90, Vector3.right) * 
                Quaternion.AngleAxis(
                    -agent.transform.rotation.eulerAngles.y, 
                    Vector3.forward);
        }
        else
        {
            transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
        }
    }

    public void InitializePosition()
    {
        FixedUpdate();
    }
}
