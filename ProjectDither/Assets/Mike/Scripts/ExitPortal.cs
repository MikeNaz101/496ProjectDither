using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitPortal : MonoBehaviour
{
    [Tooltip("The name of the win scene to load.")]
    public string winSceneName = "WinScene"; // Set your win scene name

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Make sure your player has the "Player" tag
        {
            Debug.Log("Player entered the exit portal! Loading win scene: " + winSceneName);
            SceneManager.LoadScene(winSceneName);

            // Optional: You could also notify the TaskManager to mark the "find the exit" task as complete here
            // FindObjectOfType<MikesTaskManager>()?.MarkTemporaryTaskAsCompleted("Find the Exit");
        }
    }
}