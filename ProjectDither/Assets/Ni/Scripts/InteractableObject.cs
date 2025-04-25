using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public enum InteractionType
    {
        Press,
        Hold,
        Computer,
        HideAndSeek
    }
    
    public InteractionType interactionType; // Choose this in the Inspector

    [SerializeField]
    NiPlayerMovement player;
    [SerializeField]
    GameObject objectToSwitchWith;

    bool hasInteracted = false;

    public void Interact()
    {
        if (interactionType == InteractionType.Press && !hasInteracted) //For the press interaction
        {
            Debug.Log("Object Interacted With: " + gameObject.name);
            hasInteracted = true;
            gameObject.SetActive(false);
            objectToSwitchWith.SetActive(true);
        }
        /*else if (interactionType == InteractionType.Computer) // For the computer interaction
        {
            Debug.Log("Object Interacted With: " + gameObject.name);
            hasInteracted = true;
            if (player != null)
            {
                player.EnterComputerCam();
            }
        }
        // For a hide-and-seek Doll-type interaction
        else if (interactionType == InteractionType.HideAndSeek && !hasInteracted) //For the doll interaction
        {
            Debug.Log("Object Interacted With: " + gameObject.name);
            hasInteracted = true;
        }*/

    }

    public void HoldInteract()
    {
        if (interactionType == InteractionType.Hold && !hasInteracted) //For the hold interation
        {
            Debug.Log("Hold Interacted With: " + gameObject.name);
            hasInteracted = true;
            gameObject.SetActive(false);
            objectToSwitchWith.SetActive(true);
        }
    }

    //I added this mainly for the HideAndSeekObject script
    public void SwitchOutObject(Vector3 position)
    {
        gameObject.SetActive(false);
        if (objectToSwitchWith != null)
        {
            objectToSwitchWith.SetActive(true);
            objectToSwitchWith.transform.position = position; // Makes sure it appears where the doll was previously
        }
    }
}
