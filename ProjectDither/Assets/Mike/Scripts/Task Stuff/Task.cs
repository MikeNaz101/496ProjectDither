using UnityEngine;

public abstract class Task : MonoBehaviour
{
    // Public static property to hold a reference to the TaskManager
    public static MikesTaskManager taskManagerInstance;

    // Call this method to set the TaskManager instance
    public static void SetTaskManager(MikesTaskManager manager)
    {
        taskManagerInstance = manager;
    }

    public string taskName;
    public int roomNumber;
    public GameObject taskPrefab; // Prefab for the physical task object in the world

    // Each specific task MUST implement how it gets completed
    public abstract void Complete();

    public virtual void InitializeTask() { }

    public virtual void Activate()
    {
        Debug.Log($"Task Activated (Base): {taskName}");
        // Base activation logic if any
    }

    // Call this method from your specific task implementations (e.g., BreakTV.cs)
    // when the task's completion condition is met.
    protected void TaskCompleted()
    {
        if (taskManagerInstance != null)
        {
            // Tell the TaskManager this task is done.
            // The TaskManager will handle removal, UI update, and win condition check.
            taskManagerInstance.MarkTaskAsCompleted(this.taskName); // Changed method name for clarity
            Debug.Log($"Task '{taskName}' reported completion.");
        }
        else
        {
            Debug.LogError($"TaskManager is null on task '{taskName}'! Cannot report completion.");
        }

        // Optional: Destroy the task's MonoBehaviour component or GameObject if needed
        // Destroy(this.gameObject); // Or just Destroy(this); if only removing the script
    }

    void Start()
    {
        // No changes needed here, the Awake in TaskManager should set the instance before Start runs
        if (taskManagerInstance == null)
        {
            Debug.LogError($"TaskManager not found when task '{taskName}' started. Ensure TaskManager is initialized before tasks.");
        }
    }
}