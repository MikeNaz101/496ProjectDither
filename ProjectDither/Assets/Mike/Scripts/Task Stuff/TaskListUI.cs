using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TaskListUI : MonoBehaviour
{
    public Transform taskListParent; // The UI element to hold task names (e.g., a Vertical Layout Group)
    public GameObject taskNamePrefab; // A simple Text prefab
    public GameObject taskListPanel; // The entire panel to show/hide
    public KeyCode toggleKey = KeyCode.Tab; // The key to toggle the list

    private List<GameObject> taskNameObjects = new List<GameObject>();

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
        ClearTasks(); // Clear previous UI

        foreach (Task task in tasks)
        {
            GameObject nameObject = Instantiate(taskNamePrefab, taskListParent);
            Text nameText = nameObject.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = task.taskName;
                taskNameObjects.Add(nameObject);
            }
            else
            {
                Debug.LogError("Text component not found on taskNamePrefab!");
            }
        }
    }

    void ClearTasks()
    {
        foreach (GameObject nameObject in taskNameObjects)
        {
            Destroy(nameObject);
        }
        taskNameObjects.Clear();
    }

    void ToggleTaskList()
    {
        taskListPanel.SetActive(!taskListPanel.activeSelf);
    }
}