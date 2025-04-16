using UnityEngine;
using UnityEngine.SceneManagement; 

public class RetryButton : MonoBehaviour
{
    // Put the name of your MAIN game scene here in the Inspector.
    [Tooltip("The name of the main game scene to reload.")]
    public string mainGameSceneName = "MainGame"; 

    // This Awake() function runs as soon as this script wakes up in the new scene!
    void Awake()
    {
        // Let that cursor run wild and free!
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true; 

        Debug.Log("Cursor unlocked and visible! Ready to click!"); 
    }

    // This function will be called when the button is clicked! How fruity!
    public void OnRetryPressed()
    {
        Debug.Log("Let's try again, my little kumquat!"); 
        
        // Load the main game scene again! Fresh start!
        // Consider adding code in your *main game scene* to re-lock the cursor if needed!
        SceneManager.LoadScene(mainGameSceneName);
    }

    // --- Button Setup Notes ---
    // 1. Attach this script to the Button GameObject in your Lose Scene.
    // 2. In the Inspector, find the Button component.
    // 3. Look for the 'On Click ()' section. Click the little '+' sign.
    // 4. Drag the Button GameObject (the one with this script) from the Hierarchy into the 'None (Object)' slot.
    // 5. Click the 'No Function' dropdown, find your script name ('RetryButton'), and select the 'OnRetryPressed()' function.
    // 6. Make sure the scene name in the 'mainGameSceneName' field matches your actual game scene's name AND that it's in the Build Settings!
}