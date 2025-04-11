using UnityEngine;

public abstract class Task : MonoBehaviour
{
    public string taskName;
    public int roomNumber;
    public GameObject taskPrefab;
    public MikesTaskManager taskManager;

    public abstract void Complete();

    public virtual void InitializeTask()
    {
        // Optional base initialization
    }

    public virtual void Activate()
    {
        // Optional base activation
    }
}