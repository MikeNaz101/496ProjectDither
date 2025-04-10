using UnityEngine;

public class AnswerThePhone : Task
{
    public AudioSource phoneRinging;
    public AudioSource phoneMessage;

    private GameObject phoneInstance; // Reference to the spawned phone
    private Collider phoneCollider; // Reference to the phone's collider

    void OnMouseDown()
    {
        if (!phoneCollider.enabled)
            return; // Prevent interaction if not active

        phoneRinging.Stop();
        phoneMessage.Play();
        TaskCompleted();
        phoneCollider.enabled = false; // Deactivate after use
    }

    public override void Complete()
    {
        Debug.Log("Task Completed: Answer the phone");
        // Any task-specific completion logic here
    }

    public override void InitializeTask()
    {
        // Find the spawned phone
        phoneInstance = GameObject.FindGameObjectWithTag("Phone");
        if (phoneInstance == null)
        {
            Debug.LogError("Phone not found in the scene!");
            return;
        }

        phoneCollider = phoneInstance.GetComponent<Collider>();
        if (phoneCollider == null)
        {
            Debug.LogError("Phone has no collider!");
            return;
        }

        // Activation Logic
        phoneCollider.enabled = true; // Make the phone interactable
        phoneRinging.Play(); // Start ringing
    }

    void Start()
    {
        // We'll move the initial ringing to InitializeTask()
    }
}