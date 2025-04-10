using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public enum InteractionType
    {
        Press,
        Hold
    }

    public InteractionType interactionType; // You choose this in the Inspector

    public void Interact()
    {
        if (interactionType != InteractionType.Press) return;

        Debug.Log("Object Interacted With: " + gameObject.name);
        gameObject.SetActive(false);
    }

    public void HoldInteract()
    {
        if (interactionType != InteractionType.Hold) return;

        Debug.Log("Hold Interacted With: " + gameObject.name);
        gameObject.SetActive(false);
    }
}
