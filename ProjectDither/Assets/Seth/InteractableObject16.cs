using UnityEngine;

public class InteractableObject16 : MonoBehaviour
{
    [System.Obsolete]
    public void Interact()
    {
        Debug.Log ("Object Interacted With: " +gameObject.name);
        gameObject.active = false;
    }
}
