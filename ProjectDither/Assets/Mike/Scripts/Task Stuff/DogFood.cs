using UnityEngine;
using UnityEngine.UI; // Required for Graphic check

public class DogFood : MonoBehaviour
{
    [Header("Dog Food Settings")]
    public AudioClip barkSound; // Assign the dog's bark sound in the Inspector

    private GameObject handImageObject; // Found at runtime using "Hand" tag
    private Transform attachPointTransform; // Found at runtime using "AttachPoint" tag
    private bool isHeld = false;
    private Rigidbody rb;
    private Collider coll;
    private AudioSource audioSource;
    private FeedTheDog feedTheDogTask; // Reference to the task script

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // Ensure necessary components exist
        if (rb == null || coll == null || audioSource == null)
        {
            Debug.LogError("DogFoodPickup: Missing Rigidbody, Collider, or AudioSource!");
            enabled = false;
            return;
        }

        // Find attach point
        GameObject attachPointObject = GameObject.FindGameObjectWithTag("AttachPoint");
        if (attachPointObject != null)
        {
            attachPointTransform = attachPointObject.transform;
            Debug.Log("DogFoodPickup: Found attach point object: " + attachPointObject.name);
        }
        else
        {
            Debug.LogError("DogFoodPickup: No GameObject found with the tag 'AttachPoint'!");
            enabled = false;
            return; // Stop if attach point is missing
        }

        // Find Hand UI Object by Tag
        handImageObject = GameObject.FindGameObjectWithTag("Hand");
        if (handImageObject == null)
        {
            Debug.LogWarning("DogFoodPickup: Could not find GameObject with tag 'Hand'. Make sure your hand UI GameObject exists, is active, and has the 'Hand' tag assigned. Hand UI will not be hidden/shown.");
        }
        else
        {
            Debug.Log("DogFoodPickup: Found hand UI object by tag: " + handImageObject.name);
            // Optional sanity check
            if (handImageObject.GetComponent<Graphic>() == null)
            {
                Debug.LogWarning($"DogFoodPickup: GameObject '{handImageObject.name}' tagged as 'Hand' does not have a Graphic component (like RawImage or Image).");
            }
        }

        // Find the FeedTheDog task in the scene.
        feedTheDogTask = FindFirstObjectByType<FeedTheDog>();
        if (feedTheDogTask == null)
        {
            Debug.LogError("DogFoodPickup: FeedTheDog task not found in the scene!");
        }
    }

    // This function would be called when the player interacts with the dog food
    public void Pickup()
    {
        if (!isHeld && attachPointTransform != null)
        {
            isHeld = true;
            rb.isKinematic = true;
            coll.enabled = false;

            transform.SetParent(attachPointTransform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity; // Adjust rotation as needed

            // HIDE HAND UI (checks if found in Start)
            if (handImageObject != null)
            {
                handImageObject.SetActive(false);
            }

            // Play bark sound
            if (barkSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(barkSound);
            }

            // Trigger food collection in the task
            if (feedTheDogTask != null)
            {
                feedTheDogTask.CollectFood();
            }
            else
            {
                Debug.LogWarning("DogFoodPickup: FeedTheDog task reference is null. Cannot collect food.");
            }

            Debug.Log("Dog food picked up and attached to: " + attachPointTransform.name);
        }
        else if (attachPointTransform == null)
        {
            Debug.LogError("DogFoodPickup: Cannot pickup dog food because AttachPoint was not found during Start().");
        }
    }

    // You might want to add a Drop function here if the player can drop the food.
    public void Drop()
    {
        if (isHeld)
        {
            isHeld = false;

            transform.SetParent(null); // Unparent
            rb.isKinematic = false;
            coll.enabled = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // SHOW HAND UI (checks if found in Start)
            if (handImageObject != null)
            {
                handImageObject.SetActive(true);
            }

            Debug.Log("Dog food dropped!");
        }
    }

    // You might not need an Update function for just picking up.
    // Update could be used if you want to add functionality while holding the food.
    // void Update()
    // {
    //     if (isHeld)
    //     {
    //         // Add any actions while holding the food here
    //         if (Input.GetMouseButtonDown(1)) // Example: Right click to drop
    //         {
    //             Drop();
    //         }
    //     }
    // }
}