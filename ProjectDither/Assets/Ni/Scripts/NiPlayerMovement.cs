using UnityEngine;
using UnityEngine.InputSystem;

public class NiPlayerMovement : MonoBehaviour
{
    [SerializeField]
    float speed = 2.0f;
    [SerializeField]
    float mouseSensitivity = 100;
    [SerializeField]
    Camera playerCam;
    [SerializeField]
    float interactionRange = 3.0f;

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
        playerCam.transform.localRotation = Quaternion.Euler(cameraUpRotation, 0, 0);

        //Movement
        transform.Rotate(Vector3.up * mouseX);
        float moveX = movement.x;
        float moveZ = movement.y;
        Vector3 m = (transform.right * moveX) + (transform.forward * moveZ);
        chara.SimpleMove(m * speed);

        //Interact when clicked
        if (Keyboard.current.eKey.isPressed)
        {
            OnInteract();
        }
    }

    void OnMove(InputValue moveVal)
    {
        movement = moveVal.Get<Vector2>();
    }

    void OnLook(InputValue lookVal)
    {
        mouseMovement = lookVal.Get<Vector2>();
    }

    void OnInteract()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Debug.DrawLine(ray.origin, ray.direction, Color.green);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            Debug.Log("Interacted with: " + hit.collider.gameObject.name);
        }

        hit.collider.gameObject.SendMessage("Interact");
    }
}