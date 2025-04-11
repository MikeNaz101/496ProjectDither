using UnityEngine;

public class AnswerThePhone : Task
{
    public AudioSource phoneRinging;
    public AudioSource phoneMessage;

    private GameObject phoneInstance; // Reference to the spawned phone
    private Collider phoneCollider; // Reference to the phone's collider

    public override void Activate()
    {
        phoneRinging.Stop();
        phoneMessage.Play();
        //TaskCompleted(); // REMOVE THIS LINE
        //phoneCollider.enabled = false; // Deactivate after use
    }

    public override void Complete()
    {
        Debug.Log("Task Completed: Answer the phone");
        // Any task-specific completion logic here
    }

    public override void InitializeTask()
    {
        phoneRinging.Play(); // Start ringing
    }

    void Start()
    {
        // We'll move the initial ringing to InitializeTask()
    }
}