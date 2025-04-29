using UnityEngine;
using TMPro; // Make sure you have TextMeshPro imported
using System.Collections.Generic;
using System.Linq;

public class TaskDisplayUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI[] taskNameBoxes; // Array of your TextMeshPro boxes (ASSIGN IN INSPECTOR)

    [Header("Optional UI Elements")]
    [SerializeField]
    private GameObject taskListPanel; // The panel to show/hide
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Tab; // Key to toggle visibility

    [Header("Completed Task Visuals")]
    [SerializeField]
    private Color completedTaskColor = Color.gray; // Color for completed text
    [SerializeField]
    private FontStyles completedTaskStyle = FontStyles.Strikethrough | FontStyles.Italic; // Style for completed text

    [Header("Task List UI Animation (Optional)")]
    [SerializeField]
    private Animator taskListAnimator; // Assign in Inspector if this script controls animation
    [SerializeField]
    private string boolParameterName = "IsTaskListVisible"; // Name of the boolean parameter in the Animator

    private bool isTaskListVisible = false;
    private List<string> displayedTaskNames = new List<string>(); // Keep track of displayed task names

    void Start()
    {
        if (taskNameBoxes == null || taskNameBoxes.Length == 0)
        {
            Debug.LogError("Task Name Boxes array is not assigned or empty in the Inspector!");
            enabled = false;
            return;
        }

        // Verify Line Renderers exist (optional but helpful for strikethrough)
        for (int i = 0; i < taskNameBoxes.Length; i++)
        {
            if (taskNameBoxes[i] != null)
            {
                LineRenderer lr = taskNameBoxes[i].GetComponentInChildren<LineRenderer>(true); // true to include inactive
                if (lr == null)
                {
                    Debug.LogWarning($"No LineRenderer found as a child of or on the same GameObject as Task Name Box {i}. Strikethrough will not work for this slot.");
                }
                else
                {
                    lr.enabled = false; // Ensure it's disabled at start
                }
            }
            else
            {
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
        // If you're controlling animation from here based on a key press
        else if (taskListAnimator != null && Input.GetKeyDown(toggleKey))
        {
            ToggleTaskListAnimation();
        }
    }

    // Called by MikesTaskManager to set the initial list of tasks
    public void SetTasks(List<Task> tasks)
    {
        displayedTaskNames.Clear();
        foreach (var task in tasks)
        {
            displayedTaskNames.Add(task.taskName);
        }
        UpdateTaskDisplay(); // Update the UI based on the stored names
    }

    // Method called by MikesTaskManager when a task is completed
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

                // 2. Activate and Position Line Renderer (Optional Strikethrough)
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
                        Vector3 startPoint = (corners[0] + corners[1]) * 0.5f; // Middle of left edge
                        Vector3 endPoint = (corners[2] + corners[3]) * 0.5f;   // Middle of right edge

                        lineRenderer.SetPosition(0, startPoint);
                        lineRenderer.SetPosition(1, endPoint);
                        lineRenderer.enabled = true; // Enable the line
                        Debug.Log($"Enabled LineRenderer for Task Box {i}.");
                    }
                    else
                    {
                        Debug.LogWarning($"RectTransform not found for Task Box {i}. Cannot position LineRenderer accurately.");
                        // Optionally enable with default positions as fallback if needed
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
    private void UpdateTaskDisplay()
    {
        ClearAllBoxes(); // Clear previous state first

        // Display the current tasks in the available text boxes
        for (int i = 0; i < displayedTaskNames.Count; i++)
        {
            if (i < taskNameBoxes.Length && taskNameBoxes[i] != null)
            {
                taskNameBoxes[i].text = displayedTaskNames[i];
                // Reset text style (in case it was previously completed)
                taskNameBoxes[i].color = Color.white; // Or your default text color
                taskNameBoxes[i].fontStyle = FontStyles.Normal;
            }
            else if (i >= taskNameBoxes.Length)
            {
                Debug.LogWarning($"More tasks provided ({displayedTaskNames.Count}) than available Task Name Boxes ({taskNameBoxes.Length}). Task '{displayedTaskNames[i]}' will not be displayed.");
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

    public void ToggleTaskList()
    {
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(!taskListPanel.activeSelf);
        }
        else
        {
            Debug.LogWarning("TaskListPanel is not assigned, cannot toggle visibility.");
        }

        // If you want to control the animation from this script based on panel visibility
        if (taskListAnimator != null)
        {
            isTaskListVisible = taskListPanel != null && taskListPanel.activeSelf;
            taskListAnimator.SetBool(boolParameterName, isTaskListVisible);
        }
    }

    // Separate function to control the animation directly (if needed)
    public void ToggleTaskListAnimation()
    {
        if (taskListAnimator != null)
        {
            isTaskListVisible = !isTaskListVisible;
            taskListAnimator.SetBool(boolParameterName, isTaskListVisible);

            // Optionally also toggle the panel here if the animation dictates visibility
            if (taskListPanel != null)
            {
                taskListPanel.SetActive(isTaskListVisible);
            }
        }
        else
        {
            Debug.LogWarning("TaskList Animator not assigned in TaskDisplayUI, cannot play animation.");
        }
    }

    // Method to directly add a task name to the display
    public void AddTaskToDisplay(string newTaskName)
    {
        displayedTaskNames.Add(newTaskName);
        UpdateTaskDisplay();
    }
}