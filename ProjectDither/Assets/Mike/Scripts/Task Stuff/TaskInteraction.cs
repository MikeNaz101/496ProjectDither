using UnityEngine;

public class TaskInteraction : MonoBehaviour
{
    [SerializeField]
    float interactionRange = 3.0f;
    [SerializeField]
    LayerMask interactableLayer;
    [SerializeField]
    Camera playerCam; // Assign your player camera

    void Start()
    {
        if (playerCam == null)
        {
            playerCam = Camera.main; // Try to find it if not assigned
        }
    }

    public void InteractWithCurrentTask(GameObject interactableObject)
    {
        if (interactableObject != null)
        {
            // Check if the interactable object is a Task
            if (interactableObject.TryGetComponent<Task>(out Task task))
            {
                Debug.Log($"Left-clicked on task: {task.taskName} to activate.");
                task.Activate(); // Trigger the task's activation logic
            }
            // Check if it's the BreakTheTV task specifically
            else if (interactableObject.TryGetComponent<BreakTV>(out BreakTV breakTVTask))
            {
                Debug.Log($"Left-clicked on the TV to hit it!");
                breakTVTask.HitTV();
            }
            // Handle other interactive elements here using their specific scripts
            else
            {
                var interactable = interactableObject.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
}