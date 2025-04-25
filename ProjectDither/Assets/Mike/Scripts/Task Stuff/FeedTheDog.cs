using UnityEngine;

public class FeedTheDog : Task
{
    public GameObject dog;
    private GameObject foodBowlInstance;
    private bool foodCollected = false;
    private bool taskCompleted = false;

    public override void Complete()
    {
        Debug.Log("Task Completed: Feed the dog");
        // Any task-specific completion logic
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
    }

    public override void Activate()
    {
        Debug.Log($"Player interacted to feed the dog for task: {taskName}");
        // This Activate method now represents the player's interaction.
        // You'll likely need more specific logic here based on your gameplay.

        // Example logic: If the player interacts near the dog AND has "collected" food:
        // (You'll need to track the 'foodCollected' state based on other player actions)
        if (dog != null && foodCollected && !taskCompleted)
        {
            Complete();
            TaskCompleted();
            taskCompleted = true;
            // Potentially trigger dog eating animation, etc.
        }
        else if (dog != null && !foodCollected)
        {
            Debug.Log("Need to collect food first!");
            // Optionally provide feedback to the player
        }
        else if (dog == null)
        {
            Debug.LogError("Dog GameObject is not assigned or found for FeedTheDog task!");
        }
    }

    // Example of how you might track food collection (this would be in another script or within this one based on player actions):
    public void CollectFood()
    {
        foodCollected = true;
        if (foodBowlInstance != null)
        {
            foodBowlInstance.SetActive(false); // Or handle food collection visually
        }
        Debug.Log("Dog food collected!");
    }

    // You might have a separate interaction when the player is near the dog *after* collecting food that calls Activate.
}