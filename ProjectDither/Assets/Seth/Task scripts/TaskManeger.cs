using UnityEngine;
using TMPro;


public class TaskManager : MonoBehaviour
{
    //Define a simple task struct
    //[SerializeFeild]
    public TMP_Text taskDisplay; // Single TextMeshPro text element
    public struct Task
    {
        public string taskName;
        public bool isCompleted;
        public int priority;
        public int taskNumber;
    }

    // Declare an array of Task structs
    public Task[] taskListing;
    //Phone_Answer answerPhone = Get <Phone_Answer>;

    public enum State 
    {
        Incomplete,
        Active,
        Finished
    }
    void Start()
    {
        // Initialize the array (you could also do this in the Inspector)
        taskListing = new Task[3];

        taskListing[0].taskName = "Answer The phone";
        taskListing[0].isCompleted = false;
        taskListing[0].taskNumber = 0;

        taskListing[1].taskName = "Task2";
        taskListing[1].isCompleted = false;
        taskListing[1].priority = 2;

        taskListing[2].taskName = "Task3";
        taskListing[2].isCompleted = true;
        taskListing[2].priority = 3;

        // Example: Loop through the array and print task names
        foreach (Task task in taskListing)
        {
            Debug.Log("Task: " + task.taskName + ", Completed: " + task.isCompleted + ", Priority: " + taskListing);
        }

        //Example: Access a specific task and modify it.
        taskListing[1].isCompleted = true;
        Debug.Log("Task: " + taskListing[1].taskName + ", Completed: " + taskListing[1].isCompleted);
    }

    // Example function to mark a task as completed (by index)
    public void CompleteTask(int taskIndex)
    {
        if (taskIndex >= 0 && taskIndex < taskListing.Length)
     {
           taskListing[taskIndex].isCompleted = true;
            Debug.Log("Task '" + taskListing[taskIndex].taskName + "' completed!");
        }
        else
        {
            Debug.LogError("Invalid task index.");
        }
    }
}