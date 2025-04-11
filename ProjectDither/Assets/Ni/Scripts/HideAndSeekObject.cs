using UnityEngine;
using System.Collections;

public class HideAndSeekObject : MonoBehaviour
{
    [SerializeField]
    GameObject ground;  // Use to detect the ground the doll will use to hide
    [SerializeField]
    Vector3 offset = new Vector3(0, 0.5f, 0); // to keep the toy (Doll) slightly above the surface

    int interactionCount = 0;
    bool canInteract = true;
    Vector3 hiddenPosition; // To store the last hidden position of the doll

    public void Interact()
    {
        if (!canInteract) return; // Prevent interaction if the doll is in transition

        if (interactionCount == 0)
        {
            // First interaction: The first time the player finds the doll, and it will hide again after the player interacts
            Debug.Log("Find the doll!");
            HideRandomlyOnGround();
            interactionCount++;
            canInteract = false; // Disable further interaction until the hiding is complete

            // Set a delay before the next interaction is allowed (It's an extra layer to prevent the interaction triggering multiple times)
            StartCoroutine(AllowInteractionAfterDelay(1f));
        }
        else if (interactionCount == 1)
        {
            // Second interaction: The doll won't hide again
            Debug.Log("You found the doll! You win!");
            interactionCount++;

            // Call the InteractableObject to switch out the doll with the new object
            var interactableObject = GetComponent<InteractableObject>();
            if (interactableObject != null)
            {
                interactableObject.SwitchOutObject(hiddenPosition); // Pass the hidden position
            }
        }
    }

    void HideRandomlyOnGround()
    {
        if (ground == null)
        {
            Debug.LogWarning("Ground not assigned!");
            return;
        }

        // Ensure the ground has a Renderer component
        Renderer groundRenderer = ground.GetComponent<Renderer>(); // Get the Renderer component from the ground object
        if (groundRenderer != null)
        {
            Bounds bounds = groundRenderer.bounds; // Get the bounds of the ground object

            // Generate random position on the ground surface
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            float y = bounds.max.y + offset.y; // Use the ground height to place the doll just above the surface

            // Store the current position where the doll was hidden
            hiddenPosition = new Vector3(randomX, y, randomZ);

            // Update the doll's position and make sure it's active
            transform.position = hiddenPosition;
            gameObject.SetActive(true);  // Make sure the doll is active after moving

            Debug.Log("Doll moved to: " + transform.position);
        }
        else
        {
            Debug.LogError("No Renderer found on ground!");
        }
    }

    // Coroutine to allow interaction after a delay
    IEnumerator AllowInteractionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canInteract = true; // Re-enable interaction after the delay
    }
}
