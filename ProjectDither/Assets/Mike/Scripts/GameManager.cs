using UnityEngine;
using System.Collections.Generic;
using TMPro; // Assuming your task list uses TextMeshPro
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [Header("Win Condition - Exit Portal")]
    [Tooltip("Prefab of the exit portal to spawn.")]
    public GameObject exitPortalPrefab;
    [Tooltip("Text to display for the 'find the exit' task.")]
    public string findExitTaskText = "Find the Exit";

    [Header("Task List References")]
    [Tooltip("Reference to the MikesTaskManager script.")]
    public MikesTaskManager taskManager; // Assign in Inspector
    [Tooltip("Reference to the TaskDisplayUI script in the scene.")]
    public TaskDisplayUI taskDisplayUI; // Assign in Inspector
    [Tooltip("Reference to an EnemyHitBehavior script in the scene to access GetRandomPointInRoom.")]
    public EnemyHitBehavior enemyHitBehaviorReference; // Assign in Inspector

    private bool gameWon = false; // Prevent win condition from triggering multiple times
    private GameObject spawnedExitPortal;

    void Start()
    {
        gameWon = false;
        if (taskManager == null)
        {
            Debug.LogError("GameManager needs a reference to MikesTaskManager!");
        }
        if (taskDisplayUI == null)
        {
            Debug.LogError("GameManager needs a reference to the TaskDisplayUI script!");
        }
        if (enemyHitBehaviorReference == null)
        {
            Debug.LogError("GameManager needs a reference to an EnemyHitBehavior script to use GetRandomPointInRoom!");
        }
    }

    // Called by MikesTaskManager when all initial tasks are completed
    public void AllTasksCompleted()
    {
        if (gameWon) return;

        gameWon = true;
        Debug.Log("All initial tasks completed! Spawning exit portal.");

        SpawnExitPortal();
        AddNewFindExitTaskToUI(); // Call the direct UI update method
    }

    void SpawnExitPortal()
    {
        if (exitPortalPrefab == null)
        {
            Debug.LogError("Exit Portal Prefab is not assigned in GameManager!");
            return;
        }

        if (taskManager == null || taskManager.roomGenerator == null || taskManager.roomGenerator.GetAllRooms() == null)
        {
            Debug.LogError("RoomGenerator not properly referenced via TaskManager!");
            return;
        }

        List<RoomGenerator7.Room> generatedRooms = taskManager.roomGenerator.GetAllRooms();
        if (generatedRooms.Count == 0)
        {
            Debug.LogError("No generated rooms found to spawn the exit portal in!");
            return;
        }

        RoomGenerator7.Room targetRoom = generatedRooms[Random.Range(0, generatedRooms.Count)];

        if (enemyHitBehaviorReference != null)
        {
            Vector3 spawnPosition = enemyHitBehaviorReference.GetRandomPointInRoom(targetRoom);
            spawnedExitPortal = Instantiate(exitPortalPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"Spawned exit portal in Room {targetRoom.id} at {spawnPosition}");
        }
        else
        {
            Debug.LogError("EnemyHitBehavior reference is missing, cannot get random point in room for exit portal!");
            // Fallback if no EnemyHitBehavior is referenced - spawn at room center
            spawnedExitPortal = Instantiate(exitPortalPrefab, targetRoom.roomBounds.center, Quaternion.identity);
            Debug.LogWarning($"EnemyHitBehavior reference missing. Spawning exit portal at Room {targetRoom.id} center.");
        }
    }

    void AddNewFindExitTaskToUI()
    {
        if (taskDisplayUI != null)
        {
            // We don't need to go through TaskManager for UI-only updates now.
            // Assuming TaskDisplayUI has a method to directly add a task name.
            // Let's add such a method to TaskDisplayUI.

            taskDisplayUI.AddTaskToDisplay(findExitTaskText);
            Debug.Log($"Added 'find the exit' task to the UI.");
        }
        else
        {
            Debug.LogError("TaskDisplayUI reference is missing, cannot add 'find the exit' task to UI.");
        }
    }
}