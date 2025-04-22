using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class NiPlayerMovement1 : MonoBehaviour
{
    private GameManager gameManager;

    //I used Headers to make everything organize in the Inspector
    [Header("Movement Settings")]
    [SerializeField]
    float speed = 2.0f;
    [SerializeField]
    float mouseSensitivity = 100f;

    [Header("Camera & Interaction")]
    [SerializeField]
    Camera playerCam;
    [SerializeField]
    float interactionRange = 3.0f;
    [SerializeField]
    LayerMask interactableLayer; // Layer for interactable objects

    [Header("Hold Interaction")]
    [SerializeField]
    float holdTime = 2.0f;
    [SerializeField]
    Slider holdProgressBar; // Progress bar for hold interaction

    [Header("UI")]
    [SerializeField]
    TMP_Text interactionPrompt; // Text for interaction prompt

    [Header("Button Clicking Mini-Game")]
    [SerializeField]
    Camera interactionCam;  // Computer camera
    [SerializeField]
    GameObject computerObject;
    [SerializeField]
    GameObject computerToSwitchWith;
    [SerializeField]
    GameObject buttonClickingCanvas;
    [SerializeField]
    Button[] buttons; // The buttons for the mini-game
    [SerializeField]
    float buttonClickTimeLimit = 5.0f;
    [SerializeField]
    TMP_Text gameStatusText;

    Vector2 movement;
    Vector2 mouseMovement;
    CharacterController chara;
    float cameraUpRotation = 0f;
    float holdTimer = 0f;

    bool isHolding = false;
    bool isPerformingHoldTask = false;
    bool isLookingAtInteractable = false;
    bool canHoldInteract = false;
    GameObject currentInteractable = null;

    bool inComputerCamera = false;
    bool isButtonClickingGameActive = false;
    float buttonClickTimer = 0f;
    int buttonsClicked = 0;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        Cursor.lockState = CursorLockMode.Locked;
        chara = GetComponent<CharacterController>();
        playerCam = Camera.main;
        interactionCam.enabled = false;

        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);

        if (holdProgressBar != null)
        {
            holdProgressBar.gameObject.SetActive(false);
            holdProgressBar.value = 0;
        }

        if (buttonClickingCanvas != null)
            buttonClickingCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (gameManager != null)
            {
                gameManager.ToggleTaskListAnimation();
            }
        }
        if (inComputerCamera) // If the player interacted with the computer, handle the mini-game logic
        {
            // Handle the button clicking mini-game
            if (isButtonClickingGameActive)
            {
                interactionPrompt.gameObject.SetActive(false);
                buttonClickTimer += Time.deltaTime;
                if (buttonClickTimer >= buttonClickTimeLimit || buttonsClicked == buttons.Length) // If time is up or all buttons are clicked, exit
                {
                    ExitComputerCam();
                }
                return; // Prevent FPS movement during mini-game
            }
        }
        //Handle core FPS movement
        HandleCamera();
        HandleMovement();
        HandlePressInteraction();
        HandleHoldInteraction();
        CheckForInteractable();
    }

    void HandleCamera() //Handle camera rotation
    {
        if (inComputerCamera) return; // Prevent camera movement during mini-game

        float mouseX = mouseMovement.x * Time.deltaTime * mouseSensitivity;
        float mouseY = mouseMovement.y * Time.deltaTime * mouseSensitivity;

        cameraUpRotation -= mouseY;
        cameraUpRotation = Mathf.Clamp(cameraUpRotation, -90f, 90f);

        playerCam.transform.localRotation = Quaternion.Euler(cameraUpRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement() //Handle player movement
    {
        if (inComputerCamera) return; // Prevent movement during mini-game

        Vector3 move = (transform.right * movement.x) + (transform.forward * movement.y);
        chara.SimpleMove(move * speed);
    }

    void HandlePressInteraction() //Handle Press interaction
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && isLookingAtInteractable && !canHoldInteract)
        {
            InteractWithObject();
        }
    }

    void HandleHoldInteraction() //Handle Hold interaction
    {
        if (Keyboard.current.eKey.isPressed && isLookingAtInteractable && canHoldInteract)
        {
            StartHoldInteraction();
        }
        else
        {
            ResetHoldInteraction();
        }
    }

    void StartHoldInteraction() //Start the hold interaction
    {
        if (!isHolding)
        {
            isHolding = true;
            holdTimer = 0f;
            holdProgressBar.gameObject.SetActive(true);
        }

        holdTimer += Time.deltaTime; // Increment the hold timer
        holdProgressBar.value = holdTimer / holdTime; // Update the progress bar

        if (holdTimer >= holdTime && !isPerformingHoldTask)
        {
            StartCoroutine(PerformHoldInteractionTask());
        }
    }

    void ResetHoldInteraction() //Reset the hold interaction
    {
        if (isHolding)
        {
            isHolding = false;
            holdTimer = 0f;
            holdProgressBar.value = 0;
            holdProgressBar.gameObject.SetActive(false);
        }
    }

    IEnumerator PerformHoldInteractionTask() //Simulate a task that takes time
    {
        isPerformingHoldTask = true;
        InteractWithObject(true);

        yield return new WaitForSeconds(1f); // Simulated task duration

        isPerformingHoldTask = false;
        ResetHoldInteraction();
    }

    void InteractWithObject(bool isHoldTask = false) //Interact with the object
    {
        if (currentInteractable != null)
        {
            var interactableObject = currentInteractable.GetComponent<InteractableObject>();
            if (isHoldTask)
            {
                interactableObject.HoldInteract();
            }
            else
            {
                interactableObject.Interact();
            }
            Debug.Log(isHoldTask ? "Holding interaction with: " : "Quick interacted with: " + currentInteractable.name);
        }

        var hideSeek = currentInteractable.GetComponent<HideAndSeekObject>();
        if (hideSeek != null)
        {
            hideSeek.Interact();
        }
    }

    void CheckForInteractable() //Check for interactable objects
    {
        Ray ray = playerCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)); // Create a ray from the camera to the center of the screen
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer)) //Check if raycast hits an interactable object
        {
            isLookingAtInteractable = true;
            currentInteractable = hit.collider.gameObject; //Get the interactable object

            var interactableObject = currentInteractable.GetComponent<InteractableObject>();
            canHoldInteract = interactableObject != null && interactableObject.interactionType == InteractableObject.InteractionType.Hold; // Check if the object is of type Hold

            if (interactionPrompt != null)
            {
                interactionPrompt.text = canHoldInteract ? "Hold [E] to interact" : "Press [E] to interact"; //Change the prompt text based on interaction type
                interactionPrompt.gameObject.SetActive(true);
            }
        }
        else
        {
            isLookingAtInteractable = false;
            canHoldInteract = false;
            currentInteractable = null;

            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);

            if (holdProgressBar != null)
            {
                holdProgressBar.value = 0;
                holdProgressBar.gameObject.SetActive(false);
            }
        }
    }

    void OnMove(InputValue moveVal) //Handle player movement input
    {
        movement = moveVal.Get<Vector2>();
    }

    void OnLook(InputValue lookVal) //Handle mouse look input
    {
        mouseMovement = lookVal.Get<Vector2>();
    }

    public void EnterComputerCam() //Enter the computer camera
    {
        inComputerCamera = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerCam.enabled = false;
        interactionCam.enabled = true;

        StartButtonClickingMiniGame();
    }

    public void ExitComputerCam() //Exit the computer camera
    {
        inComputerCamera = false;
        isButtonClickingGameActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerCam.enabled = true;
        interactionCam.enabled = false;

        buttonClickingCanvas.SetActive(false);
    }

    void StartButtonClickingMiniGame() //Start the button clicking mini-game
    {
        isButtonClickingGameActive = true;
        buttonClickingCanvas.SetActive(true);
        buttonClickTimer = 0f;
        buttonsClicked = 0;
        gameStatusText.text = "Click all the buttons!";

        foreach (Button button in buttons)
        {
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners(); // Clear old listeners
            button.onClick.AddListener(() => OnButtonClicked(button)); // Add new listener for click event instead of manually assigning in Inspector
        }
    }



    void OnButtonClicked(Button clickedButton) //Handle button click
    {
        clickedButton.gameObject.SetActive(false);
        buttonsClicked++;

        if (buttonClickTimer >= buttonClickTimeLimit) //If time is up, reset the game
        {
            ResetButtonMiniGame();
            StartButtonClickingMiniGame();
        }
        else if (buttonsClicked == buttons.Length) //If all buttons are clicked, exit the mini-game
        {
            if (computerObject != null)
            {
                computerObject.SetActive(false);
                computerToSwitchWith.SetActive(true);
            }
            ExitComputerCam();
        }
    }

    void ResetButtonMiniGame() //Reset the button mini-game
    {
        isButtonClickingGameActive = false;
        buttonClickTimer = 0f;
        buttonsClicked = 0;

        foreach (Button button in buttons) // Reset all buttons
        {
            button.gameObject.SetActive(true); 
        }

        buttonClickingCanvas.SetActive(false);
    }

}
