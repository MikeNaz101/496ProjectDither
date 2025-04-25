using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class MikesTaskManager : MonoBehaviour
{
    public List<Task> availableTasks;
    public int numberOfTasks = 5;
    public TaskDisplayUI taskListUI;
    public RoomGenerator7 roomGenerator;
    public string playerTag = "Player";

    private Dictionary<int, List<Task>> tasksByRoom = new Dictionary<int, List<Task>>();
    private List<Task> activeTasks = new List<Task>();
    private GameObject player;
    private TaskDisplayUI taskListUIComponent; // Cached reference

    void Awake()
    {
        // Set the TaskManager instance when it's created
        Task.SetTaskManager(this);
    }

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

    void FindPlayerAndUI()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                Debug.LogError($"No GameObject found with the tag '{playerTag}'!");
                return;
            }
        }

        if (taskListUIComponent == null)
        {
            taskListUIComponent = player.GetComponentInChildren<TaskDisplayUI>();
            if (taskListUIComponent == null)
            {
                Debug.LogError("TaskDisplayUI component not found on the player or its children!");
            }
            else
            {
                Debug.Log("TaskDisplayUI found on the player.");
            }
        }
        taskListUI = taskListUIComponent;
    }

    void AssignTasksToRooms(List<RoomGenerator7.Room> generatedRooms)
    {
        Debug.Log("TaskManager AssignTasksToRooms() called. Generated rooms count: " + generatedRooms.Count);

        FindPlayerAndUI();

        if (taskListUI != null)
        {
            Debug.Log("MikesTaskManager: taskListUI is available!");
            taskListUI.SetTasks(activeTasks);
        }
        else
        {
            Debug.LogError("TaskListUI not found, cannot update task list!");
        }

        if (availableTasks.Count == 0)
        {
            Debug.LogError("No available tasks to assign!");
            return;
        }

        tasksByRoom.Clear();
        activeTasks.Clear();

        List<Task> shuffledTasks = availableTasks.OrderBy(a => Random.value).ToList();
        Debug.Log("Shuffled tasks. Task count: " + shuffledTasks.Count);

        int taskIndex = 0;
        foreach (RoomGenerator7.Room room in generatedRooms)
        {
            if (taskIndex < numberOfTasks && taskIndex < shuffledTasks.Count)
            {
                Debug.Log("Assigning task to room " + room.id);

                Task newTask = Instantiate(shuffledTasks[taskIndex]);
                newTask.roomNumber = room.id;
                // newTask.taskManager = this; // No longer directly assigning
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

                if (newTask.taskPrefab != null)
                {
                    Vector3 spawnPosition = GetRandomPointInRoom(room);
                    Debug.Log("Spawning task prefab: " + newTask.taskPrefab.name + " at position: " + spawnPosition);
                    Instantiate(newTask.taskPrefab, spawnPosition, Quaternion.identity);
                    Debug.Log("Task prefab spawned.");
                }
                else
                {
                    Debug.LogWarning($"Task prefab is null for task '{newTask.taskName}' (index {taskIndex}) in availableTasks!");
                }
                newTask.InitializeTask();
                Debug.Log("Task Initialized.");
            }
            else
            {
                Debug.Log("Reached task limit or no more tasks. Breaking loop.");
                break;
            }
        }

        if (taskListUI != null)
        {
            taskListUI.SetTasks(activeTasks);
            Debug.Log("TaskListUI updated with active tasks. Active task count: " + activeTasks.Count);
        }
        else
        {
            Debug.LogError("TaskListUI not found, cannot update task list!");
        }
    }

    Vector3 GetRandomPointInRoom(RoomGenerator7.Room room)
    {
        float x = Random.Range(room.roomBounds.min.x + 1f, room.roomBounds.max.x - 1f); // Avoid spawning on walls
        float z = Random.Range(room.roomBounds.min.z + 1f, room.roomBounds.max.z - 1f); // Avoid spawning on walls
        Vector3 spawnPos = new Vector3(x, 0.5f, z);
        Debug.Log("Generated random spawn position: " + spawnPos + " for room " + room.id);
        return spawnPos;
    }

    public List<Task> GetTasksForRoom(int roomId)
    {
        if (tasksByRoom != null && tasksByRoom.ContainsKey(roomId))
        {
            Debug.Log("Returning tasks for room " + roomId + ". Task count: " + tasksByRoom[roomId].Count);
            return tasksByRoom[roomId];
        }
        Debug.Log("No tasks found for room " + roomId + ". Returning empty list.");
        return new List<Task>();
    }

    public void RemoveTask(string taskNameToRemove)
    {
        // Remove from active tasks
        activeTasks.RemoveAll(task => task.taskName == taskNameToRemove);

        // Remove from tasks by room
        foreach (var roomId in tasksByRoom.Keys.ToList()) // ToList() to avoid modifying during iteration
        {
            if (tasksByRoom[roomId] != null)
            {
                tasksByRoom[roomId].RemoveAll(task => task.taskName == taskNameToRemove);
                if (tasksByRoom[roomId].Count == 0)
                {
                    tasksByRoom.Remove(roomId); // Remove empty room lists
                }
            }
        }

        Debug.Log($"Task '{taskNameToRemove}' removed by TaskManager.");
    }
}