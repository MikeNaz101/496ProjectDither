using UnityEngine;

public class AnswerThePhone : Task
{
    public AudioSource phoneRinging;
    public AudioSource phoneMessage;
    // public GameObject phoneInstance; // You might need this for visual changes
    // public Collider phoneCollider; // You might need this to disable interaction

    public override void Activate()
    {
        Debug.Log($"Player answered the phone for task: {taskName}");
        phoneRinging.Stop();
        phoneMessage.Play();
        TaskCompleted(); // Mark the task as completed upon activation
        // If you have visual elements or want to disable further interaction:
        // if (phoneInstance != null) { /* Change appearance */ }
        // if (phoneCollider != null) { phoneCollider.enabled = false; }
    }

    public override void Complete()
    {
        Debug.Log("Task Completed: Answer the phone");
        // Any task-specific completion logic (e.g., update game state)
    }

    public override void InitializeTask()
    {
        phoneRinging.Play(); // Start ringing when the task is initialized
    }

    void Start()
    {
        if (taskName == null)
        {
            taskName = "Answer the Phone";
        }
    }
}