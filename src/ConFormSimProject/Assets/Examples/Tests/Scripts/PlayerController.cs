/*
 * This script uses Code from Sollyman on this thread https://answers.unity.com/questions/1459773/picking-upholding-objects-portal-style.html
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConFormSim.Actions;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform = null;
    [SerializeField] private Image cursorImage = null;
    public GameObject toolIndicator = null;
    public Transform handTransform = null;

    private RaycastHit raycastFocus;
    private bool canInteract = false;

    public float pickUpRange = 3.0f;
    public float speed = 10.0f;

    public bool hasTool = false;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // locks cursor (turn it off and stay inside game window)
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        // use the WASD keys to move the player
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        float zTranslation = verticalInput * speed * Time.deltaTime;
        float xTranslation = horizontalInput * speed * Time.deltaTime;

        transform.Translate(xTranslation, 0, zTranslation);
        
        // On ESC-key press release the cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // Has interact button been pressed whilst interactable object is in froont of player?
        if (Input.GetKeyDown(KeyCode.E) && canInteract)
        {
            IMovable interactComponent = raycastFocus.collider.transform.GetComponent<IMovable>();

            if (interactComponent != null)
            {
                // Perform object's interaction
                interactComponent.Move(this.transform, handTransform);
            }
        }
    }

    void FixedUpdate()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // Is interactable object in front of the player?
        if (Physics.Raycast(ray, out raycastFocus, pickUpRange) && (raycastFocus.collider.CompareTag("Movable") || raycastFocus.collider.CompareTag("Tool")))
        {
            // turn cursor green
            cursorImage.color = Color.green;
            canInteract = true;
        }
        else
        {
            // turn cursor white
            cursorImage.color = Color.white;
            canInteract = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tool"))
        {
            other.GetComponent<IMovable>().Move(this.transform, handTransform);
        }
    }
}
