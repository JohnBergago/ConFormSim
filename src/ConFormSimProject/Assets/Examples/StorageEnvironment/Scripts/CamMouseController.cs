using UnityEngine;

public class CamMouseController : MonoBehaviour
{
    private Vector2 mouseLook;
    private Vector2 smoothV;

    public float sensitivity = 5.0f;
    public float smoothing = 2.0f;
    
    GameObject character;

    // Start is called before the first frame update
    void Start()
    {
        // get the parent GameObject which is our player
        character = this.transform.parent.gameObject;
    }

    // Update is called once per frame
    void LateUpdate()
    {   
        // Claculate rotation of the camera and character on MouseMovement
        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothV.x = Mathf.Lerp(smoothV.x, mouseDelta.x, 1.0f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, mouseDelta.y, 1.0f / smoothing);
        mouseLook += smoothV;
        // clamp the camera on looking up and down
        mouseLook.y = Mathf.Clamp(mouseLook.y, -90.0f, 90f);

        transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
        character.transform.localRotation = Quaternion.AngleAxis(mouseLook.x, character.transform.up);
    }
}
