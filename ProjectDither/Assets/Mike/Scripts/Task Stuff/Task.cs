using UnityEngine;

public abstract class Task : MonoBehaviour
{
    public string taskName;
    public int roomNumber;
    public abstract void Complete();

    public MikesTaskManager taskManager;
    public GameObject taskPrefab; // Prefab to spawn

    protected void TaskCompleted()
    {
        if (taskManager != null)
        {
            taskManager.TaskCompleted(taskName);
        }
        else
        {
            Debug.LogError("TaskManager not assigned in " + taskName + " script!");
        }
    }

    public virtual void InitializeTask()
    {
        // Optional: Base initialization logic
    }
}