using UnityEngine;
 using System.Collections; // Optional: If you need coroutines

 // Ensure your base Task class exists and is accessible
 // using YourTaskSystemNamespace; // If applicable

 public class PlayMinigameTask : Task // Inherit from your base Task class
 {
     [Header("Minigame Setup")]
     [Tooltip("The Canvas GameObject containing the minigame UI and controller script. Should start inactive.")]
     public GameObject minigameCanvasObject;

     [Tooltip("The script responsible for managing the minigame logic (must be on the Canvas object or its child).")]
     //public ExampleMinigameController minigameController; // Change 'ExampleMinigameController' if your script has a different name

     [Header("Player Control (Optional)")]
     //[Tooltip("(Optional) Reference to the player's movement script to disable/enable.")]
     public MonoBehaviour playerMovementScript; // Example: Assign NiPlayerMovement here if applicable

     private bool isMinigameActive = false;

     void Start()
     {
         // Initial validation
         if (minigameCanvasObject == null)
         {
             Debug.LogError($"PlayMinigameTask on '{gameObject.name}': Minigame Canvas Object is not assigned!");
             enabled = false;
             return;
         }
         /*if (minigameController == null)
         {
             // Try to find it if not assigned
             minigameController = minigameCanvasObject.GetComponentInChildren<ExampleMinigameController>(); // Change Type if needed
             if (minigameController == null)
             {
                 Debug.LogError($"PlayMinigameTask on '{gameObject.name}': Minigame Controller script not found on the Canvas object or its children, and not assigned!");
                 enabled = false;
                 return;
             }
              Debug.LogWarning($"PlayMinigameTask on '{gameObject.name}': Minigame Controller was found dynamically. Consider assigning it directly in the Inspector for clarity.");
         }*/

         // Ensure canvas starts inactive
         minigameCanvasObject.SetActive(false);
     }

     // Called when the player interacts with this task object
     public override void Activate()
     {
        //if (isMinigameActive || isCompleted) return; // Prevent re-activation if already active or done

         base.Activate(); // Call base class activation logic if needed
         Debug.Log($"Task '{taskName}' Activated: Starting Minigame.");

         isMinigameActive = true;

         // --- Prepare for Minigame ---
         // Disable player movement (optional but common)
         if (playerMovementScript != null)
         {
             playerMovementScript.enabled = false;
             Debug.Log("Disabled player movement.");
         }
         // Unlock and show cursor (common for UI interaction)
         Cursor.lockState = CursorLockMode.None;
         Cursor.visible = true;
         // ---------------------------

         // Activate the canvas
         minigameCanvasObject.SetActive(true);

         // Start the minigame controller, passing a reference to this task
         //minigameController.StartMinigame(this);
     }

     // This method is called BY the MinigameController when the game is finished successfully
     public void NotifyMinigameComplete()
     {
         if (!isMinigameActive) return; // Only complete if the minigame was actually started by this task

         Debug.Log($"Task '{taskName}': Received Minigame Complete notification.");
         Complete(); // Call the task's completion logic
     }

     // This method handles the task completion logic
     public override void Complete()
     {
         //if (isCompleted) return; // Prevent double completion

         // --- Clean up after Minigame ---
         // Restore player movement (if previously disabled)
         if (playerMovementScript != null)
         {
             playerMovementScript.enabled = true;
              Debug.Log("Re-enabled player movement.");
         }
         // Lock and hide cursor (common for FPS)
         Cursor.lockState = CursorLockMode.Locked;
         Cursor.visible = false;
         // -----------------------------

          // Deactivate the canvas
         if (minigameCanvasObject != null)
         {
             minigameCanvasObject.SetActive(false);
         }

         isMinigameActive = false;
         // Call the base Complete method AFTER cleaning up THIS task's specifics
         //base.Complete(); // This likely handles setting isCompleted flag and notifying TaskManager
         Debug.Log($"Task '{taskName}' (Minigame) is now complete.");
         // Ensure base.Complete() or similar calls TaskCompleted() if needed by your system
     }

     // Optional: Handle cancellation or failure
     public void NotifyMinigameFailedOrCancelled()
     {
          if (!isMinigameActive) return;
          Debug.Log($"Task '{taskName}': Minigame Failed or Cancelled.");

          // --- Clean up (same as completion) ---
         if (playerMovementScript != null) playerMovementScript.enabled = true;
         Cursor.lockState = CursorLockMode.Locked;
         Cursor.visible = false;
         if (minigameCanvasObject != null) minigameCanvasObject.SetActive(false);
         // ------------------------------------

         isMinigameActive = false;
         // Decide how to handle failure - does the task reset? Is it removed?
         // For now, just reset state without completing. Player might need to reactivate.
     }
 }