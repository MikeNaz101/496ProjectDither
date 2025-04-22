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
    public GameObject taskPrefab;

    public abstract void Complete();

    public virtual void InitializeTask() { }

    public virtual void Activate()
    {
        Debug.Log("Task Activated (Base)");
        // Access TaskManager using Task.taskManagerInstance
        if (taskManagerInstance != null)
        {
            // Do something with the task manager
        }
        else
        {
            Debug.LogError("TaskManager not set! Make sure to call Task.SetTaskManager.");
        }
    }

    void Start()
    {
        if (taskManagerInstance == null)
        {
            Debug.LogError($"TaskManager not found when task '{taskName}' started. Ensure TaskManager is initialized before tasks.");
        }
    }

    protected void TaskCompleted()
    {
        if (taskManagerInstance != null)
        {
            taskManagerInstance.RemoveTask(this.taskName);
            Debug.Log($"Task '{taskName}' completed: {taskName}");
        }
        else
        {
            Debug.LogError($"TaskManager is null on task '{taskName}'! Cannot report completion.");
        }
    }
}