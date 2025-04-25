using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TaskDisplayUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI[] taskNameBoxes; // Array of your 5 TextMeshPro boxes
    [SerializeField]
    private GameObject taskListPanel; // The panel to show/hide (optional)
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Tab; // Key to toggle visibility (optional)

    private List<Task> displayedTasks = new List<Task>();

    void Start()
    {
        // Ensure you have exactly 5 TextMeshPro boxes assigned
        if (taskNameBoxes.Length != 5)
        {
            Debug.LogError("Please assign exactly 5 TextMeshProUGUI components to the taskNameBoxes array in the Inspector!");
            enabled = false;
            return;
        }

        // Optionally hide the panel at the start
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Handle toggling the panel visibility
        if (taskListPanel != null && Input.GetKeyDown(toggleKey))
        {
            ToggleTaskList();
        }
    }

    // This method will be called by MikesTaskManager to update the displayed tasks
    public void SetTasks(List<Task> tasks)
    {
        displayedTasks = tasks;
        UpdateTaskDisplay();
    }

    // You might not need these Add/Remove methods directly called from elsewhere now,
    // as MikesTaskManager handles the task list. However, you could use them
    // if the UI needs to react to individual task additions/removals outside of a full refresh.
    public void AddTask(Task newTask)
    {
        displayedTasks.Add(newTask);
        UpdateTaskDisplay();
    }

    public void RemoveTask(Task taskToRemove)
    {
        displayedTasks.Remove(taskToRemove);
        UpdateTaskDisplay();
    }

    public void MarkTaskCompletedOnUI(string taskNameToMark)
    {
        // Find the task in our displayed list and update the UI
        for (int i = 0; i < displayedTasks.Count; i++)
        {
            if (displayedTasks[i].taskName == taskNameToMark)
            {
                // Apply your visual feedback here (e.g., change font, color, strikethrough)
                Debug.Log($"Task '{taskNameToMark}' marked completed on UI.");
                // Example:
                // if (completedFont != null && i < taskNameBoxes.Length)
                // {
                //     taskNameBoxes[i].font = completedFont;
                // }
                break; // Assuming task names are unique
            }
        }
        // No need to call UpdateTaskDisplay() here if the visual change is immediate
    }

    private void UpdateTaskDisplay()
    {
        // Clear all the text boxes
        foreach (var box in taskNameBoxes)
        {
            box.text = "";
        }

        // Display the current tasks in the available text boxes
        for (int i = 0; i < Mathf.Min(displayedTasks.Count, taskNameBoxes.Length); i++)
        {
            taskNameBoxes[i].text = displayedTasks[i].taskName;
        }
    }

    void ToggleTaskList()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(!taskListPanel.activeSelf);
        }
        else
        {
            Debug.LogWarning("TaskListPanel is not assigned, cannot toggle visibility.");
        }
    }
}