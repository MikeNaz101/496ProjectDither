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
    }

    public override void InitializeTask()
    {
        // Find the spawned food bowl
        foodBowlInstance = GameObject.FindGameObjectWithTag("FoodBowl");
        if (foodBowlInstance == null)
        {
            Debug.LogError("FoodBowl not found in the scene!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (taskCompleted)
            return;

        if (other.gameObject == foodBowlInstance)
        {
            foodCollected = true;
            foodBowlInstance.SetActive(false);
            Debug.Log("Dog food collected!");
        }

        if (other.gameObject == dog && foodCollected)
        {
            TaskCompleted();
            taskCompleted = true;
        }
    }
}