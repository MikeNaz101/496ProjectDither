using UnityEngine;
using UnityEngine.InputSystem;

public class NickPlayerMovement : MonoBehaviour
{
    [SerializeField]
    float speed = 2.0f;
    [SerializeField]
    float mouseSensitivity = 100;
    [SerializeField]
    Camera playerCam;
    [SerializeField]
    float interactionRange = 3.0f;

    [SerializeField]
    HandUI hui;

    public LayerMask interactableLayer;
    
    Vector2 movement;
    Vector2 mouseMovement;
    CharacterController chara;
    float cameraUpRotation = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        chara = GetComponent<CharacterController>();
        playerCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        //Camera
        float mouseX = mouseMovement.x * Time.deltaTime * mouseSensitivity;
        float mouseY = mouseMovement.y * Time.deltaTime * mouseSensitivity;
        cameraUpRotation -= mouseY;
        cameraUpRotation = Mathf.Clamp(cameraUpRotation, -90, 90);
        //playerCam.transform.localRotation = Quaternion.Euler(cameraUpRotation, 0, 0);

        //Movement
        transform.Rotate(Vector3.up * mouseX);
        float moveX = movement.x;
        float moveZ = movement.y;
        Vector3 m = (transform.right * moveX) + (transform.forward * moveZ);
        chara.SimpleMove(m * speed);



        //Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        //Ray ray = new Ray(playerCam.transform.position, playerCam.transform.right);
        
        //Debug.DrawLine(ray.origin, playerCam.transform.up, Color.red);
        //Debug.DrawLine(ray.origin, playerCam.transform.right, Color.green);
        //Debug.DrawLine(ray.origin, playerCam.transform.forward, Color.blue);

        //Debug.Log("Forward: " + playerCam.transform.forward);
        //Debug.Log("Right: " + playerCam.transform.right);
        //Debug.Log("Up: " + playerCam.transform.up);

        RaycastHit hit;
        Debug.DrawLine(playerCam.transform.position, playerCam.transform.forward, Color.white);
        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, 5))
        {
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.tag == "Interactable")
                {
                    Debug.Log("hit");
                    hui.makeInteractVisible(true);
                    if (Keyboard.current.eKey.isPressed)
                    {
                        OnInteract(hit.collider.gameObject);
                    }
                }
            }
            else
            {
                hui.makeInteractVisible(false);
            }
            //Debug.Log("Interacted with: " + hit.collider.gameObject.name);
        }
        //Interact when clicked
    }

    void OnMove(InputValue moveVal)
    {
        movement = moveVal.Get<Vector2>();
    }

    void OnLook(InputValue lookVal)
    {
        mouseMovement = lookVal.Get<Vector2>();
    }

    void OnInteract(GameObject go)
    {
        go.gameObject.SendMessage("Interact");
    }
}