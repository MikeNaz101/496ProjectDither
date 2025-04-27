using UnityEngine;
using UnityEngine.AI; // If you're using Unity's navigation system

public class Dog1 : MonoBehaviour
{
    public Transform foodBowlTransform; // Assign the food bowl's transform in the Inspector
    public float movementSpeed = 3f;
    private NavMeshAgent navMeshAgent; // For pathfinding
    private FeedTheDog feedTheDogTask; // Reference to the task script
    private bool isMovingToBowl = false;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = movementSpeed;
            navMeshAgent.isStopped = true; // Start the dog stopped
        }
        else
        {
            Debug.LogWarning("NavMeshAgent not found on the dog. Consider adding one for movement.");
        }

        // Find the FeedTheDog task in the scene.
        // Again, you might need a more robust way to get this reference.
        feedTheDogTask = FindFirstObjectByType<FeedTheDog>();
        if (feedTheDogTask == null)
        {
            Debug.LogError("FeedTheDog task not found in the scene!");
        }
    }

    // This function would be called by your interaction logic when the player interacts
    // with the food bowl *while* the 'foodCollected' flag in the FeedTheDog task is true.
    public void StartMoveToBowl()
    {
        if (feedTheDogTask != null && feedTheDogTask.FoodCollected && !feedTheDogTask.TaskCompleted)
        {
            Debug.Log("Dog starting to move to food bowl.");
            isMovingToBowl = true;
            if (navMeshAgent != null && foodBowlTransform != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(foodBowlTransform.position);
            }
            else if (foodBowlTransform != null)
            {
                // Simple direct movement
            }
            // Potentially trigger dog moving animation
        }
        else if (feedTheDogTask != null && !feedTheDogTask.FoodCollected)
        {
            Debug.Log("Dog won't move yet. Need to collect food first.");
            // Optionally provide feedback
        }
        else if (feedTheDogTask != null && feedTheDogTask.TaskCompleted)
        {
            Debug.Log("Task already completed!");
        }
        else
        {
            Debug.LogWarning("FeedTheDog task reference is null. Cannot start moving.");
        }
    }

    void Update()
    {
        if (isMovingToBowl)
        {
            if (navMeshAgent != null && !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                Debug.Log("Dog reached the food bowl.");
                isMovingToBowl = false;
                // At this point, you might want to trigger the actual Complete() method
                // on the FeedTheDog task. However, based on your Activate() logic,
                // it seems the Complete() is called upon interaction with the dog
                // *after* collecting food. So, the movement here is more of a visual cue.

                // You might want to trigger an "eating" animation here.
                if (feedTheDogTask != null && !feedTheDogTask.TaskCompleted)
                {
                    feedTheDogTask.Activate(); // This will check if foodCollected is true and complete the task.
                }
            }
            // For simple direct movement (without NavMeshAgent):
            /*
            if (foodBowlTransform != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, foodBowlTransform.position, movementSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, foodBowlTransform.position) < 0.1f)
                {
                    Debug.Log("Dog reached the food bowl.");
                    isMovingToBowl = false;
                    if (feedTheDogTask != null && !feedTheDogTask.TaskCompleted)
                    {
                        feedTheDogTask.Activate();
                    }
                }
            }
            */
        }
    }
}