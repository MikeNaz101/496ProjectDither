
using UnityEngine;

 /// <summary>
 /// This Task requires the player to pick up an item tagged "FoodItem"
 /// and then interact with the GameObject this script is attached to (e.g., a dog or food bowl)
 /// to complete the task.
 /// Assumes a base 'Task' class exists.
 /// </summary>
 public class FeedTheDog : Task // Inherit from your base Task class
 {
     [Header("Task Settings")]
     [Tooltip("Tag identifying the food item the player needs to hold.")]
     public string requiredFoodTag = "FoodItem";

     [Tooltip("Tag identifying the player's item attachment point.")]
     public string playerAttachPointTag = "AttachPoint";

     // Internal state
     private Transform playerAttachPoint; // Found at runtime
     private bool taskIsComplete = false; // Use internal flag to prevent re-completion

     // It's often better to find references in Start or Awake
     // Using Start() as an example lifecycle method.
     void Start()
     {
         // Call standard initialization logic
         InitializeTask();
     }

     /// <summary>
     /// Initializes the task, finding necessary references like the player's attach point.
     /// </summary>
     public override void InitializeTask()
     {
         // Set default task name if not set in Inspector
         if (string.IsNullOrEmpty(taskName))
         {
             taskName = "Feed the Dog";
         }

         // Find the player's attachment point once
         // Using GameObject.FindGameObjectWithTag is okay in Start/Initialization but avoid in Update.
         GameObject attachPointObject = GameObject.FindGameObjectWithTag(playerAttachPointTag);
         if (attachPointObject != null)
         {
             playerAttachPoint = attachPointObject.transform;
             Debug.Log($"FeedTheDog Task on '{gameObject.name}': Found player attach point '{playerAttachPoint.name}'.");
         }
         else
         {
             // Log an error if the attach point isn't found, as it's critical for checking held items.
             Debug.LogError($"FeedTheDog Task on '{gameObject.name}': Could not find GameObject with tag '{playerAttachPointTag}'! Task cannot check for held item.");
             // Optionally disable the component if it cannot function without the attach point.
             // this.enabled = false;
         }
     }

     /// <summary>
     /// Called when the player interacts with THIS GameObject (the dog/bowl).
     /// Checks if the player is holding the required food item.
     /// </summary>
     public override void Activate()
     {
         // Prevent activating if the task is already marked as complete.
         if (taskIsComplete)
         {
             Debug.Log($"Task '{taskName}' on '{gameObject.name}' is already complete.");
             return;
         }

         // Call base class activation logic if it exists and is needed.
         // base.Activate(); // Uncomment if your base Task class has activation logic.

         Debug.Log($"Player interacted with '{gameObject.name}' for task '{taskName}'. Checking for food...");

         // Check if the player is holding the food item.
         GameObject heldFood = FindHeldFoodItem();

         if (heldFood != null)
         {
             // Player has the required food item.
             Debug.Log($"Player is holding food ('{heldFood.name}'). Completing task '{taskName}'.");

             // --- Task Success Logic ---
             // Optionally: Trigger dog eating animation/sound here
             // Example: GetComponent<Animator>()?.SetTrigger("Eat");

             // Consume the food item by destroying its GameObject.
             Destroy(heldFood);

             // Complete the task (calls task-specific logic and notifies manager).
             Complete(); // Calls the Complete method below.

             // Mark internally as complete to prevent re-activation issues.
             taskIsComplete = true;
         }
         else
         {
             // Player does not have the required food item.
             Debug.Log($"Player needs to hold the dog food ('{requiredFoodTag}' tag) first to complete task '{taskName}'!");
             // Provide feedback to the player (e.g., UI message, sound).
             // Example: UIManager.Instance.ShowMessage("You need to find the dog food first!");
         }
     }

     /// <summary>
     /// Helper method to check if the player's attach point holds an item with the required tag.
     /// </summary>
     /// <returns>The GameObject of the held food item, or null if not found.</returns>
     private GameObject FindHeldFoodItem()
     {
         // Ensure the attach point reference is valid.
         if (playerAttachPoint == null)
         {
             // Attempt to find it again, just in case it was missed in Start (though less ideal).
             GameObject attachPointObject = GameObject.FindGameObjectWithTag(playerAttachPointTag);
             if (attachPointObject != null) {
                 playerAttachPoint = attachPointObject.transform;
             } else {
                 Debug.LogError($"FeedTheDog Task: Cannot check for held item - Player Attach Point with tag '{playerAttachPointTag}' not found.");
                 return null; // Attach point wasn't found.
             }
         }

         // Iterate through all direct children of the attach point.
         foreach (Transform child in playerAttachPoint)
         {
             // Check if the child's tag matches the required food tag.
             if (child.CompareTag(requiredFoodTag))
             {
                 // Found the food item.
                 return child.gameObject;
             }
         }

         // No child found with the required tag.
         return null;
     }

     /// <summary>
     /// Contains task-specific logic that runs when the task is successfully completed.
     /// Also notifies the Task Manager.
     /// </summary>
     public override void Complete()
     {
         // Ensure this logic only runs once.
         /*if (!isCompleted) // Check the isCompleted flag from the base Task class
         {
             Debug.Log($"Task Specific Completion Logic: '{taskName}' on '{gameObject.name}' finished.");

             // Add any other effects for completing this specific task (e.g., dog happy animation, reward).
             // Example: GetComponent<Animator>()?.SetTrigger("Happy");

             // IMPORTANT: Call the base class's Complete method or TaskCompleted method
             // to ensure it's marked as done in your task management system.
             // Choose ONE of the following based on your base Task class structure:
             base.Complete(); // If your base class handles setting 'isCompleted' and notifying manager.
             // OR
             // TaskCompleted(); // If you need to explicitly call the notification method.

             // Note: taskIsComplete flag in *this* script helps prevent Activate() issues after completion.
             // isCompleted flag in the *base* class likely handles manager updates.
         }*/
     }
 }