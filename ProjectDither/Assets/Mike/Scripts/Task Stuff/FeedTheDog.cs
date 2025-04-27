using UnityEngine;

public class FeedTheDog : Task
{
    public GameObject dog;
    private GameObject foodBowlInstance;
    public bool FoodCollected { get; private set; } = false; // Use a property for controlled access
    public bool TaskCompleted { get; private set; } = false; // Use a property
    private Dog dogScript; // To easily access the Dog script

    public override void Complete()
    {
        Debug.Log("Task Completed: Feed the dog");
        // Any task-specific completion logic (e.g., reward the player)
    }

    public override void InitializeTask()
    {
        // Find the spawned food bowl
        foodBowlInstance = GameObject.FindGameObjectWithTag("FoodBowl");
        if (foodBowlInstance == null)
        {
            Debug.LogError("FoodBowl not found in the scene for FeedTheDog task!");
        }
        if (taskName == null)
        {
            taskName = "Feed the Dog";
        }

        if (dog != null)
        {
            dogScript = dog.GetComponent<Dog>();
            if (dogScript == null)
            {
                Debug.LogError("Dog GameObject does not have a Dog script attached!");
            }
        }
        else
        {
            Debug.LogError("Dog GameObject is not assigned for FeedTheDog task!");
        }

        FoodCollected = false; // Initialize
        TaskCompleted = false; // Initialize
    }

    public override void Activate()
    {
        Debug.Log($"Dog reached the food bowl for task: {taskName}");
        if (FoodCollected && !TaskCompleted)
        {
            Complete();
            TaskCompleted();
            // Potentially trigger dog eating animation here (if not handled in Dog script)
        }
        else if (!FoodCollected)
        {
            Debug.Log("Dog reached the bowl, but food hasn't been collected yet.");
            // Optionally provide feedback
        }
        else if (TaskCompleted)
        {
            Debug.Log("Task already completed.");
        }
    }

    public void CollectFood()
    {
        if (!FoodCollected && !TaskCompleted)
        {
            FoodCollected = true;
            if (foodBowlInstance != null)
            {
                foodBowlInstance.SetActive(false); // Or handle food collection visually
            }
            Debug.Log("Dog food collected!");
        }
        else if (TaskCompleted)
        {
            Debug.Log("Cannot collect food, task is already completed.");
        }
        else if (FoodCollected)
        {
            Debug.Log("Food already collected.");
        }
    }
}