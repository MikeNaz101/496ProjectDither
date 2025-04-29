using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToStartScreen : MonoBehaviour
{
    // Put the name of your Start Screen scene here in the Inspector.
    [Tooltip("The name of the scene to load when this button is clicked.")]
    public string startScreenSceneName = "StartScreen";

    // This function will be called when the button is clicked.
    public void OnButtonClicked()
    {
        Debug.Log("Button clicked! Loading: " + startScreenSceneName);

        // Load the scene with the specified name.
        SceneManager.LoadScene(startScreenSceneName);
    }

    // --- Button Setup Notes in Unity ---
    // 1. Attach this script to the GameObject that has your Button component.
    //    This could be the Button itself or a parent GameObject.
    // 2. In the Inspector, find the Button component on that GameObject.
    // 3. Look for the 'On Click (List)' section at the bottom of the Button component.
    // 4. Click the '+' button to add a new event listener.
    // 5. Drag the GameObject that has this script attached (from step 1) into the 'None (Object)' field of the new event.
    // 6. In the 'No Function' dropdown, find the name of this script ('GoToStartScreen') and then select the 'OnButtonClicked ()' function.
    // 7. Make sure the 'startScreenSceneName' variable in the Inspector is set to the exact name of your Start Screen scene in your Project window AND that the Start Screen scene is added to your Build Settings (File -> Build Settings...).
}