using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class MikesTaskManager : MonoBehaviour
{
    public List<Task> availableTasks;
    public int numberOfTasks = 5;
    public TaskListUI taskListUI;
    public RoomGenerator6 roomGenerator;

    private Dictionary<int, List<Task>> tasksByRoom = new Dictionary<int, List<Task>>();
    private List<Task> activeTasks = new List<Task>();

    void Start()
    {
        Debug.Log("TaskManager Start() called.");

        if (roomGenerator == null)
        {
            Debug.LogError("TaskManager needs a reference to the RoomGenerator!");
            return;
        }

        roomGenerator.OnRoomsGenerated += AssignTasksToRooms;
        Debug.Log("TaskManager subscribed to RoomGenerator.OnRoomsGenerated.");
    }

    void AssignTasksToRooms(List<RoomGenerator6.Room> generatedRooms)
    {
        Debug.Log("TaskManager AssignTasksToRooms() called. Generated rooms count: " + generatedRooms.Count);

        if (taskListUI != null)
        {
            Debug.Log("MikesTaskManager: taskListUI is assigned!"); // RIGHT BEFORE the call
            taskListUI.SetTasks(activeTasks);
        }
        else
        {
            Debug.LogError("TaskListUI not assigned in TaskManager!");
        }

        if (availableTasks.Count == 0)
        {
            Debug.LogError("No available tasks to assign!");
            return;
        }

        tasksByRoom.Clear();
        activeTasks.Clear();

        // 1. Shuffle available tasks to randomize assignment
        List<Task> shuffledTasks = availableTasks.OrderBy(a => Random.value).ToList();
        Debug.Log("Shuffled tasks. Task count: " + shuffledTasks.Count);

        // 2. Distribute tasks (one per room for the first few)
        int taskIndex = 0;
        foreach (RoomGenerator6.Room room in generatedRooms)
        {
            if (taskIndex < numberOfTasks && taskIndex < shuffledTasks.Count)
            {
                Debug.Log("Assigning task to room " + room.id);

                Task newTask = Instantiate(shuffledTasks[taskIndex]);
                newTask.roomNumber = room.id;
                newTask.taskManager = this;
                activeTasks.Add(newTask);

                Debug.Log("Instantiated task: " + newTask.taskName + ". Room number: " + newTask.roomNumber);

                if (!tasksByRoom.ContainsKey(room.id))
                {
                    tasksByRoom[room.id] = new List<Task>();
                    Debug.Log("Created new task list for room " + room.id);
                }
                tasksByRoom[room.id].Add(newTask);
                Debug.Log("Added task to room " + room.id + ". Task count in room: " + tasksByRoom[room.id].Count);

                taskIndex++;

                // 3. Spawn the associated item in the room
                if (newTask.taskPrefab != null)
                {
                    Vector3 spawnPosition = GetRandomPointInRoom(room);
                    Debug.Log("Spawning task prefab: " + newTask.taskPrefab.name + " at position: " + spawnPosition);
                    Instantiate(newTask.taskPrefab, spawnPosition, Quaternion.identity);
                    Debug.Log("Task prefab spawned.");
                }
                else
                {
                    Debug.LogWarning("Task prefab is null for task: " + newTask.taskName);
                }
                newTask.InitializeTask();
                Debug.Log("Task Initialized.");
            }
            else
            {
                Debug.Log("Reached task limit or no more tasks. Breaking loop.");
                break; // Stop if we've assigned enough tasks
            }
        }

        if (taskListUI != null)
        {
            taskListUI.SetTasks(activeTasks); // Send active tasks to UI
            Debug.Log("TaskListUI updated with active tasks. Active task count: " + activeTasks.Count);
        }
        else
        {
            Debug.LogError("TaskListUI not assigned in TaskManager!");
        }
    }

    // Helper method to get a random point within a room
    Vector3 GetRandomPointInRoom(RoomGenerator6.Room room)
    {
        float x = Random.Range(room.roomBounds.min.x + 1f, room.roomBounds.max.x - 1f);
        float z = Random.Range(room.roomBounds.min.z + 1f, room.roomBounds.max.z - 1f);
        Vector3 spawnPos = new Vector3(x, 0.5f, z);
        Debug.Log("Generated random spawn position: " + spawnPos + " for room " + room.id);
        return spawnPos; // Adjust Y as needed
    }

    public List<Task> GetTasksForRoom(int roomId)
    {
        if (tasksByRoom.ContainsKey(roomId))
        {
            Debug.Log("Returning tasks for room " + roomId + ". Task count: " + tasksByRoom[roomId].Count);
            return tasksByRoom[roomId];
        }
        Debug.Log("No tasks found for room " + roomId + ". Returning empty list.");
        return new List<Task>();
    }
}