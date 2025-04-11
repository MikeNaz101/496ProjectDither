using UnityEngine;

public class InteractableObject1 : MonoBehaviour
{
    public enum InteractionType
    {
        Press,
        Hold
    }

    public InteractionType interactionType;
    public float interactionRange = 3.0f; // Add this, you'll likely need it

    // New: Store the Interactable layer as an integer
    private int interactableLayerMask;

    void Start()
    {
        // Get the layer mask for the "Interactable" layer
        interactableLayerMask = LayerMask.GetMask("Interactable");
    }

    public void Interact()
    {
        if (interactionType != InteractionType.Press) return;
        PerformInteraction();
    }

    public void HoldInteract()
    {
        if (interactionType != InteractionType.Hold) return;
        PerformInteraction();
    }

    private void PerformInteraction()
    {
        // Use a collider cast instead of FindGameObjectWithTag
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayerMask);

        if (hitColliders.Length > 0)
        {
            // Assuming you want to interact with the closest object, or the first one found:
            GameObject foundObject = hitColliders[0].gameObject;

            Task taskScriptInstance = foundObject.GetComponent<Task>();

            if (taskScriptInstance != null)
            {
                taskScriptInstance.Activate();
            }
            else
            {
                Debug.LogError("Found object with Interactable layer, but no Task component!");
            }
        }
        else
        {
            Debug.Log("No Interactable object found within range.");
        }
    }
}