using UnityEngine;

public class UnlockCursorOnLoad : MonoBehaviour
{
    void Start()
    {
        // Unlock the cursor
        Cursor.lockState = CursorLockMode.None;

        // Make the cursor visible
        Cursor.visible = true;

        Debug.Log("Cursor unlocked and made visible on scene load.");
    }
}