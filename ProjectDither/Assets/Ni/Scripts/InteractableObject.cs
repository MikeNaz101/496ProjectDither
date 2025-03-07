using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [System.Obsolete]
    public void Interact()
    {
        Debug.Log ("Object Interacted With: " +gameObject.name);
        gameObject.active = false;
    }
}
