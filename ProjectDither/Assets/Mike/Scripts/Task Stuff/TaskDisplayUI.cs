using UnityEngine;
using TMPro; // Make sure you have TextMeshPro imported
using System.Collections.Generic;
using System.Linq;

public class TaskDisplayUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI[] taskNameBoxes; // Array of your 5 TextMeshPro boxes (ASSIGN IN INSPECTOR)

    [Header("Optional")]
    [SerializeField]
    private GameObject taskListPanel; // The panel to show/hide
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Tab; // Key to toggle visibility
    [SerializeField]
    private Color completedTaskColor = Color.gray; // Color for completed text
    [SerializeField]
    private FontStyles completedTaskStyle = FontStyles.Strikethrough | FontStyles.Italic; // Style for completed text

    // We don't strictly need to store the list if MikesTaskManager always calls SetTasks
    // private List<Task> displayedTasks = new List<Task>();

    void Start()
    {
        if (taskNameBoxes == null || taskNameBoxes.Length == 0)
        {
            Debug.LogError("Task Name Boxes array is not assigned or empty in the Inspector!");
            enabled = false;
            return;
        }
        // Optional: Check if exactly 5? Depends on your design.
        // if (taskNameBoxes.Length != 5)
        // {
        //    Debug.LogWarning("Expected 5 Task Name Boxes, found " + taskNameBoxes.Length);
        // }

        // Verify Line Renderers exist (optional but helpful)
        for(int i = 0; i < taskNameBoxes.Length; i++)
        {
            if (taskNameBoxes[i] != null) {
                // Check on the same GameObject or a specific child/parent if structured differently
                 LineRenderer lr = taskNameBoxes[i].GetComponentInChildren<LineRenderer>(true); // true to include inactive
                 if (lr == null) {
                    Debug.LogWarning($"No LineRenderer found as a child of or on the same GameObject as Task Name Box {i}. Strikethrough will not work for this slot.");
                 } else {
                    lr.enabled = false; // Ensure it's disabled at start
                 }
            } else {
                 Debug.LogWarning($"Task Name Box at index {i} is not assigned.");
            }
        }

        // Optionally hide the panel at the start
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }

        // Clear boxes initially
        ClearAllBoxes();
    }

    void Update()
    {
        // Handle toggling the panel visibility
        if (taskListPanel != null && Input.GetKeyDown(toggleKey))
        {
            ToggleTaskList();
        }
    }

    // Called by MikesTaskManager to set the initial list of tasks
    public void SetTasks(List<Task> tasks)
    {
        // displayedTasks = tasks; // Update internal list if needed for other logic
        UpdateTaskDisplay(tasks); // Pass the list directly
    }

    // Method called by MikesTaskManager when a task is completed
    // Renamed slightly to avoid confusion with the base Task method
    public void MarkTaskCompleteUI(string taskNameToMark)
    {
        Debug.Log($"TaskDisplayUI trying to mark '{taskNameToMark}' as complete.");
        // Find the text box displaying this task name
        for (int i = 0; i < taskNameBoxes.Length; i++)
        {
            if (taskNameBoxes[i] != null && taskNameBoxes[i].text == taskNameToMark)
            {
                Debug.Log($"Found '{taskNameToMark}' in text box {i}. Applying changes.");
                // --- Apply Visual Feedback ---

                // 1. Change Text Style (Optional)
                taskNameBoxes[i].color = completedTaskColor;
                taskNameBoxes[i].fontStyle = completedTaskStyle; // Applies Strikethrough | Italic

                // 2. Activate and Position Line Renderer
                // Assuming LineRenderer is a component on the same GameObject or a child
                 LineRenderer lineRenderer = taskNameBoxes[i].GetComponentInChildren<LineRenderer>(true); // Find inactive LR

                if (lineRenderer != null)
                {
                    // Ensure basic settings
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.positionCount = 2;

                    // Calculate line position based on the TextMeshPro RectTransform
                    RectTransform textRect = taskNameBoxes[i].rectTransform;
                    if (textRect != null)
                    {
                        // Get corners in the local space of the TextMeshPro object
                        Vector3[] corners = new Vector3[4];
                        textRect.GetLocalCorners(corners); // corners[0]=bottom-left -> [3]=bottom-right

                        // Calculate center points of left and right edges
                        // Use local positions as LineRenderer is using local space (useWorldSpace = false)
                        Vector3 startPoint = (corners[0] + corners[1]) * 0.5f; // Middle of left edge
                        Vector3 endPoint = (corners[2] + corners[3]) * 0.5f;   // Middle of right edge

                         // If LineRenderer is NOT on the same GameObject as the Text,
                         // you might need to transform points:
                         // startPoint = lineRenderer.transform.InverseTransformPoint(textRect.TransformPoint(startPoint));
                         // endPoint = lineRenderer.transform.InverseTransformPoint(textRect.TransformPoint(endPoint));
                         // But if it's a direct child or on the same object, direct local coords often work. Test this!

                        lineRenderer.SetPosition(0, startPoint);
                        lineRenderer.SetPosition(1, endPoint);
                        lineRenderer.enabled = true; // Enable the line
                        Debug.Log($"Enabled LineRenderer for Task Box {i}.");
                    } else {
                         Debug.LogWarning($"RectTransform not found for Task Box {i}. Cannot position LineRenderer accurately.");
                         // Optionally enable with default positions as fallback
                         // lineRenderer.SetPosition(0, new Vector3(-50, 0, 0));
                         // lineRenderer.SetPosition(1, new Vector3(50, 0, 0));
                         // lineRenderer.enabled = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"LineRenderer component not found for Task Box {i}. Cannot draw strikethrough line.");
                }

                break; // Exit loop once the task is found and processed
            }
        }
         Debug.Log($"Finished searching for '{taskNameToMark}' in UI boxes.");
    }

    // Updates the text in the boxes based on the provided task list
    private void UpdateTaskDisplay(List<Task> tasksToDisplay)
    {
        ClearAllBoxes(); // Clear previous state first

        // Display the current tasks in the available text boxes
        for (int i = 0; i < tasksToDisplay.Count; i++)
        {
            if (i < taskNameBoxes.Length && taskNameBoxes[i] != null)
            {
                 // Make sure the task exists and has a name
                 if(tasksToDisplay[i] != null && !string.IsNullOrEmpty(tasksToDisplay[i].taskName)) {
                    taskNameBoxes[i].text = tasksToDisplay[i].taskName;
                    // Reset text style (in case it was previously completed)
                    taskNameBoxes[i].color = Color.white; // Or your default text color
                    taskNameBoxes[i].fontStyle = FontStyles.Normal;
                 } else {
                     Debug.LogWarning($"Task at index {i} in the provided list is null or has no name.");
                     taskNameBoxes[i].text = "[Invalid Task]"; // Placeholder
                 }

            } else if (i >= taskNameBoxes.Length) {
                Debug.LogWarning($"More tasks provided ({tasksToDisplay.Count}) than available Task Name Boxes ({taskNameBoxes.Length}). Task '{tasksToDisplay[i]?.taskName}' will not be displayed.");
                break; // Stop trying to display if we run out of boxes
            }
        }
    }

    // Helper to clear text and disable line renderers
    private void ClearAllBoxes()
    {
        foreach (var box in taskNameBoxes)
        {
            if (box != null)
            {
                box.text = "";
                box.color = Color.white; // Reset to default color
                box.fontStyle = FontStyles.Normal; // Reset style

                // Also disable the associated Line Renderer
                LineRenderer lr = box.GetComponentInChildren<LineRenderer>(true);
                if (lr != null)
                {
                    lr.enabled = false;
                }
            }
        }
    }

    void ToggleTaskList()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(!taskListPanel.activeSelf);
            // Optionally update display only when shown?
            // if(taskListPanel.activeSelf) UpdateTaskDisplay();
        }
        else
        {
            Debug.LogWarning("TaskListPanel is not assigned, cannot toggle visibility.");
        }
    }

    // Removed AddTask/RemoveTask as MikesTaskManager controls the list via SetTasks
    // public void AddTask(Task newTask) { ... }
    // public void RemoveTask(Task taskToRemove) { ... }
}