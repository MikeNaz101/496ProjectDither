using System.Collections;
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
    [SerializeField]
    float holdTime = 2.0f; // Time required to hold E for interaction

    public LayerMask interactableLayer;

    Vector2 movement;
    Vector2 mouseMovement;
    CharacterController chara;
    float cameraUpRotation = 0;

    float holdTimer = 0f; // Timer for holding the E key
    bool isHolding = false; // Whether the player is holding the E key
    bool isPerformingHoldTask = false; // Whether the hold task is being performed

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
        // Camera control (look around)
        float mouseX = mouseMovement.x * Time.deltaTime * mouseSensitivity;
        float mouseY = mouseMovement.y * Time.deltaTime * mouseSensitivity;
        cameraUpRotation -= mouseY;
        cameraUpRotation = Mathf.Clamp(cameraUpRotation, -90, 90);
        playerCam.transform.localRotation = Quaternion.Euler(cameraUpRotation, 0, 0);

        // Movement control
        transform.Rotate(Vector3.up * mouseX);
        float moveX = movement.x;
        float moveZ = movement.y;
        Vector3 m = (transform.right * moveX) + (transform.forward * moveZ);
        chara.SimpleMove(m * speed);

        // Handle press interaction (press E once for a quick action)
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            PerformPressInteraction();
        }

        // Handle hold interaction (hold E for a longer task)
        if (Keyboard.current.eKey.isPressed)
        {
            if (!isHolding)
            {
                // Start holding the key
                isHolding = true;
                holdTimer = 0f; // Reset the timer
            }

            // Increment the hold timer
            holdTimer += Time.deltaTime;

            // If the hold time is met, start performing the task
            if (holdTimer >= holdTime && !isPerformingHoldTask)
            {
                StartCoroutine(PerformHoldInteractionTask());
            }
        }
        else
        {
            // Reset when the key is released
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;
            }
        }
    }

    // Methods to handle input actions
    void OnMove(InputValue moveVal)
    {
        movement = moveVal.Get<Vector2>();
    }

    void OnLook(InputValue lookVal)
    {
        mouseMovement = lookVal.Get<Vector2>();
    }

    // Perform the press interaction (when E is pressed once)
    private void PerformPressInteraction()
    {
        // You can add your quick interaction logic here
        Ray ray = playerCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        Debug.DrawLine(ray.origin, ray.direction, Color.green);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            Debug.Log("Quick Interacted with: " + hit.collider.gameObject.name);
            hit.collider.gameObject.SendMessage("Interact");
        }
    }

    // Coroutine for performing the task after holding [E] (longer interaction)
    private IEnumerator PerformHoldInteractionTask()
    {
        isPerformingHoldTask = true;

        // Perform the task (example: interacting with an object)
        Ray ray = playerCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        Debug.DrawLine(ray.origin, ray.direction, Color.red);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            Debug.Log("Holding interaction started with: " + hit.collider.gameObject.name);
            hit.collider.gameObject.SendMessage("HoldInteract");
        }

        // Simulate some task duration (e.g., waiting for an interaction to complete)
        yield return new WaitForSeconds(1f); // Adjust based on the task duration

        // Task completed
        isPerformingHoldTask = false;
    }
}
