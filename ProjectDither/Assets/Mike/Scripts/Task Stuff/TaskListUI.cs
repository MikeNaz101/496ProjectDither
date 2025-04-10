using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TaskListUI : MonoBehaviour
{
    public GameObject taskItemPrefab;
    public Transform taskListParent;
    public GameObject taskListPanel; // The entire panel to show/hide
    public KeyCode toggleKey = KeyCode.Tab; // The key to toggle the list

    private List<GameObject> taskItems = new List<GameObject>();
    private Dictionary<string, GameObject> taskItemLookup = new Dictionary<string, GameObject>();

    void Start()
    {
        taskListPanel.SetActive(false); // Initially hide the list
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleTaskList();
        }
    }

    public void SetTasks(List<Task> tasks)
    {
        ClearTasks(); // Clear any existing UI
        foreach (Task task in tasks)
        {
            GameObject taskItem = Instantiate(taskItemPrefab, taskListParent);
            Text taskText = taskItem.GetComponentInChildren<Text>();
            taskText.text = task.taskName;
            taskItems.Add(taskItem);
            taskItemLookup[task.taskName] = taskItem;
        }
    }

    void ClearTasks()
    {
        foreach (GameObject item in taskItems)
        {
            Destroy(item);
        }
        taskItems.Clear();
        taskItemLookup.Clear();
    }

    void ToggleTaskList()
    {
        taskListPanel.SetActive(!taskListPanel.activeSelf);
    }

    public void UpdateTaskDisplay(string completedTaskName)
    {
        if (taskItemLookup.ContainsKey(completedTaskName))
        {
            GameObject item = taskItemLookup[completedTaskName];
            Text taskText = item.GetComponentInChildren<Text>();

            // Create a line to draw across the text
            GameObject line = new GameObject("ScratchLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(item.transform, false);

            // Calculate the position and size of the line
            RectTransform textRect = taskText.GetComponent<RectTransform>();
            RectTransform lineRect = line.GetComponent<RectTransform>();

            lineRect.anchorMin = new Vector2(0, 0.5f); // Middle left
            lineRect.anchorMax = new Vector2(1, 0.5f); // Middle right
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(textRect.rect.width, 5f); // Line thickness
            lineRect.anchoredPosition = Vector2.zero;

            // Set the line's color
            line.GetComponent<Image>().color = Color.red;

            // Make the text look scratched out
            taskText.color = Color.gray;
            taskText.fontStyle = FontStyle.Italic;
        }
    }
}