using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class MikesTaskManager : MonoBehaviour
{
    [Header("Task Setup")]
    public List<Task> availableTasks; // Assign task prefabs here in Inspector
    public int numberOfTasks = 5; // How many tasks to select and activate

    [Header("References")]
    public TaskDisplayUI taskListUI; // Assign the UI handler in Inspector
    public RoomGenerator7 roomGenerator; // Assign RoomGenerator in Inspector
    public GameManager gameManager; // Assign GameManager in Inspector

    [Header("Runtime Data (Read Only)")]
    [SerializeField] private List<Task> activeTasks = new List<Task>();
    [SerializeField] private int totalTasksAssigned = 0;
    [SerializeField] private int tasksCompletedCount = 0;

    // --- Private Runtime Data ---
    private Dictionary<int, List<Task>> tasksByRoom = new Dictionary<int, List<Task>>();
    // Removed player finding logic as it wasn't directly used for task completion reporting
    // private GameObject player;
    // private string playerTag = "Player";
    // private TaskDisplayUI taskListUIComponent; // Cache is handled by public field now

    void Awake()
    {
        // Set the static TaskManager instance for Tasks to use
        Task.SetTaskManager(this);

        // Ensure required references are set
        if (gameManager == null)
        {
            Debug.LogError("GameManager reference is not set in MikesTaskManager!");
            // Optionally try to find it: gameManager = FindObjectOfType<GameManager>();
        }
        if (taskListUI == null)
        {
             Debug.LogError("TaskDisplayUI reference is not set in MikesTaskManager!");
             // Optionally try to find it: taskListUI = FindObjectOfType<TaskDisplayUI>();
             // Note: Finding UI might be better done via player reference if it's on the player
        }
         if (roomGenerator == null)
        {
             Debug.LogError("RoomGenerator reference is not set in MikesTaskManager!");
        }
    }

    void Start()
    {
        Debug.Log("TaskManager Start() called.");

        if (roomGenerator != null)
        {
             roomGenerator.OnRoomsGenerated += AssignTasksToRooms;
             Debug.Log("TaskManager subscribed to RoomGenerator.OnRoomsGenerated.");
        }
        else
        {
            Debug.LogError("TaskManager needs a reference to the RoomGenerator!");
        }
    }

    // Renamed from FindPlayerAndUI - only sets UI if needed
    void EnsureUIReference()
    {
        // This assumes taskListUI is assigned in the inspector.
        // If it needs to be found dynamically (e.g., on a player prefab):
        // if (taskListUI == null)
        // {
        //     GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        //     if (player != null) taskListUI = player.GetComponentInChildren<TaskDisplayUI>();
        //     if (taskListUI == null) Debug.LogError("TaskDisplayUI component not found!");
        // }

        if (taskListUI != null)
        {
            Debug.Log("MikesTaskManager: taskListUI is available!");
        }
        else
        {
            Debug.LogError("TaskListUI reference is missing!");
        }
    }

    void AssignTasksToRooms(List<RoomGenerator7.Room> generatedRooms)
    {
        Debug.Log("TaskManager AssignTasksToRooms() called. Generated rooms count: " + generatedRooms.Count);

        EnsureUIReference(); // Make sure we have the UI reference

        if (availableTasks.Count == 0)
        {
            Debug.LogError("No available tasks (prefabs) assigned in the Inspector!");
            return;
        }

        // Clear previous state
        tasksByRoom.Clear();
        activeTasks.Clear();
        tasksCompletedCount = 0;
        totalTasksAssigned = 0; // Reset for this assignment

        // Shuffle and select tasks
        List<Task> shuffledTasks = availableTasks.OrderBy(a => Random.value).ToList();
        Debug.Log("Shuffled available tasks. Count: " + shuffledTasks.Count);

        int taskIndex = 0;
        foreach (RoomGenerator7.Room room in generatedRooms)
        {
            // Stop if we've assigned enough tasks or run out of task types or rooms
            if (taskIndex >= numberOfTasks || taskIndex >= shuffledTasks.Count)
            {
                 Debug.Log($"Stopping task assignment. Reached limit ({numberOfTasks}) or ran out of task types ({shuffledTasks.Count}).");
                 break;
            }

             Debug.Log("Assigning task to room " + room.id);

            // Instantiate the Task component itself (often from a prefab containing the script)
            Task taskPrefabToInstantiate = shuffledTasks[taskIndex];
            if (taskPrefabToInstantiate == null) {
                Debug.LogError($"Available task at index {taskIndex} is null!");
                taskIndex++;
                continue; // Skip this one
            }

            Task newTask = Instantiate(taskPrefabToInstantiate); // Instantiate the prefab containing the Task script
            newTask.name = taskPrefabToInstantiate.name; // Keep original prefab name for easier debugging
            newTask.taskName = taskPrefabToInstantiate.taskName; // Ensure the taskName is copied
            newTask.roomNumber = room.id;
            activeTasks.Add(newTask); // Add the script instance to active list

            Debug.Log($"Instantiated Task Script: {newTask.taskName} (from prefab {taskPrefabToInstantiate.name}). Room number: {newTask.roomNumber}");

            // Add to room dictionary
            if (!tasksByRoom.ContainsKey(room.id))
            {
                tasksByRoom[room.id] = new List<Task>();
                Debug.Log("Created new task list for room " + room.id);
            }
            tasksByRoom[room.id].Add(newTask);
            Debug.Log($"Added task to room {room.id}. Task count in room: {tasksByRoom[room.id].Count}");

            taskIndex++;

            // Instantiate the *physical representation* of the task in the world (if any)
            if (newTask.taskPrefab != null)
            {
                Vector3 spawnPosition = GetRandomPointInRoom(room);
                Debug.Log($"Spawning physical task object: {newTask.taskPrefab.name} at position: {spawnPosition}");
                GameObject taskObject = Instantiate(newTask.taskPrefab, spawnPosition, Quaternion.identity);
                // Optional: Link the physical object back to the task script instance if needed
                // taskObject.GetComponent<SomeComponent>()?.SetTaskReference(newTask);
                Debug.Log("Physical task object spawned.");
            }
            else
            {
                // This might be okay if the task doesn't need a physical object (e.g., just UI based)
                Debug.LogWarning($"Physical task prefab (taskPrefab field) is null for task '{newTask.taskName}'. No object spawned in world.");
            }

            // Initialize the task (e.g., set up internal state)
            newTask.InitializeTask();
            Debug.Log($"Task Initialized: {newTask.taskName}");
        }

        // Set the total count AFTER assignment loop is finished
        totalTasksAssigned = activeTasks.Count;
        Debug.Log($"Total tasks assigned and active: {totalTasksAssigned}");

        // Update the UI with the list of assigned tasks
        if (taskListUI != null)
        {
            taskListUI.SetTasks(activeTasks); // Assumes SetTasks clears old list and adds new ones
            Debug.Log("TaskListUI updated with active tasks. Active task count: " + activeTasks.Count);
        }
        else
        {
            Debug.LogError("TaskListUI not found, cannot update task list!");
        }
    }

    Vector3 GetRandomPointInRoom(RoomGenerator7.Room room)
    {
        // Ensure bounds are valid before using them
        if (room.roomBounds.size == Vector3.zero) {
            Debug.LogError($"Room {room.id} has zero size bounds!");
            return Vector3.zero; // Or some default position
        }
        float padding = 1.0f; // Keep away from walls
        float x = Random.Range(room.roomBounds.min.x + padding, room.roomBounds.max.x - padding);
        float z = Random.Range(room.roomBounds.min.z + padding, room.roomBounds.max.z - padding);
        // Assuming Y=0 is floor level, spawn slightly above
        Vector3 spawnPos = new Vector3(x, 0.5f, z);
        Debug.Log($"Generated random spawn position: {spawnPos} for room {room.id}");
        return spawnPos;
    }

    public List<Task> GetTasksForRoom(int roomId)
    {
        if (tasksByRoom != null && tasksByRoom.ContainsKey(roomId))
        {
            // Debug.Log("Returning tasks for room " + roomId + ". Task count: " + tasksByRoom[roomId].Count);
            return tasksByRoom[roomId];
        }
        // Debug.Log("No tasks found for room " + roomId + ". Returning empty list.");
        return new List<Task>(); // Return empty list, not null
    }

    // This method is called by individual Task scripts when they are completed
    public void MarkTaskAsCompleted(string taskName)
    {
        // Find the task in the active list (we might need its instance later)
        Task completedTask = activeTasks.FirstOrDefault(t => t.taskName == taskName);

        if (completedTask != null)
        {
            // Check if it was already marked as complete somehow (safety check)
             if (!activeTasks.Contains(completedTask)) {
                 Debug.LogWarning($"Task '{taskName}' reported completion but was already removed from active list.");
                 return; // Already processed
             }

            Debug.Log($"TaskManager processing completion for: {taskName}");

            // 1. Increment Counter
            tasksCompletedCount++;
            Debug.Log($"Tasks completed: {tasksCompletedCount} / {totalTasksAssigned}");

            // 2. Update UI (Tell TaskDisplayUI to draw the line)
            if (taskListUI != null)
            {
                // --- YOU NEED TO IMPLEMENT THIS METHOD IN TaskDisplayUI ---
                taskListUI.MarkTaskCompleteUI(taskName);
                // This method should find the UI element for taskName and enable/configure its Line Renderer
                Debug.Log($"Requested UI update for completed task: {taskName}");
            }
            else
            {
                Debug.LogWarning("Cannot mark task complete on UI - TaskListUI reference is missing.");
            }

            // 3. Remove from internal tracking lists
            activeTasks.Remove(completedTask);
            if (tasksByRoom.ContainsKey(completedTask.roomNumber))
            {
                 tasksByRoom[completedTask.roomNumber].Remove(completedTask);
                 if (tasksByRoom[completedTask.roomNumber].Count == 0)
                 {
                     tasksByRoom.Remove(completedTask.roomNumber);
                 }
            }

            // 4. Check for Win Condition
            // Ensure totalTasksAssigned > 0 to prevent winning immediately if no tasks were assigned
            if (totalTasksAssigned > 0 && tasksCompletedCount >= totalTasksAssigned)
            {
                Debug.Log("All tasks completed! Notifying GameManager.");
                if (gameManager != null)
                {
                    gameManager.AllTasksCompleted(); // Notify GameManager
                }
                else
                {
                    Debug.LogError("Cannot notify GameManager of win - reference is missing!");
                }
            }
        }
        else
        {
            Debug.LogWarning($"TaskManager received completion for task '{taskName}', but it wasn't found in the active list. It might have already been completed or removed.");
        }
    }

    // --- Replaced RemoveTask with MarkTaskAsCompleted ---
    // public void RemoveTask(string taskNameToRemove) // Old method
    // { ... }
}