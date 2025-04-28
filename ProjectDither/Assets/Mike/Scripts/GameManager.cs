using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Task List UI Animation")]
    public Animator taskListAnimator; // Assign in Inspector or keep finding logic
    public string animatorHolderName = "HandWithPaper"; // Name of the object with the Animator
    public string boolParameterName = "IsTaskListVisible"; // Name of the boolean parameter

    [Header("Win Condition")]
    public GameObject winScreenUI; // Assign your Win Screen Panel/Canvas Group here in Inspector

    private bool isTaskListVisible = false;
    private bool gameWon = false; // Prevent win condition from triggering multiple times

    void Start()
    {
        // Try to find the Animator at start if not assigned
        if (taskListAnimator == null)
        {
             FindTaskListAnimator();
             if (taskListAnimator == null)
             {
                 Debug.LogWarning($"Animator on object '{animatorHolderName}' not found. Task list toggle may not work.");
             }
        }

        // Ensure Win Screen is initially hidden
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Win Screen UI is not assigned in GameManager. Win condition will not display UI.");
        }
        gameWon = false; // Reset win state on start
    }

    void FindTaskListAnimator()
    {
        // Find the object with the Animator
        GameObject animatorHolder = GameObject.Find(animatorHolderName);
        if (animatorHolder != null)
        {
            taskListAnimator = animatorHolder.GetComponent<Animator>();
        }
    }

    public void ToggleTaskListAnimation()
    {
        if (taskListAnimator != null)
        {
            isTaskListVisible = !isTaskListVisible;
            taskListAnimator.SetBool(boolParameterName, isTaskListVisible);
        }
        else
        {
            Debug.LogError($"Animator on object '{animatorHolderName}' not found, cannot toggle animation!");
        }
    }

    // Called by MikesTaskManager when all tasks are completed
    public void AllTasksCompleted()
    {
        if (gameWon) return; // Already won, do nothing

        gameWon = true; // Set flag to prevent re-triggering
        Debug.Log("--- GAME WON ---");

        // --- Add Your Win Condition Logic Here ---

        // Example 1: Activate a Win Screen UI
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(true);
            Debug.Log("Win Screen Activated.");
        }

        // Example 2: Stop player movement (if you have a player controller script)
        // FindObjectOfType<PlayerMovement>()?.DisableMovement();

        // Example 3: Load a new scene (e.g., a credits scene)
        // UnityEngine.SceneManagement.SceneManager.LoadScene("CreditsScene");

        // Example 4: Unlock cursor
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;

        // Example 5: Play a victory sound
        // AudioSource victoryAudio = GetComponent<AudioSource>(); // Add an AudioSource component
        // if (victoryAudio != null) victoryAudio.Play(); // Assign a victory clip
    }
}